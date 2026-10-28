(async function () {
    const svgNs = 'http://www.w3.org/2000/svg';
    const container = document.getElementById('map');
    const details = document.getElementById('details');
    const tabs = document.getElementById('view-tabs');
    const colours = { 'port-wan': '#1e88c8', 'port-1000': '#4a8a1c', 'port-100': '#e6c229', 'port-10': '#e67e22', 'port-down': '#7b8794' };
    const darkText = ['port-100', 'port-down'];
    const portSize = 16;
    const portGap = 4;
    const padding = 12;
    const cornerRadius = 6;
    const gridCell = 40;
    const groupGap = 80;
    const groupPadding = 12;
    const titleHeight = 32;
    const roomHeader = 28;
    const roomPadding = 12;
    const chipHeight = 24;
    const laneGap = 10;
    const stackGap = 8;
    const step = 4;
    const margin = 40;
    const corridor = 80;
    const defaultDetails = details.textContent;

    const response = await fetch(container.dataset.graphUrl);
    const raw = await response.json();
    const portsById = new Map(raw.nodes.flatMap(node => node.ports.map(port => [port.id, port])));
    const roomsById = new Map(raw.floors.flatMap(floor => floor.rooms.map(room => [room.id, room])));
    const rawById = new Map(raw.nodes.map(node => [node.id, node]));
    const rackOf = new Map(raw.nodes.filter(node => node.rack).map(node => [node.id, node.rack]));

    // The All and Floor Plan views draw each rack as one block in place of its devices. Links between devices in the same rack are hidden.
    const collapsed = {
        ...raw,
        nodes: [...raw.nodes.filter(node => !node.rack), ...raw.racks.map(rack => ({ id: rack.id, label: rack.name, kind: 'rack', detail: rack.detail, room: rack.room, rack: '', online: rack.online, ports: [] }))],
        edges: raw.edges.filter(edge => !rackOf.get(edge.source) || rackOf.get(edge.source) !== rackOf.get(edge.target)).map(edge => {
            const fromRack = rackOf.get(edge.source);
            const toRack = rackOf.get(edge.target);
            if (!fromRack && !toRack) {
                return edge;
            }
            const fromPort = portsById.get(edge.sourcePort);
            const toPort = portsById.get(edge.targetPort);
            return { ...edge, source: fromRack ?? edge.source, target: toRack ?? edge.target, sourcePort: fromRack ? '' : edge.sourcePort, targetPort: toRack ? '' : edge.targetPort, label: `${rawById.get(edge.source).label}${fromPort ? ` Port ${fromPort.index}` : ''} → ${rawById.get(edge.target).label}${toPort ? ` Port ${toPort.index}` : ''}` };
        })
    };

    // What the current view is drawing, and the lookups built from it.
    let graph = raw;
    let nodesById = null;
    let sources = null;
    let incoming = null;
    let uplinkPortIds = null;
    const useView = data => {
        graph = data;
        nodesById = new Map(graph.nodes.map(node => [node.id, node]));
        sources = new Set(graph.edges.map(edge => edge.source));
        incoming = new Map();
        graph.edges.filter(edge => edge.source !== edge.target).forEach(edge => incoming.set(edge.target, [...(incoming.get(edge.target) ?? []), edge]));
        // Ports a device is fed from are drawn on its top edge so the incoming line plugs straight into them.
        uplinkPortIds = new Set(graph.edges.filter(edge => edge.targetPort && edge.source !== edge.target).map(edge => edge.targetPort));
    };
    const roundUp = (value, to) => Math.ceil(value / to) * to;
    // How much devices are shrunk on the current view; lines shrink to match.
    let lineScale = 1;

    // Measure names with the same font the labels are drawn in.
    const measure = document.createElement('canvas').getContext('2d');
    const fontFamily = getComputedStyle(container).fontFamily;
    const textWidth = (text, bold) => {
        measure.font = `${bold ? 600 : 400} 12px ${fontFamily}`;
        return measure.measureText(text).width;
    };
    const isInfrastructure = node => ['gateway', 'switch', 'accesspoint', 'rack'].includes(node.kind);

    // Size each device to fit its name and a row of port squares along the bottom. Widths are multiples of 8 so lines can land on the routing grid.
    const sizeOf = node => {
        const portsWidth = node.ports.length * (portSize + portGap) - portGap + padding * 2;
        if (node.kind === 'rack') {
            return { width: roundUp(Math.max(textWidth(node.label, true) + 40, 120), 8), height: 48 };
        }
        return { width: roundUp(Math.max(textWidth(node.label, isInfrastructure(node)) + 28, node.ports.length ? portsWidth : 0), 8), height: node.ports.length ? 44 : 32 };
    };

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
            const radius = Math.min(cornerRadius * lineScale, inLength / 2, outLength / 2);
            const start = { x: current.x - (current.x - previous.x) / inLength * radius, y: current.y - (current.y - previous.y) / inLength * radius };
            const end = { x: current.x + (next.x - current.x) / outLength * radius, y: current.y + (next.y - current.y) / outLength * radius };
            path += ` L ${start.x} ${start.y} Q ${current.x} ${current.y} ${end.x} ${end.y}`;
        }
        const last = points[points.length - 1];
        return path + ` L ${last.x} ${last.y}`;
    };

    // A subtle zigzag along each straight run, used for lines carrying PoE.
    const zigzagPath = points => {
        const amplitude = 1.5 * lineScale;
        const zigStep = Math.max(1.5, 3 * lineScale);
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
            const steps = Math.floor(length / zigStep);
            for (let s = 1; s < steps; s++) {
                const offset = s % 2 === 0 ? -amplitude : amplitude;
                path += ` L ${from.x + unitX * s * zigStep - unitY * offset} ${from.y + unitY * s * zigStep + unitX * offset}`;
            }
            path += ` L ${to.x} ${to.y}`;
        }
        return path;
    };

    // Drops repeated and collinear points so the path builders only see real corners.
    const tidy = points => {
        const distinct = points.filter((point, index) => index === 0 || point.x !== points[index - 1].x || point.y !== points[index - 1].y);
        return distinct.filter((point, index) => {
            if (index === 0 || index === distinct.length - 1) {
                return true;
            }
            const before = distinct[index - 1];
            const after = distinct[index + 1];
            return !((before.x === point.x && point.x === after.x) || (before.y === point.y && point.y === after.y));
        });
    };

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
        create('path', { d: edge.poE ? zigzagPath(clean) : d, class: 'map-edge-line', stroke: colours[edge.style] ?? '#7b8794', style: `stroke-width: ${3 * lineScale}px` }, group);
        group.addEventListener('click', event => {
            event.stopPropagation();
            showDetails(`${nodesById.get(edge.source).label} → ${nodesById.get(edge.target).label} (${edge.label})`, edge.detail);
        });
    };

    // A device with its ports, or just its name when compact.
    const drawNode = (node, x, y, width, height, parent, compact, scale) => {
        const group = create('g', { class: `map-node ${node.kind}${node.online ? '' : ' offline'}`, transform: `translate(${x}, ${y}) scale(${scale ?? 1})` }, parent);
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

    const drawFrame = (title, x, y, width, height, parent) => {
        create('rect', { x: x, y: y, width: width, height: height, rx: 8, class: 'map-floor-box' }, parent);
        create('text', { x: x + groupPadding, y: y + 20, class: 'map-floor-title' }, parent).textContent = title;
    };

    // A device drawn as just its name, used on the Floor Plan view.
    const chipSize = node => ({ width: roundUp(textWidth(node.label, isInfrastructure(node)) + 16, 8), height: chipHeight });

    // Lays out a set of devices with ELK and routes their lines. Client devices at the end of a chain are stacked in a column under their parent instead of spreading sideways.
    // Compact lays devices out as name-only chips; aspect asks ELK to pack separate groups to roughly that width-to-height ratio.
    const layoutGroup = async (group, compact, aspect) => {
        const sizeFor = node => compact ? chipSize(node) : sizeOf(node);
        const stackEdgesByParent = new Map();
        group.edges.filter(edge => edge.source !== edge.target && !sources.has(edge.target) && nodesById.get(edge.target).kind === 'client' && incoming.get(edge.target).length === 1).forEach(edge => stackEdgesByParent.set(edge.source, [...(stackEdgesByParent.get(edge.source) ?? []), edge]));
        const stacks = [...stackEdgesByParent.entries()].filter(([, edges]) => edges.length > 1).map(([parentId, edges]) => {
            edges.sort((a, b) => ((portsById.get(a.sourcePort)?.index ?? 0) - (portsById.get(b.sourcePort)?.index ?? 0)) || nodesById.get(a.target).label.localeCompare(nodesById.get(b.target).label));
            const leafWidth = Math.max(...edges.map(edge => sizeFor(nodesById.get(edge.target)).width));
            let y = 0;
            const items = edges.map((edge, lane) => {
                const size = sizeFor(nodesById.get(edge.target));
                const item = { edge: edge, node: nodesById.get(edge.target), lane: lane, y: y, width: size.width, height: size.height };
                y += size.height + stackGap;
                return item;
            });
            return { id: `stack-${parentId}`, items: items, leafWidth: leafWidth, laneStart: leafWidth + 16, width: leafWidth + 16 + edges.length * laneGap, height: y - stackGap };
        });
        const stackItemsByEdge = new Map(stacks.flatMap(stack => stack.items.map(item => [item.edge.id, { stack: stack, item: item }])));
        const stackedNodeIds = new Set(stacks.flatMap(stack => stack.items.map(item => item.node.id)));

        const elkGraph = {
            id: 'root',
            layoutOptions: {
                'elk.algorithm': 'layered',
                'elk.direction': 'DOWN',
                'elk.edgeRouting': 'ORTHOGONAL',
                'elk.layered.spacing.nodeNodeBetweenLayers': '50',
                'elk.layered.spacing.edgeNodeBetweenLayers': '20',
                'elk.layered.spacing.edgeEdgeBetweenLayers': '10',
                'elk.spacing.nodeNode': '24',
                'elk.spacing.edgeEdge': '10',
                'elk.layered.nodePlacement.strategy': 'BRANDES_KOEPF',
                'elk.layered.considerModelOrder.strategy': 'NODES_AND_EDGES',
                ...(aspect ? { 'elk.aspectRatio': String(aspect) } : {})
            },
            children: [
                ...group.nodes.filter(node => !stackedNodeIds.has(node.id)).map(node => {
                    const size = sizeFor(node);
                    return {
                        id: node.id,
                        width: size.width,
                        height: size.height,
                        layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
                        ports: [
                            { id: `${node.id}-in`, x: size.width / 2, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } },
                            { id: `${node.id}-out`, x: size.width / 2, y: size.height, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'SOUTH' } },
                            ...(compact ? [] : node.ports).map((port, index) => ({ id: port.id, x: portX(index), y: portY(port, size.height), width: portSize, height: portSize, layoutOptions: { 'elk.port.side': uplinkPortIds.has(port.id) ? 'NORTH' : 'SOUTH' } }))
                        ]
                    };
                }),
                ...stacks.map(stack => ({
                    id: stack.id,
                    width: stack.width,
                    height: stack.height,
                    layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
                    ports: stack.items.map(item => ({ id: `${stack.id}-lane-${item.lane}`, x: stack.laneStart + item.lane * laneGap, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } }))
                }))
            ],
            edges: group.edges.map(edge => {
                const stacked = stackItemsByEdge.get(edge.id);
                const target = stacked ? `${stacked.stack.id}-lane-${stacked.item.lane}` : edge.targetPort || `${edge.target}-in`;
                return { id: edge.id, sources: [(compact ? '' : edge.sourcePort) || `${edge.source}-out`], targets: [compact && !stacked ? `${edge.target}-in` : target] };
            })
        };
        const layout = await new ELK().layout(elkGraph);
        const layoutById = new Map(layout.children.map(child => [child.id, child]));

        const placements = [];
        layout.children.forEach(child => {
            const stack = stacks.find(candidate => candidate.id === child.id);
            if (stack) {
                stack.items.forEach(item => placements.push({ node: item.node, x: child.x + stack.leafWidth - item.width, y: child.y + item.y, width: item.width, height: item.height, stacked: true }));
            } else {
                placements.push({ node: nodesById.get(child.id), x: child.x, y: child.y, width: child.width, height: child.height, stacked: false });
            }
        });
        return { layout: layout, layoutById: layoutById, layoutEdges: layout.edges ?? [], placements: placements, stackItemsByEdge: stackItemsByEdge, width: layout.width, height: layout.height };
    };

    // Draws a laid-out group with the lines ELK routed.
    const drawGroup = (result, layers) => {
        result.layoutEdges.forEach(layoutEdge => {
            const edge = graph.edges.find(candidate => candidate.id === layoutEdge.id);
            const stacked = result.stackItemsByEdge.get(edge.id);
            (layoutEdge.sections || []).forEach(section => {
                const points = [section.startPoint, ...(section.bendPoints || []), section.endPoint];
                if (stacked) {
                    // Continue down the lane, then turn into the side of the stacked device.
                    const stackLayout = result.layoutById.get(stacked.stack.id);
                    const centreY = stackLayout.y + stacked.item.y + stacked.item.height / 2;
                    points.push({ x: section.endPoint.x, y: centreY }, { x: stackLayout.x + stacked.stack.leafWidth, y: centreY });
                }
                drawEdge(edge, points, layers.edges);
            });
        });
        result.placements.forEach(placement => drawNode(placement.node, placement.x, placement.y, placement.width, placement.height, layers.nodes, false));
    };

    // How far a device reaches beyond its box: port squares hang below it, or above it for the ports it is fed from.
    const extents = item => {
        if (item.compact || !item.node.ports.length) {
            return { top: 0, bottom: 0 };
        }
        const reach = portSize * (item.scale ?? 1);
        return { top: item.node.ports.some(port => uplinkPortIds.has(port.id)) ? reach : 0, bottom: item.node.ports.some(port => !uplinkPortIds.has(port.id)) ? reach : 0 };
    };

    // Lays a set of devices out as a small tree, leaving room above for the ports they are fed from. The Floor Plan view uses name-only chips.
    const contentFor = async (mode, nodes, aspect) => {
        const compact = mode === 'floorplan';
        const ids = new Set(nodes.map(node => node.id));
        const result = await layoutGroup({ nodes: nodes, edges: graph.edges.filter(edge => ids.has(edge.source) && ids.has(edge.target)) }, compact, aspect);
        const items = result.placements.map(placement => ({ ...placement, y: placement.y + (compact ? 0 : portSize), compact: compact }));
        return { items: items, width: Math.max(...items.map(item => item.x + item.width)), height: Math.max(...items.map(item => item.y + item.height + extents(item).bottom)) };
    };

    // Turns laid-out devices into boxes inside a room, shrunk by the scale so they fit.
    const place = (content, scale, left, top) => content.items.map(item => ({ ...item, nativeWidth: item.width, nativeHeight: item.height, x: left + item.x * scale, y: top + item.y * scale, width: item.width * scale, height: item.height * scale, scale: scale }));

    // Rooms exactly as saved on the floor plan, floors side by side. Devices shrink to fit, all by the same amount so every room matches.
    const buildFrames = async mode => {
        const scales = [1, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.25];
        const fits = (content, scale, availWidth, availHeight) => content.width * scale <= availWidth + 0.5 && content.height * scale <= availHeight + 0.5;
        const plans = [];
        for (const floor of graph.floors) {
            for (const room of floor.rooms) {
                const nodes = graph.nodes.filter(node => node.room === room.id);
                const availWidth = room.width * gridCell - roomPadding * 2;
                const availHeight = room.height * gridCell - roomHeader - roomPadding;
                const content = nodes.length ? await contentFor(mode, nodes, availWidth / availHeight) : null;
                plans.push({ floor: floor, room: room, nodes: nodes, availWidth: availWidth, availHeight: availHeight, content: content });
            }
        }
        const unplaced = graph.nodes.filter(node => !roomsById.has(node.room));
        const unplacedContent = unplaced.length ? await contentFor(mode, unplaced, 2) : null;

        // The largest scale at which every room's devices fit.
        const needed = plan => {
            if (!plan.nodes.length) {
                return 1;
            }
            return scales.find(scale => fits(plan.content, scale, plan.availWidth, plan.availHeight)) ?? scales[scales.length - 1];
        };
        const scale = Math.min(1, ...plans.map(needed));
        lineScale = scale;

        const frames = [];
        for (const floor of graph.floors) {
            const rooms = plans.filter(plan => plan.floor === floor).map(plan => {
                return { name: plan.room.name, x: plan.room.x * gridCell, y: plan.room.y * gridCell, width: plan.room.width * gridCell, height: plan.room.height * gridCell, items: plan.content ? place(plan.content, scale, roomPadding, roomHeader) : [] };
            });
            if (rooms.length) {
                frames.push({ title: floor.name, rooms: rooms });
            }
        }
        if (unplaced.length) {
            const content = unplacedContent;
            const width = roundUp(content.width * scale + roomPadding * 2, step);
            frames.push({ title: 'Unplaced', rooms: [{ name: '', x: 0, y: 0, width: width, height: roundUp(content.height * scale + roomPadding * 2 + (mode === 'floorplan' ? 0 : portSize * scale), step), items: place(content, scale, roomPadding, roomPadding) }] });
        }
        return frames;
    };

    // Routes lines on a 4px grid. Every cable gets its own track: cables may cross but never run along each other.
    // A cable stays inside its own rooms, leaves a room only through a border that doesn't touch another room, crosses the floor straight out to the corridor around the floors, and travels there to the other room.
    const createRouter = (width, height, obstacles, rooms, frames) => {
        const cols = Math.ceil((width + margin * 2) / step) + 1;
        const rows = Math.ceil((height + margin * 2) / step) + 1;
        const cellCount = cols * rows;
        const blocked = new Uint8Array(cellCount);
        const usedH = new Uint8Array(cellCount);
        const usedV = new Uint8Array(cellCount);
        const nearH = new Uint8Array(cellCount);
        const nearV = new Uint8Array(cellCount);
        const roomOf = new Int16Array(cellCount);
        const borderOf = new Int16Array(cellCount);
        const borderAxis = new Uint8Array(cellCount);
        const inFrame = new Uint8Array(cellCount);
        const nearFrame = new Uint8Array(cellCount);
        const header = new Uint8Array(cellCount);
        const stateCount = cellCount * 16;
        const cost = new Float32Array(stateCount);
        const seen = new Uint32Array(stateCount);
        const parent = new Int32Array(stateCount);
        const vectors = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }];
        const kinds = { room: 1, border: 2, wall: 3, gap: 4, corridor: 5 };
        let stamp = 0;
        const colOf = x => Math.round((x + margin) / step);
        const rowOf = y => Math.round((y + margin) / step);
        const gridOf = box => ({ firstCol: colOf(box.x), lastCol: colOf(box.x + box.width), firstRow: rowOf(box.y), lastRow: rowOf(box.y + box.height) });
        obstacles.forEach(box => {
            const firstCol = Math.max(0, Math.floor((box.x + margin) / step));
            const lastCol = Math.min(cols - 1, Math.ceil((box.x + box.width + margin) / step));
            const firstRow = Math.max(0, Math.floor((box.y + margin) / step));
            const lastRow = Math.min(rows - 1, Math.ceil((box.y + box.height + margin) / step));
            for (let row = firstRow; row <= lastRow; row++) {
                for (let col = firstCol; col <= lastCol; col++) {
                    blocked[row * cols + col] = 1;
                }
            }
        });

        // Floor outlines, and a thin band outside them that cables avoid so they don't hug the outline.
        frames.forEach(frame => {
            const grid = gridOf(frame);
            for (let row = Math.max(0, grid.firstRow - 2); row <= Math.min(rows - 1, grid.lastRow + 2); row++) {
                for (let col = Math.max(0, grid.firstCol - 2); col <= Math.min(cols - 1, grid.lastCol + 2); col++) {
                    const inside = row >= grid.firstRow && row <= grid.lastRow && col >= grid.firstCol && col <= grid.lastCol;
                    if (inside) {
                        inFrame[row * cols + col] = 1;
                    } else {
                        nearFrame[row * cols + col] = 1;
                    }
                }
            }
        });

        // Room interiors, and their borders: a border is only a doorway where it doesn't touch another room. Corners and shared walls are solid.
        const borderCount = new Uint8Array(cellCount);
        rooms.forEach((room, index) => {
            const grid = gridOf(room);
            for (let row = grid.firstRow; row <= grid.lastRow; row++) {
                for (let col = grid.firstCol; col <= grid.lastCol; col++) {
                    const cell = row * cols + col;
                    const onRow = row === grid.firstRow || row === grid.lastRow;
                    const onCol = col === grid.firstCol || col === grid.lastCol;
                    if (!onRow && !onCol) {
                        roomOf[cell] = index + 1;
                        header[cell] = row - grid.firstRow <= roomHeader / step ? 1 : 0;
                        continue;
                    }
                    borderCount[cell]++;
                    borderOf[cell] = onRow && onCol ? -1 : index + 1;
                    borderAxis[cell] = onRow ? 1 : 2;
                }
            }
        });
        for (let cell = 0; cell < cellCount; cell++) {
            if (borderCount[cell] > 1 || (borderCount[cell] && roomOf[cell])) {
                borderOf[cell] = -1;
                roomOf[cell] = 0;
            }
        }
        const kindOf = cell => roomOf[cell] ? kinds.room : borderOf[cell] > 0 ? kinds.border : borderOf[cell] < 0 ? kinds.wall : inFrame[cell] ? kinds.gap : kinds.corridor;

        // A border is only a doorway if it sits on the outside of the floor: straight out from it, the corridor is no further than the floor's title band.
        const reach = Math.ceil(titleHeight / step) + 2;
        const doorways = [];
        for (let cell = 0; cell < cellCount; cell++) {
            if (borderOf[cell] <= 0) {
                continue;
            }
            const offset = borderAxis[cell] === 1 ? (roomOf[cell + cols] === borderOf[cell] ? -cols : cols) : (roomOf[cell + 1] === borderOf[cell] ? -1 : 1);
            let walk = cell + offset;
            let length = 0;
            while (walk >= 0 && walk < cellCount && kindOf(walk) === kinds.gap && length <= reach) {
                walk += offset;
                length++;
            }
            doorways.push({ cell: cell, open: length <= reach && walk >= 0 && walk < cellCount && kindOf(walk) === kinds.corridor });
        }
        doorways.filter(doorway => !doorway.open).forEach(doorway => {
            borderOf[doorway.cell] = -1;
        });

        // Whether a cable may step from one cell to the next, and the gap mode it is in afterwards: 1 heading out of a floor, 2 heading into one.
        const transition = (cell, dir, mode, next, nextDir, sourceRoom, targetRoom, free) => {
            const from = kindOf(cell);
            const to = kindOf(next);
            if (free) {
                return to === kinds.wall ? -1 : 0;
            }
            if ((from === kinds.border || from === kinds.gap) && nextDir !== dir) {
                return -1;
            }
            const allowed = room => room === sourceRoom + 1 || room === targetRoom + 1;
            switch (to) {
                case kinds.room:
                    return allowed(roomOf[next]) && (from === kinds.room || borderOf[cell] === roomOf[next]) ? 0 : -1;
                case kinds.border:
                    if (sourceRoom === targetRoom || !allowed(borderOf[next]) || (borderAxis[next] === 1) !== (nextDir === 0 || nextDir === 2)) {
                        return -1;
                    }
                    return from === kinds.room || from === kinds.corridor || (from === kinds.gap && mode === 2) ? 0 : -1;
                case kinds.gap:
                    return from === kinds.border ? 1 : from === kinds.gap ? mode : from === kinds.corridor ? 2 : -1;
                case kinds.corridor:
                    return from === kinds.corridor || from === kinds.border || (from === kinds.gap && mode === 1) ? 0 : -1;
                default:
                    return -1;
            }
        };

        // Level 0 keeps every rule. Level 1 lets cables share tracks if there is no other way. Level 2 also ignores rooms.
        const route = (starts, goals, sourceRoom, targetRoom, level) => {
            stamp++;
            const goalCells = new Map(goals.map(goal => [rowOf(goal.stub.y) * cols + colOf(goal.stub.x), goal]));
            const goalList = [...goalCells.keys()].map(cell => ({ col: cell % cols, row: Math.floor(cell / cols) }));
            const heap = [];
            const push = (priority, state) => {
                heap.push({ priority: priority, state: state });
                let index = heap.length - 1;
                while (index > 0) {
                    const up = (index - 1) >> 1;
                    if (heap[up].priority <= heap[index].priority) {
                        break;
                    }
                    [heap[up], heap[index]] = [heap[index], heap[up]];
                    index = up;
                }
            };
            const pop = () => {
                const top = heap[0];
                const last = heap.pop();
                if (heap.length) {
                    heap[0] = last;
                    let index = 0;
                    for (;;) {
                        const left = index * 2 + 1;
                        const right = left + 1;
                        let smallest = index;
                        if (left < heap.length && heap[left].priority < heap[smallest].priority) {
                            smallest = left;
                        }
                        if (right < heap.length && heap[right].priority < heap[smallest].priority) {
                            smallest = right;
                        }
                        if (smallest === index) {
                            break;
                        }
                        [heap[smallest], heap[index]] = [heap[index], heap[smallest]];
                        index = smallest;
                    }
                }
                return top;
            };
            const weight = level === 0 ? 1 : 1.3;
            // Distance to the box around all the goals: cheap, and never more than the real distance.
            const goalBox = { minCol: Math.min(...goalList.map(goal => goal.col)), maxCol: Math.max(...goalList.map(goal => goal.col)), minRow: Math.min(...goalList.map(goal => goal.row)), maxRow: Math.max(...goalList.map(goal => goal.row)) };
            const estimate = cell => {
                const col = cell % cols;
                const row = (cell - col) / cols;
                const dx = col < goalBox.minCol ? goalBox.minCol - col : col > goalBox.maxCol ? col - goalBox.maxCol : 0;
                const dy = row < goalBox.minRow ? goalBox.minRow - row : row > goalBox.maxRow ? row - goalBox.maxRow : 0;
                return weight * (dx + dy);
            };
            // Several cables can share a single port, so right next to a lone start or goal they may touch.
            const shared = new Set();
            [starts.length === 1 ? starts[0] : null, goals.length === 1 ? goals[0] : null].filter(anchor => anchor).forEach(anchor => {
                const col = colOf(anchor.stub.x);
                const row = rowOf(anchor.stub.y);
                for (let y = row - 2; y <= row + 2; y++) {
                    for (let x = col - 2; x <= col + 2; x++) {
                        shared.add(y * cols + x);
                    }
                }
            });
            const startByState = new Map();
            const startCells = new Set();
            starts.forEach(start => {
                const cell = rowOf(start.stub.y) * cols + colOf(start.stub.x);
                if (level === 0 && starts.length > 1 && (start.dir === 1 || start.dir === 3 ? usedH[cell] : usedV[cell])) {
                    return;
                }
                const state = (cell * 4 + start.dir) * 4;
                startCells.add(cell);
                cost[state] = start.penalty ?? 0;
                seen[state] = stamp;
                parent[state] = -1;
                startByState.set(state, start);
                push(cost[state] + estimate(cell), state);
            });
            let pops = 0;
            while (heap.length && pops < (level === 0 ? 600000 : 1500000)) {
                pops++;
                const { priority, state } = pop();
                const mode = state & 3;
                const dir = (state >> 2) & 3;
                const cell = state >> 4;
                if (priority - estimate(cell) > cost[state] + 0.001) {
                    continue;
                }
                if (goalCells.has(cell)) {
                    const cells = [];
                    let walk = state;
                    let root = state;
                    while (walk !== -1) {
                        cells.push(walk >> 4);
                        root = walk;
                        walk = parent[walk];
                    }
                    cells.reverse();
                    const points = cells.map(item => ({ x: (item % cols) * step - margin, y: Math.floor(item / cols) * step - margin }));
                    // Claim the track and the tracks either side so the next cable keeps a gap.
                    for (let i = 1; i < cells.length; i++) {
                        const horizontal = points[i].y === points[i - 1].y;
                        [cells[i - 1], cells[i]].forEach(index => {
                            const room = kindOf(index) === kinds.room;
                            [-2, -1, 0, 1, 2].map(offset => ({ offset: offset, track: index + offset * (horizontal ? cols : 1) })).filter(item => item.track >= 0 && item.track < cellCount).forEach(item => {
                                (room && Math.abs(item.offset) === 2 ? (horizontal ? nearH : nearV) : (horizontal ? usedH : usedV))[item.track] = 1;
                            });
                        });
                    }
                    return { points: points, start: startByState.get(root), goal: goalCells.get(cell) };
                }
                for (const nextDir of [dir, (dir + 1) % 4, (dir + 3) % 4]) {
                    const col = (cell % cols) + vectors[nextDir].x;
                    const row = Math.floor(cell / cols) + vectors[nextDir].y;
                    if (col < 0 || row < 0 || col >= cols || row >= rows) {
                        continue;
                    }
                    const next = row * cols + col;
                    const isGoal = goalCells.has(next);
                    // Lines leave a device straight out and arrive straight in.
                    if ((blocked[next] && !isGoal) || (isGoal && nextDir !== (goalCells.get(next).dir + 2) % 4) || (parent[state] === -1 && nextDir !== dir)) {
                        continue;
                    }
                    const nextMode = isGoal ? 0 : transition(cell, dir, mode, next, nextDir, sourceRoom, targetRoom, level === 2);
                    if (nextMode < 0) {
                        continue;
                    }
                    const horizontal = nextDir === 1 || nextDir === 3;
                    const turning = nextDir !== dir;
                    const clash = !shared.has(next) && ((horizontal ? usedH[next] : usedV[next]) || (turning && !startCells.has(cell) && (horizontal ? usedH[cell] : usedV[cell])));
                    if (clash && level === 0) {
                        continue;
                    }
                    let added = (kindOf(next) === kinds.room ? 2 : 1) + (turning ? 6 : 0) + (nearFrame[next] ? 2 : 0) + (header[next] ? 2 : 0) + (clash ? 60 : 0) + (isGoal ? goalCells.get(next).penalty ?? 0 : 0);
                    if (horizontal ? usedV[next] : usedH[next]) {
                        added += 2;
                    }
                    if (horizontal ? nearH[next] : nearV[next]) {
                        added += 3;
                    }
                    const nextState = ((next * 4 + nextDir) * 4) + nextMode;
                    const total = cost[state] + added;
                    if (seen[nextState] !== stamp || total < cost[nextState]) {
                        seen[nextState] = stamp;
                        cost[nextState] = total;
                        parent[nextState] = state;
                        push(total + estimate(next), nextState);
                    }
                }
            }
            return null;
        };
        return { route: route };
    };

    // Where a line leaves or enters a device, with a short stub clear of the device so the router starts on a free grid point.
    const withStub = anchor => {
        const snap = value => Math.round(value / step) * step;
        const stub = anchor.dir === 0 ? { x: snap(anchor.x), y: Math.floor((anchor.y - 2) / step) * step } : anchor.dir === 2 ? { x: snap(anchor.x), y: Math.ceil((anchor.y + 2) / step) * step } : anchor.dir === 1 ? { x: Math.ceil((anchor.x + 2) / step) * step, y: snap(anchor.y) } : { x: Math.floor((anchor.x - 2) / step) * step, y: snap(anchor.y) };
        return { ...anchor, stub: stub };
    };
    // Connection points on every grid line along each side of a device without ports, so each cable can land on its own spot in line with where it comes from.
    const spread = (length, start) => {
        const count = Math.max(1, Math.floor((length - 12) / step) + 1);
        const first = (length - (count - 1) * step) / 2;
        // Snap onto the routing grid so lines meet the device dead straight.
        return Array.from({ length: count }, (_, index) => Math.min(start + length - step, Math.max(start + step, Math.round((start + first + index * step) / step) * step)));
    };
    // Each side carries a cost so lines behave consistently: out of the bottom, in at the top, into the right of a stacked device. A rack is a hub, so any side will do.
    const sideAnchors = (box, role) => {
        const penalties = box.node.kind === 'rack' ? [0, 0, 0, 0] : role === 'source' ? [60, 30, 0, 30] : box.stacked ? [60, 0, 60, 60] : [0, 30, 60, 30];
        return [...spread(box.width, box.x).flatMap(x => [{ x: x, y: box.y, dir: 0 }, { x: x, y: box.y + box.height, dir: 2 }]), ...spread(box.height, box.y).flatMap(y => [{ x: box.x + box.width, y: y, dir: 1 }, { x: box.x, y: y, dir: 3 }])].map(anchor => withStub({ ...anchor, penalty: penalties[anchor.dir] }));
    };
    // Lines the first and last straight runs up with the exact anchors, so a line never kinks to reach the routing grid.
    const align = (start, path, goal) => {
        const points = path.map(point => ({ ...point }));
        const vertical = anchor => anchor.dir === 0 || anchor.dir === 2;
        const axis = anchor => vertical(anchor) ? 'x' : 'y';
        const straight = points.every(point => point[axis(start)] === points[0][axis(start)]);
        if (straight && vertical(start) === vertical(goal) && start[axis(start)] !== goal[axis(start)]) {
            // One straight run between two anchors that don't line up: put a single clean step half way.
            const other = vertical(start) ? 'y' : 'x';
            const middle = Math.round((start[other] + goal[other]) / 2);
            const first = { [axis(start)]: start[axis(start)], [other]: middle };
            const second = { [axis(start)]: goal[axis(start)], [other]: middle };
            return [start, first, second, goal];
        }
        const key = axis(start);
        for (let i = 0; i < points.length && points[i][key] === path[0][key]; i++) {
            points[i][key] = start[key];
        }
        const goalKey = axis(goal);
        for (let i = points.length - 1; i >= 0 && path[i][goalKey] === path[path.length - 1][goalKey]; i--) {
            points[i][goalKey] = goal[goalKey];
        }
        return [start, ...points, goal];
    };
    const portAnchor = (box, portId) => {
        const index = box.node.ports.findIndex(port => port.id === portId);
        if (box.compact || index < 0) {
            return null;
        }
        const top = uplinkPortIds.has(portId);
        const scale = box.scale ?? 1;
        return withStub({ x: box.x + (portX(index) + portSize / 2) * scale, y: top ? box.y - portSize * scale : box.y + box.height + portSize * scale, dir: top ? 0 : 2 });
    };

    // Routes every line, shortest first. Failures counts lines between rooms that could only be routed by bending the rules, so the caller can widen the corridors and try again.
    const routeEdges = (edges, boxes, bounds, rooms, frames) => {
        const obstacles = [...boxes.values()].map(box => {
            const reach = extents(box);
            return { x: box.x, y: box.y - reach.top, width: box.width, height: box.height + reach.top + reach.bottom };
        });
        const router = createRouter(bounds.width, bounds.height, obstacles, rooms, frames);
        const centre = box => ({ x: box.x + box.width / 2, y: box.y + box.height / 2 });
        const routable = edges.filter(edge => boxes.has(edge.source) && boxes.has(edge.target));
        const distance = edge => Math.abs(centre(boxes.get(edge.source)).x - centre(boxes.get(edge.target)).x) + Math.abs(centre(boxes.get(edge.source)).y - centre(boxes.get(edge.target)).y);
        routable.sort((a, b) => distance(a) - distance(b));
        let failures = 0;
        const routes = routable.map((edge, lane) => {
            const from = boxes.get(edge.source);
            const to = boxes.get(edge.target);
            const starts = portAnchor(from, edge.sourcePort) ? [portAnchor(from, edge.sourcePort)] : sideAnchors(from, 'source');
            const goals = portAnchor(to, edge.targetPort) ? [portAnchor(to, edge.targetPort)] : sideAnchors(to, 'target');
            let found = router.route(starts, goals, from.room, to.room, 0);
            if (!found) {
                found = router.route(starts, goals, from.room, to.room, 1);
                // Only a lack of tracks between rooms is fixed by wider corridors; a room with no doorway isn't.
                failures += found && from.room !== to.room ? 1 : 0;
                found = found ?? router.route(starts, goals, from.room, to.room, 2);
            }
            if (!found) {
                return { edge: edge, points: elbow(from, to, lane) };
            }
            return { edge: edge, points: align(found.start, found.points, found.goal) };
        });
        return { routes: routes, failures: failures };
    };

    // Plain elbow between two boxes, used if the router can't find a way through.
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
        useView(mode === 'unifi' ? raw : collapsed);
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
        const bounds = { width: 0, height: 0 };

        lineScale = 1;
        if (mode === 'unifi' || !graph.floors.length) {
            // The network on its own, laid out and routed by ELK.
            const result = await layoutGroup({ nodes: graph.nodes, edges: graph.edges });
            if (id !== renderId) {
                return;
            }
            drawGroup(result, layers);
            bounds.width = result.width;
            bounds.height = result.height;
        } else {
            // Rooms at their saved floor plan positions with the devices inside them.
            const frames = await buildFrames(mode);
            if (id !== renderId) {
                return;
            }
            // Lay the floors out with corridors around them, widening the corridors until every cable between rooms fits on its own track.
            let gap = corridor;
            for (let attempt = 0; ; attempt++) {
                Object.values(layers).forEach(layer => layer.replaceChildren());
                const boxes = new Map();
                const roomRects = [];
                const frameRects = [];
                let ox = gap;
                bounds.height = 0;
                frames.forEach(frame => {
                    const width = Math.max(...frame.rooms.map(room => room.x + room.width));
                    const height = Math.max(...frame.rooms.map(room => room.y + room.height));
                    const frameRect = { x: ox - groupPadding, y: gap, width: width + groupPadding * 2, height: titleHeight + height + groupPadding };
                    frameRects.push(frameRect);
                    drawFrame(frame.title, frameRect.x, frameRect.y, frameRect.width, frameRect.height, layers.boxes);
                    frame.rooms.forEach(room => {
                        const rect = { x: ox + room.x, y: gap + titleHeight + room.y, width: room.width, height: room.height };
                        const index = roomRects.push(rect) - 1;
                        if (room.name) {
                            drawRoom(room.name, rect.x, rect.y, rect.width, rect.height, layers.boxes);
                        }
                        room.items.forEach(item => boxes.set(item.node.id, { ...item, x: rect.x + item.x, y: rect.y + item.y, room: index }));
                    });
                    bounds.height = Math.max(bounds.height, frameRect.y + frameRect.height + gap);
                    ox += width + groupPadding * 2 + gap;
                });
                bounds.width = ox - groupPadding;
                const result = routeEdges(graph.edges, boxes, bounds, roomRects, frameRects);
                if (result.failures === 0 || attempt === 3) {
                    boxes.forEach(box => drawNode(box.node, box.x, box.y, box.nativeWidth, box.nativeHeight, layers.nodes, box.compact, box.scale));
                    result.routes.forEach(routed => drawEdge(routed.edge, routed.points, layers.edges));
                    break;
                }
                gap += 40;
            }
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
