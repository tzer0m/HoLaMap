(async function () {
    const svgNs = 'http://www.w3.org/2000/svg';
    const container = document.getElementById('map');
    const details = document.getElementById('details');
    const tabs = document.getElementById('view-tabs');
    const colours = { 'port-wan': '#1e88c8', 'port-1000': '#4a8a1c', 'port-100': '#e6c229', 'port-10': '#e67e22', 'port-down': '#7b8794' };
    const darkText = ['port-100', 'port-down'];
    const portSize = 16;
    const portGap = 3;
    const padding = 12;
    const cornerRadius = 6;
    const gridCell = 40;
    const groupGap = 80;
    const groupPadding = 12;
    const titleHeight = 30;
    const roomHeader = 28;
    const chipHeight = 22;
    const chipGap = 6;
    const rowGap = 22;
    const unplacedWidth = 480;
    const laneGap = 10;
    const stackGap = 8;
    const defaultDetails = details.textContent;

    const response = await fetch(container.dataset.graphUrl);
    const graph = await response.json();
    const nodesById = new Map(graph.nodes.map(node => [node.id, node]));
    const portsById = new Map(graph.nodes.flatMap(node => node.ports.map(port => [port.id, port])));
    const roomsById = new Map(graph.floors.flatMap(floor => floor.rooms.map(room => [room.id, room])));

    // Measure names with the same font the labels are drawn in.
    const measure = document.createElement('canvas').getContext('2d');
    const fontFamily = getComputedStyle(container).fontFamily;
    const textWidth = (text, bold) => {
        measure.font = `${bold ? 600 : 400} 12px ${fontFamily}`;
        return measure.measureText(text).width;
    };
    const isInfrastructure = node => ['gateway', 'switch', 'accesspoint'].includes(node.kind);

    // Size each device to fit its name and a row of port squares along the bottom.
    const sizeOf = node => {
        const portsWidth = node.ports.length * (portSize + portGap) - portGap + padding * 2;
        return { width: Math.ceil(Math.max(textWidth(node.label, isInfrastructure(node)) + 28, node.ports.length ? portsWidth : 0)), height: node.ports.length ? 44 : 32 };
    };

    const sources = new Set(graph.edges.map(edge => edge.source));
    const incoming = new Map();
    graph.edges.filter(edge => edge.source !== edge.target).forEach(edge => incoming.set(edge.target, [...(incoming.get(edge.target) ?? []), edge]));
    // Ports a device is fed from are drawn on its top edge so the incoming line plugs straight into them.
    const uplinkPortIds = new Set(graph.edges.filter(edge => edge.targetPort && edge.source !== edge.target).map(edge => edge.targetPort));
    const portY = (port, height) => uplinkPortIds.has(port.id) ? -portSize : height;
    const portX = index => padding + index * (portSize + portGap);

    // SVG helpers.
    const create = (name, attributes, parent) => {
        const element = document.createElementNS(svgNs, name);
        Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
        if (parent) {
            parent.appendChild(element);
        }
        return element;
    };

    const roundedPath = points => {
        let path = `M ${points[0].x} ${points[0].y}`;
        for (let i = 1; i < points.length - 1; i++) {
            const previous = points[i - 1];
            const current = points[i];
            const next = points[i + 1];
            const inLength = Math.hypot(current.x - previous.x, current.y - previous.y);
            const outLength = Math.hypot(next.x - current.x, next.y - current.y);
            const radius = Math.min(cornerRadius, inLength / 2, outLength / 2);
            const start = { x: current.x - (current.x - previous.x) / inLength * radius, y: current.y - (current.y - previous.y) / inLength * radius };
            const end = { x: current.x + (next.x - current.x) / outLength * radius, y: current.y + (next.y - current.y) / outLength * radius };
            path += ` L ${start.x} ${start.y} Q ${current.x} ${current.y} ${end.x} ${end.y}`;
        }
        const last = points[points.length - 1];
        return path + ` L ${last.x} ${last.y}`;
    };

    // A subtle zigzag along each straight run, used for lines carrying PoE.
    const zigzagPath = points => {
        const amplitude = 1.5;
        const step = 3;
        let path = `M ${points[0].x} ${points[0].y}`;
        for (let i = 1; i < points.length; i++) {
            const from = points[i - 1];
            const to = points[i];
            const length = Math.hypot(to.x - from.x, to.y - from.y);
            if (length === 0) {
                continue;
            }
            const unitX = (to.x - from.x) / length;
            const unitY = (to.y - from.y) / length;
            const steps = Math.floor(length / step);
            for (let s = 1; s < steps; s++) {
                const offset = s % 2 === 0 ? -amplitude : amplitude;
                path += ` L ${from.x + unitX * s * step - unitY * offset} ${from.y + unitY * s * step + unitX * offset}`;
            }
            path += ` L ${to.x} ${to.y}`;
        }
        return path;
    };

    // Drops repeated points so zero-length segments never reach the path builders.
    const tidy = points => points.filter((point, index) => index === 0 || point.x !== points[index - 1].x || point.y !== points[index - 1].y);

    const showDetails = (title, body) => {
        details.replaceChildren();
        const heading = document.createElement('strong');
        heading.textContent = title;
        const text = document.createElement('p');
        text.textContent = body;
        details.append(heading, text);
    };

    const drawEdge = (edge, points, parent) => {
        const clean = tidy(points);
        if (clean.length < 2) {
            return;
        }
        const d = roundedPath(clean);
        const group = create('g', { class: 'map-edge' + (edge.online ? '' : ' offline') }, parent);
        create('path', { d: d, class: 'map-edge-hit' }, group);
        create('path', { d: edge.poE ? zigzagPath(clean) : d, class: 'map-edge-line', stroke: colours[edge.style] ?? '#7b8794' }, group);
        group.addEventListener('click', event => {
            event.stopPropagation();
            showDetails(`${nodesById.get(edge.source).label} → ${nodesById.get(edge.target).label} (${edge.label})`, edge.detail);
        });
    };

    // A device with its ports, or just its name when compact.
    const drawNode = (node, x, y, width, height, parent, compact) => {
        const group = create('g', { class: `map-node ${node.kind}${node.online ? '' : ' offline'}`, transform: `translate(${x}, ${y})` }, parent);
        create('rect', { width: width, height: height, rx: compact ? 4 : 8, class: 'map-node-box' }, group);
        const label = create('text', { x: width / 2, y: !compact && node.ports.length ? 18 : height / 2 + 4, class: 'map-node-label' }, group);
        label.textContent = node.label;
        group.addEventListener('click', event => {
            event.stopPropagation();
            showDetails(node.label, node.detail);
        });
        if (compact) {
            return;
        }
        node.ports.forEach((port, index) => {
            const square = create('g', { class: 'map-port', transform: `translate(${portX(index)}, ${portY(port, height)})` }, group);
            create('rect', { width: portSize, height: portSize, rx: 2, fill: colours[port.style] ?? '#7b8794' }, square);
            const number = create('text', { x: portSize / 2, y: portSize / 2 + 3, class: 'map-port-label' + (darkText.includes(port.style) ? ' dark' : '') }, square);
            number.textContent = port.index;
            create('title', {}, square).textContent = port.detail;
            square.addEventListener('click', event => {
                event.stopPropagation();
                showDetails(`${node.label} Port ${port.index}`, port.detail);
            });
        });
    };

    const drawRoom = (name, x, y, width, height, parent) => {
        create('rect', { x: x, y: y, width: width, height: height, rx: 6, class: 'map-room-box' }, parent);
        create('text', { x: x + 8, y: y + 17, class: 'map-room-title' }, parent).textContent = name;
    };

    const drawFrame = (title, x, width, height, parent) => {
        create('rect', { x: x, y: 0, width: width, height: height, rx: 8, class: 'map-floor-box' }, parent);
        create('text', { x: x + groupPadding, y: 20, class: 'map-floor-title' }, parent).textContent = title;
    };

    // Lays out one group of devices with ELK. Rooms become containers around their devices.
    // Client devices at the end of a chain are stacked in a column under their parent instead of spreading sideways.
    const layoutGroup = async group => {
        const keyOf = node => group.rooms.length ? node.room : '';
        const stackEdgesByKey = new Map();
        group.edges.filter(edge => edge.source !== edge.target && !sources.has(edge.target) && nodesById.get(edge.target).kind === 'client' && incoming.get(edge.target).length === 1).forEach(edge => {
            const key = `${edge.source}-${keyOf(nodesById.get(edge.target))}`;
            stackEdgesByKey.set(key, [...(stackEdgesByKey.get(key) ?? []), edge]);
        });
        const stacks = [...stackEdgesByKey.entries()].filter(([, edges]) => edges.length > 1).map(([key, edges]) => {
            edges.sort((a, b) => ((portsById.get(a.sourcePort)?.index ?? 0) - (portsById.get(b.sourcePort)?.index ?? 0)) || nodesById.get(a.target).label.localeCompare(nodesById.get(b.target).label));
            const leafWidth = Math.max(...edges.map(edge => sizeOf(nodesById.get(edge.target)).width));
            let y = 0;
            const items = edges.map((edge, lane) => {
                const size = sizeOf(nodesById.get(edge.target));
                const item = { edge: edge, node: nodesById.get(edge.target), lane: lane, y: y, width: size.width, height: size.height };
                y += size.height + stackGap;
                return item;
            });
            return { id: `stack-${key}`, room: keyOf(items[0].node), items: items, leafWidth: leafWidth, laneStart: leafWidth + 16, width: leafWidth + 16 + edges.length * laneGap, height: y - stackGap };
        });
        const stacksById = new Map(stacks.map(stack => [stack.id, stack]));
        const stackItemsByEdge = new Map(stacks.flatMap(stack => stack.items.map(item => [item.edge.id, { stack: stack, item: item }])));
        const stackedNodeIds = new Set(stacks.flatMap(stack => stack.items.map(item => item.node.id)));

        const nodeChild = node => {
            const size = sizeOf(node);
            return {
                id: node.id,
                width: size.width,
                height: size.height,
                layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
                ports: [
                    { id: `${node.id}-in`, x: size.width / 2, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } },
                    ...node.ports.map((port, index) => ({ id: port.id, x: portX(index), y: portY(port, size.height), width: portSize, height: portSize, layoutOptions: { 'elk.port.side': uplinkPortIds.has(port.id) ? 'NORTH' : 'SOUTH' } }))
                ]
            };
        };
        const stackChild = stack => ({
            id: stack.id,
            width: stack.width,
            height: stack.height,
            layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
            ports: stack.items.map(item => ({ id: `${stack.id}-lane-${item.lane}`, x: stack.laneStart + item.lane * laneGap, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } }))
        });
        const loose = group.nodes.filter(node => !stackedNodeIds.has(node.id));
        const childrenOf = roomId => [...loose.filter(node => keyOf(node) === roomId).map(nodeChild), ...stacks.filter(stack => stack.room === roomId).map(stackChild)];
        const children = [
            ...group.rooms.map(room => {
                const inner = childrenOf(room.id);
                return inner.length ? { id: room.id, layoutOptions: { 'elk.padding': '[top=30,left=12,bottom=12,right=12]' }, children: inner } : { id: room.id, width: 140, height: 60 };
            }),
            ...childrenOf('')
        ];

        const elkGraph = {
            id: 'root',
            layoutOptions: {
                'elk.algorithm': 'layered',
                'elk.direction': 'DOWN',
                'elk.edgeRouting': 'ORTHOGONAL',
                'elk.hierarchyHandling': 'INCLUDE_CHILDREN',
                'elk.layered.spacing.nodeNodeBetweenLayers': '50',
                'elk.layered.spacing.edgeNodeBetweenLayers': '20',
                'elk.layered.spacing.edgeEdgeBetweenLayers': '10',
                'elk.spacing.nodeNode': '24',
                'elk.spacing.edgeEdge': '10',
                'elk.layered.nodePlacement.strategy': 'BRANDES_KOEPF',
                'elk.layered.considerModelOrder.strategy': 'NODES_AND_EDGES'
            },
            children: children,
            edges: group.edges.map(edge => {
                const stacked = stackItemsByEdge.get(edge.id);
                const target = stacked ? `${stacked.stack.id}-lane-${stacked.item.lane}` : edge.targetPort || `${edge.target}-in`;
                return { id: edge.id, sources: [edge.sourcePort], targets: [target] };
            })
        };
        const layout = await new ELK().layout(elkGraph);

        // Collect absolute positions and edges from every level of the result.
        const positions = new Map();
        const layoutEdges = [];
        const walk = (element, offsetX, offsetY) => {
            (element.children ?? []).forEach(child => {
                positions.set(child.id, { x: offsetX + child.x, y: offsetY + child.y, width: child.width, height: child.height });
                walk(child, offsetX + child.x, offsetY + child.y);
            });
            layoutEdges.push(...(element.edges ?? []));
        };
        walk(layout, 0, 0);

        const placements = [];
        group.nodes.forEach(node => {
            if (!stackedNodeIds.has(node.id)) {
                placements.push({ node: node, ...positions.get(node.id) });
            }
        });
        stacks.forEach(stack => {
            const at = positions.get(stack.id);
            stack.items.forEach(item => placements.push({ node: item.node, x: at.x + stack.leafWidth - item.width, y: at.y + item.y, width: item.width, height: item.height }));
        });
        return { group: group, positions: positions, layoutEdges: layoutEdges, placements: placements, stacksById: stacksById, stackItemsByEdge: stackItemsByEdge, width: layout.width, height: layout.height };
    };

    // Draws one laid-out group at an offset and records where every device ended up.
    const drawGroup = (result, ox, oy, layers, absolute) => {
        const transform = `translate(${ox}, ${oy})`;
        const boxes = create('g', { transform: transform }, layers.boxes);
        const edges = create('g', { transform: transform }, layers.edges);
        const nodes = create('g', { transform: transform }, layers.nodes);
        result.group.rooms.forEach(room => {
            const box = result.positions.get(room.id);
            drawRoom(room.name, box.x, box.y, box.width, box.height, boxes);
        });
        result.layoutEdges.forEach(layoutEdge => {
            const edge = graph.edges.find(candidate => candidate.id === layoutEdge.id);
            const stacked = result.stackItemsByEdge.get(edge.id);
            (layoutEdge.sections || []).forEach(section => {
                // Edge coordinates are relative to the container ELK routed them in, which is a room for lines inside one.
                const origin = result.positions.get(layoutEdge.container) ?? { x: 0, y: 0 };
                const points = [section.startPoint, ...(section.bendPoints || []), section.endPoint].map(point => ({ x: origin.x + point.x, y: origin.y + point.y }));
                if (stacked) {
                    // Continue down the lane, then turn into the side of the stacked device.
                    const stackLayout = result.positions.get(stacked.stack.id);
                    const centreY = stackLayout.y + stacked.item.y + stacked.item.height / 2;
                    points.push({ x: points[points.length - 1].x, y: centreY }, { x: stackLayout.x + stacked.stack.leafWidth, y: centreY });
                }
                drawEdge(edge, points, edges);
            });
        });
        result.placements.forEach(placement => {
            drawNode(placement.node, placement.x, placement.y, placement.width, placement.height, nodes, false);
            absolute.set(placement.node.id, { x: ox + placement.x, y: oy + placement.y, width: placement.width, height: placement.height });
        });
    };

    // Line from a source port to a device in another group, which ELK never saw.
    const crossPoints = (edge, absolute) => {
        const from = absolute.get(edge.source);
        const to = absolute.get(edge.target);
        const fromIndex = nodesById.get(edge.source).ports.findIndex(port => port.id === edge.sourcePort);
        const toIndex = edge.targetPort ? nodesById.get(edge.target).ports.findIndex(port => port.id === edge.targetPort) : -1;
        const startX = fromIndex >= 0 ? from.x + portX(fromIndex) + portSize / 2 : from.x + from.width / 2;
        const startY = from.y + from.height + (fromIndex >= 0 ? portSize : 0);
        const endX = toIndex >= 0 ? to.x + portX(toIndex) + portSize / 2 : to.x + to.width / 2;
        const endY = to.y - (toIndex >= 0 ? portSize : 0);
        const turnY = endY > startY + 30 ? (startY + endY) / 2 : startY + 14;
        return [{ x: startX, y: startY }, { x: startX, y: turnY }, { x: endX, y: turnY }, { x: endX, y: endY }];
    };

    // Which groups the All and UniFi views split the devices into.
    const groupsFor = mode => {
        if (mode === 'unifi' || !graph.floors.length) {
            return [{ title: null, rooms: [], nodes: graph.nodes, edges: graph.edges }];
        }
        const groups = graph.floors.map(floor => ({ title: floor.name, rooms: floor.rooms, nodes: graph.nodes.filter(node => floor.rooms.some(room => room.id === node.room)) }));
        const unplaced = graph.nodes.filter(node => !roomsById.has(node.room));
        if (unplaced.length) {
            groups.push({ title: 'Unplaced', rooms: [], nodes: unplaced });
        }
        groups.forEach(group => {
            const ids = new Set(group.nodes.map(node => node.id));
            group.edges = graph.edges.filter(edge => ids.has(edge.source) && ids.has(edge.target));
        });
        return groups;
    };

    // Wraps compact device boxes into rows no wider than maxWidth.
    const flowChips = (nodes, maxWidth, top) => {
        let x = 8;
        let y = top;
        const sorted = [...nodes].sort((a, b) => (isInfrastructure(b) - isInfrastructure(a)) || a.label.localeCompare(b.label));
        const items = sorted.map(node => {
            const width = Math.ceil(textWidth(node.label, isInfrastructure(node)) + 16);
            if (x > 8 && x + width > maxWidth) {
                x = 8;
                y += chipHeight + rowGap;
            }
            const item = { node: node, x: x, y: y, width: width, height: chipHeight };
            x += width + chipGap;
            return item;
        });
        return { items: items, height: nodes.length ? y + chipHeight + 8 : top + 8 };
    };

    // Elbow line between two boxes. Boxes in the same row are joined by a line that runs above them, using one of a few lanes so lines don't overlap.
    const elbow = (a, b, lane) => {
        const ax = a.x + a.width / 2;
        const ay = a.y + a.height / 2;
        const bx = b.x + b.width / 2;
        const by = b.y + b.height / 2;
        if (Math.abs(by - ay) < chipHeight) {
            const laneY = Math.min(a.y, b.y) - 4 - (lane % 4) * 4;
            return [{ x: ax, y: a.y }, { x: ax, y: laneY }, { x: bx, y: laneY }, { x: bx, y: b.y }];
        }
        const startY = by > ay ? a.y + a.height : a.y;
        const endY = by > ay ? b.y : b.y + b.height;
        const midY = (startY + endY) / 2 + (lane % 3 - 1) * 3;
        return [{ x: ax, y: startY }, { x: ax, y: midY }, { x: bx, y: midY }, { x: bx, y: endY }];
    };

    let renderId = 0;
    const render = async mode => {
        const id = ++renderId;
        tabs.querySelectorAll('button').forEach(button => button.classList.toggle('active', button.dataset.view === mode));
        details.textContent = defaultDetails;
        if (mode === 'floorplan' && !graph.floors.length) {
            container.replaceChildren();
            container.textContent = 'No floors yet. Add floors and rooms on the Floor Plan page.';
            return;
        }
        const svg = create('svg', { class: 'map-svg' });
        const viewport = create('g', {}, svg);
        const layers = { boxes: create('g', {}, viewport), edges: create('g', {}, viewport), nodes: create('g', {}, viewport) };
        const absolute = new Map();
        const bounds = { width: 0, height: 0 };

        if (mode === 'floorplan') {
            // Rooms sit where they were saved. Devices flow inside them as compact boxes.
            let ox = groupPadding;
            const frames = graph.floors.map(floor => ({ title: floor.name, rooms: floor.rooms.map(room => ({ name: room.name, x: room.x * gridCell, y: room.y * gridCell, width: room.width * gridCell, minHeight: room.height * gridCell, nodes: graph.nodes.filter(node => node.room === room.id) })) }));
            const unplaced = graph.nodes.filter(node => !roomsById.has(node.room));
            if (unplaced.length) {
                frames.push({ title: 'Unplaced', rooms: [{ name: '', x: 0, y: 0, width: unplacedWidth, minHeight: 0, nodes: unplaced }] });
            }
            frames.forEach(frame => {
                const rooms = frame.rooms.map(room => {
                    const flow = flowChips(room.nodes, room.width - 8, room.name ? roomHeader : 8);
                    return { ...room, height: Math.max(room.minHeight, flow.height), chips: flow.items };
                });
                const width = Math.max(...rooms.map(room => room.x + room.width));
                const height = Math.max(...rooms.map(room => room.y + room.height));
                drawFrame(frame.title, ox - groupPadding, width + groupPadding * 2, titleHeight + height + groupPadding, layers.boxes);
                rooms.forEach(room => {
                    if (room.name) {
                        drawRoom(room.name, ox + room.x, titleHeight + room.y, room.width, room.height, layers.boxes);
                    }
                    room.chips.forEach(chip => {
                        const placement = { x: ox + room.x + chip.x, y: titleHeight + room.y + chip.y, width: chip.width, height: chip.height };
                        absolute.set(chip.node.id, placement);
                        drawNode(chip.node, placement.x, placement.y, placement.width, placement.height, layers.nodes, true);
                    });
                });
                bounds.height = Math.max(bounds.height, titleHeight + height + groupPadding);
                ox += width + groupPadding * 2 + groupGap;
            });
            bounds.width = ox - groupGap;
            graph.edges.filter(edge => edge.source !== edge.target && absolute.has(edge.source) && absolute.has(edge.target)).forEach((edge, lane) => drawEdge(edge, elbow(absolute.get(edge.source), absolute.get(edge.target), lane), layers.edges));
        } else {
            const groups = groupsFor(mode);
            const results = [];
            for (const group of groups) {
                results.push(await layoutGroup(group));
                if (id !== renderId) {
                    return;
                }
            }
            let ox = groups[0].title ? groupPadding : 0;
            results.forEach(result => {
                const oy = result.group.title ? titleHeight : 0;
                if (result.group.title) {
                    drawFrame(result.group.title, ox - groupPadding, result.width + groupPadding * 2, oy + result.height + groupPadding, layers.boxes);
                }
                drawGroup(result, ox, oy, layers, absolute);
                bounds.height = Math.max(bounds.height, oy + result.height + groupPadding);
                ox += result.width + (result.group.title ? groupPadding * 2 + groupGap : 0);
            });
            bounds.width = ox - (groups[0].title ? groupGap : 0);
            const groupOf = new Map(results.flatMap((result, index) => result.group.nodes.map(node => [node.id, index])));
            graph.edges.filter(edge => groupOf.get(edge.source) !== groupOf.get(edge.target) && absolute.has(edge.source) && absolute.has(edge.target)).forEach(edge => drawEdge(edge, crossPoints(edge, absolute), layers.edges));
        }

        // Pan and zoom.
        const view = { scale: 1, x: 0, y: 0 };
        const apply = () => viewport.setAttribute('transform', `translate(${view.x}, ${view.y}) scale(${view.scale})`);
        const fit = () => {
            const box = container.getBoundingClientRect();
            view.scale = Math.min(box.width / (bounds.width + 40), box.height / (bounds.height + 40), 1.5);
            view.x = (box.width - bounds.width * view.scale) / 2;
            view.y = 20;
            apply();
        };
        container.replaceChildren(svg);
        fit();

        svg.addEventListener('wheel', event => {
            event.preventDefault();
            const box = svg.getBoundingClientRect();
            const mouseX = event.clientX - box.left;
            const mouseY = event.clientY - box.top;
            const scale = Math.min(4, Math.max(0.2, view.scale * (event.deltaY < 0 ? 1.1 : 1 / 1.1)));
            view.x = mouseX - (mouseX - view.x) * (scale / view.scale);
            view.y = mouseY - (mouseY - view.y) * (scale / view.scale);
            view.scale = scale;
            apply();
        }, { passive: false });

        let drag = null;
        svg.addEventListener('pointerdown', event => {
            if (event.target.closest('.map-node, .map-edge')) {
                return;
            }
            drag = { x: event.clientX, y: event.clientY, viewX: view.x, viewY: view.y };
            svg.setPointerCapture(event.pointerId);
        });
        svg.addEventListener('pointermove', event => {
            if (drag) {
                view.x = drag.viewX + event.clientX - drag.x;
                view.y = drag.viewY + event.clientY - drag.y;
                apply();
            }
        });
        svg.addEventListener('pointerup', () => drag = null);
        svg.addEventListener('dblclick', fit);
    };

    const modes = ['all', 'unifi', 'floorplan'];
    let stored = null;
    try {
        stored = localStorage.getItem('holamap.view');
    } catch (error) {
        stored = null;
    }
    let mode = modes.includes(stored) ? stored : 'all';
    tabs.addEventListener('click', event => {
        const button = event.target.closest('button[data-view]');
        if (!button) {
            return;
        }
        mode = button.dataset.view;
        try {
            localStorage.setItem('holamap.view', mode);
        } catch (error) {
            // Storage is only a convenience for remembering the view.
        }
        render(mode);
    });
    await render(mode);
})();
