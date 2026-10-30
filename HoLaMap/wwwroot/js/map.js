(async function () {
    const svgNs = 'http://www.w3.org/2000/svg';
    const container = document.getElementById('map');
    const details = document.getElementById('details');
    const tabs = document.getElementById('view-tabs');
    const colours = { 'port-wan': '#1e88c8', 'port-1000': '#4a8a1c', 'port-100': '#e6c229', 'port-10': '#e67e22', 'port-down': '#7b8794' };
    const darkText = ['port-100', 'port-down'];
    // Cables are drawn in a darker shade of their speed colour, and PoE cables in the full colour so they stand out.
    const darken = hex => '#' + [1, 3, 5].map(i => Math.round(parseInt(hex.substr(i, 2), 16) * 0.65).toString(16).padStart(2, '0')).join('');
    const cableColour = edge => edge.poE ? colours[edge.style] ?? '#7b8794' : darken(colours[edge.style] ?? '#7b8794');
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

    // Measure names with the same font the labels are drawn in.
    const measure = document.createElement('canvas').getContext('2d');
    const fontFamily = getComputedStyle(container).fontFamily;
    const textWidth = (text, bold) => {
        measure.font = `${bold ? 600 : 400} 12px ${fontFamily}`;
        return measure.measureText(text).width;
    };
    const isInfrastructure = node => ['gateway', 'switch', 'accesspoint', 'rack'].includes(node.kind);

    // Size each device to fit its name and a row of port squares along the bottom.
    const sizeOf = node => {
        const portsWidth = node.ports.length * (portSize + portGap) - portGap + padding * 2;
        // Racks are square so they stand out, and wide enough for a connection point per cable along each edge.
        if (node.kind === 'rack') {
            const cables = Math.max(graph.edges.filter(edge => edge.target === node.id).length, graph.edges.filter(edge => edge.source === node.id).length);
            const side = roundUp(Math.max(textWidth(node.label, true) + 32, cables * 10 + 20, 96), 8);
            return { width: side, height: side };
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
        const zigStep = 3;
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

    // Clicking a rack opens a drawing of the rack and its cables over the map.
    const rackDialog = document.getElementById('rack-dialog');
    const openRack = id => {
        const rack = raw.racks.find(item => item.id === id);
        rackDialog.querySelector('[data-title]').textContent = rack.name;
        rackDialog.querySelector('[data-edit]').href = rackDialog.dataset.rackUrl.replace(/0$/, id.substring(1));
        window.RackMap.draw(rackDialog.querySelector('[data-body]'), raw, id);
        rackDialog.showModal();
    };
    rackDialog.querySelector('[data-close]').addEventListener('click', () => rackDialog.close());
    rackDialog.addEventListener('click', event => {
        if (event.target === rackDialog) {
            rackDialog.close();
        }
    });

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
        const group = create('g', { class: 'map-edge' + (edge.online ? '' : ' offline'), 'data-edge': edge.id }, parent);
        create('path', { d: d, class: 'map-edge-hit' }, group);
        drawLine(edge, clean, 'map-edge-line', group, true);
        group.addEventListener('click', event => {
            event.stopPropagation();
            showDetails(`${nodesById.get(edge.source).label} → ${nodesById.get(edge.target).label} (${edge.label})`, edge.detail);
            focusEdges([edge]);
        });
    };

    // A PoE splitter sits inline near the end of a cable. The cable carries PoE up to it and plain Ethernet after it.
    const splitAt = (points, distance) => {
        let remaining = distance;
        for (let i = points.length - 1; i > 0; i--) {
            const from = points[i - 1];
            const to = points[i];
            const length = Math.hypot(to.x - from.x, to.y - from.y);
            if (length >= remaining && length > 0) {
                const at = { x: to.x + (from.x - to.x) * remaining / length, y: to.y + (from.y - to.y) * remaining / length };
                return { before: [...points.slice(0, i), at], after: [at, ...points.slice(i)], at: at, vertical: from.x === to.x };
            }
            remaining -= length;
        }
        return null;
    };
    const pathLength = points => points.slice(1).reduce((sum, point, index) => sum + Math.hypot(point.x - points[index].x, point.y - points[index].y), 0);
    const drawSplitter = (at, vertical, colour, parent) => {
        const group = create('g', { class: 'poe-splitter', transform: `translate(${at.x}, ${at.y})${vertical ? ' rotate(90)' : ''}` }, parent);
        create('polygon', { points: '-13,6 -7,-6 13,-6 7,6', stroke: colour }, group);
        create('path', { d: 'M -1 -4 L -4 1 L 1 1 L -2 5', stroke: colour }, group);
        create('title', {}, group).textContent = 'PoE Splitter';
    };
    // Draws a cable's line, split at its PoE splitter when it has one: full colour with the zigzag up to the splitter, the plain darker shade after it.
    const drawLine = (edge, clean, lineClass, parent, splitterAtEnd) => {
        const split = edge.splitter && splitterAtEnd !== null ? splitAt(splitterAtEnd ? clean : [...clean].reverse(), Math.min(40, pathLength(clean) * 0.3)) : null;
        if (!split) {
            create('path', { d: edge.poE && !edge.back ? zigzagPath(clean) : roundedPath(clean), class: lineClass, stroke: cableColour(edge) }, parent);
            return;
        }
        create('path', { d: edge.back ? roundedPath(split.before) : zigzagPath(split.before), class: lineClass, stroke: colours[edge.style] ?? '#7b8794' }, parent);
        create('path', { d: roundedPath(split.after), class: lineClass, stroke: cableColour({ ...edge, poE: false }) }, parent);
        drawSplitter(split.at, split.vertical, colours[edge.style] ?? '#7b8794', parent);
    };

    // A device with its ports, or just its name when compact.
    const drawNode = (node, x, y, width, height, parent, compact, scale) => {
        const group = create('g', { class: `map-node ${node.kind}${node.online ? '' : ' offline'}`, transform: `translate(${x}, ${y}) scale(${scale ?? 1})`, 'data-node': node.id }, parent);
        create('rect', { width: width, height: height, rx: compact ? 4 : 8, class: 'map-node-box' }, group);
        const label = create('text', { x: width / 2, y: !compact && node.ports.length ? 18 : height / 2 + 4, class: 'map-node-label' }, group);
        label.textContent = node.label;
        group.addEventListener('click', event => {
            event.stopPropagation();
            showDetails(node.label, node.detail);
            focusNode(node.id);
            if (node.kind === 'rack') {
                openRack(node.id);
            }
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

    // Room and floor names go on a layer above the cables, on a plate in the background colour so a cable passing behind never hides them.
    let labelLayer = null;
    const drawTitle = (text, x, y, size, plateClass, textClass, parent) => {
        if (!text) {
            return;
        }
        measure.font = `${size === 14 ? 700 : 600} ${size}px ${fontFamily}`;
        const width = measure.measureText(text).width;
        create('rect', { x: x - 4, y: y - size, width: width + 8, height: size + 6, rx: 3, class: plateClass }, labelLayer ?? parent);
        create('text', { x: x, y: y, class: textClass }, labelLayer ?? parent).textContent = text;
    };
    const drawRoom = (name, x, y, width, height, parent) => {
        create('rect', { x: x, y: y, width: width, height: height, rx: 6, class: 'map-room-box' }, parent);
        drawTitle(name, x + 8, y + 17, 12, 'map-room-plate', 'map-room-title', parent);
    };

    const drawFrame = (title, x, y, width, height, parent) => {
        create('rect', { x: x, y: y, width: width, height: height, rx: 8, class: 'map-floor-box' }, parent);
        drawTitle(title, x + groupPadding, y + 20, 14, 'map-floor-plate', 'map-floor-title', parent);
    };

    // A device drawn as just its name, used on the Floor Plan view.
    const chipSize = node => {
        if (node.kind === 'rack') {
            const side = roundUp(Math.max(textWidth(node.label, true) + 16, 64), 8);
            return { width: side, height: side };
        }
        return { width: roundUp(textWidth(node.label, isInfrastructure(node)) + 16, 8), height: chipHeight };
    };

    // Lays out a set of devices with ELK and routes their lines. Client devices at the end of a chain are stacked in a column under their parent instead of spreading sideways.
    // Compact lays devices out as name-only chips; aspect asks ELK to pack separate groups to roughly that width-to-height ratio.
    // On chips the exit point along the edge doesn't matter, so where ELK routed a cable with a sideways jog, slide its exit along the chip so it drops straight down instead.
    // All of a chip's exits move together, keeping at least 8px between them; a jog stays where the straight line would run through another box.
    const straighten = (layout, group, layoutById, stackItemsByEdge) => {
        const edgesById = new Map(group.edges.map(edge => [edge.id, edge]));
        const same = (a, b) => Math.abs(a - b) < 0.5;
        const exits = new Map();
        (layout.edges ?? []).forEach(layoutEdge => {
            const edge = edgesById.get(layoutEdge.id);
            const sections = layoutEdge.sections ?? [];
            if (!edge || sections.length !== 1) {
                return;
            }
            const section = sections[0];
            const bends = section.bendPoints ?? [];
            const source = layoutById.get(edge.source);
            const stackId = stackItemsByEdge.get(edge.id)?.stack.id;
            let wanted = null;
            if (bends.length === 2 && same(section.startPoint.x, bends[0].x) && same(bends[0].y, bends[1].y) && same(bends[1].x, section.endPoint.x) && source) {
                const x = section.endPoint.x;
                const clear = !layout.children.some(child => child.id !== edge.source && child.id !== edge.target && child.id !== stackId && x > child.x - 4 && x < child.x + child.width + 4 && child.y < section.endPoint.y && child.y + child.height > section.startPoint.y);
                wanted = x >= source.x + 4 && x <= source.x + source.width - 4 && clear ? x : null;
            }
            exits.set(edge.source, [...(exits.get(edge.source) ?? []), { section: section, wanted: wanted }]);
        });
        exits.forEach(list => {
            // Try moving every cable that wants to; drop the ones that would crowd a neighbour until the rest fit.
            let moving = list.filter(item => item.wanted !== null);
            const spaced = () => {
                const xs = list.map(item => moving.includes(item) ? item.wanted : item.section.startPoint.x).sort((a, b) => a - b);
                return xs.every((x, index) => index === 0 || x - xs[index - 1] >= 8);
            };
            while (moving.length && !spaced()) {
                const worst = moving.reduce((best, item) => Math.abs(item.wanted - item.section.startPoint.x) > Math.abs(best.wanted - best.section.startPoint.x) ? item : best);
                moving = moving.filter(item => item !== worst);
            }
            moving.forEach(item => {
                item.section.startPoint.x = item.wanted;
                item.section.bendPoints = [];
            });
        });
    };

    const layoutGroup = async (group, compact, aspect) => {
        const sizeFor = node => compact ? chipSize(node) : sizeOf(node);
        const stackEdgesByParent = new Map();
        group.edges.filter(edge => edge.source !== edge.target && !sources.has(edge.target) && nodesById.get(edge.target).kind === 'client' && incoming.get(edge.target).length === 1).forEach(edge => stackEdgesByParent.set(edge.source, [...(stackEdgesByParent.get(edge.source) ?? []), edge]));

        // A stack's lanes run down one side and each cable turns into its device from that side. Lanes on the left put the devices on the right, and the other way round.
        const makeStacks = leftSide => [...stackEdgesByParent.entries()].filter(([, edges]) => edges.length > 1).map(([parentId, edges]) => {
            edges.sort((a, b) => ((portsById.get(a.sourcePort)?.index ?? 0) - (portsById.get(b.sourcePort)?.index ?? 0)) || nodesById.get(a.target).label.localeCompare(nodesById.get(b.target).label));
            const id = `stack-${parentId}`;
            const left = leftSide.has(id);
            const leafWidth = Math.max(...edges.map(edge => sizeFor(nodesById.get(edge.target)).width));
            const lanesWidth = edges.length * laneGap + 16;
            let y = 0;
            const items = edges.map((edge, lane) => {
                const size = sizeFor(nodesById.get(edge.target));
                const item = { edge: edge, node: nodesById.get(edge.target), lane: lane, x: left ? lanesWidth : leafWidth - size.width, y: y, width: size.width, height: size.height };
                y += size.height + stackGap;
                return item;
            });
            // The top device takes the lane nearest the devices so no two cables cross.
            const laneX = lane => left ? (edges.length - 1 - lane) * laneGap + 4 : leafWidth + 16 + lane * laneGap;
            return { id: id, parentId: parentId, left: left, items: items, laneX: laneX, entryX: left ? lanesWidth : leafWidth, width: leafWidth + lanesWidth, height: y - stackGap };
        });

        const run = async leftSide => {
            const stacks = makeStacks(leftSide);
            const stackItemsByEdge = new Map(stacks.flatMap(stack => stack.items.map(item => [item.edge.id, { stack: stack, item: item }])));
            const stackedNodeIds = new Set(stacks.flatMap(stack => stack.items.map(item => item.node.id)));
            // Chips have no ports, so each cable gets its own spot along the bottom and ELK spreads them out rather than running them on top of each other.
            const outgoing = new Map();
            group.edges.forEach(edge => outgoing.set(edge.source, [...(outgoing.get(edge.source) ?? []), edge]));
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
                        if (compact) {
                            return {
                                id: node.id,
                                width: size.width,
                                height: size.height,
                                layoutOptions: { 'elk.portConstraints': 'FIXED_SIDE', 'elk.spacing.portPort': '8' },
                                ports: [{ id: `${node.id}-in`, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } }, ...(outgoing.get(node.id) ?? []).map(edge => ({ id: `${edge.id}-from`, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'SOUTH' } }))]
                            };
                        }
                        return {
                            id: node.id,
                            width: size.width,
                            height: size.height,
                            layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
                            ports: [
                                { id: `${node.id}-in`, x: size.width / 2, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } },
                                { id: `${node.id}-out`, x: size.width / 2, y: size.height, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'SOUTH' } },
                                ...node.ports.map((port, index) => ({ id: port.id, x: portX(index), y: portY(port, size.height), width: portSize, height: portSize, layoutOptions: { 'elk.port.side': uplinkPortIds.has(port.id) ? 'NORTH' : 'SOUTH' } }))
                            ]
                        };
                    }),
                    ...stacks.map(stack => ({
                        id: stack.id,
                        width: stack.width,
                        height: stack.height,
                        layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
                        ports: stack.items.map(item => ({ id: `${stack.id}-lane-${item.lane}`, x: stack.laneX(item.lane), y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } }))
                    }))
                ],
                edges: group.edges.map(edge => {
                    const stacked = stackItemsByEdge.get(edge.id);
                    const target = stacked ? `${stacked.stack.id}-lane-${stacked.item.lane}` : compact ? `${edge.target}-in` : edge.targetPort || `${edge.target}-in`;
                    return { id: edge.id, sources: [compact ? `${edge.id}-from` : edge.sourcePort || `${edge.source}-out`], targets: [target] };
                })
            };
            const layout = await new ELK().layout(elkGraph);
            return { layout: layout, stacks: stacks, stackItemsByEdge: stackItemsByEdge };
        };

        // On the Floor Plan, lay out once, then put each stack's lanes on whichever side faces the device feeding it and lay out again.
        let pass = await run(new Set());
        if (compact && pass.stacks.length) {
            const byId = new Map(pass.layout.children.map(child => [child.id, child]));
            const leftSide = new Set(pass.stacks.filter(stack => byId.has(stack.parentId) && byId.get(stack.id).x + byId.get(stack.id).width / 2 > byId.get(stack.parentId).x + byId.get(stack.parentId).width / 2).map(stack => stack.id));
            if (leftSide.size) {
                pass = await run(leftSide);
            }
        }
        const { layout, stacks, stackItemsByEdge } = pass;
        const layoutById = new Map(layout.children.map(child => [child.id, child]));
        if (compact) {
            straighten(layout, group, layoutById, stackItemsByEdge);
        }
        const placements = [];
        layout.children.forEach(child => {
            const stack = stacks.find(candidate => candidate.id === child.id);
            if (stack) {
                stack.items.forEach(item => placements.push({ node: item.node, x: child.x + item.x, y: child.y + item.y, width: item.width, height: item.height, stacked: true }));
            } else {
                placements.push({ node: nodesById.get(child.id), x: child.x, y: child.y, width: child.width, height: child.height, stacked: false });
            }
        });
        return { layout: layout, layoutById: layoutById, layoutEdges: layout.edges ?? [], placements: placements, stackItemsByEdge: stackItemsByEdge, width: layout.width, height: layout.height };
    };

    // Draws a laid-out group with the lines ELK routed, as name-only chips when compact.
    const drawGroup = (result, layers, compact) => {
        result.layoutEdges.forEach(layoutEdge => {
            const edge = graph.edges.find(candidate => candidate.id === layoutEdge.id);
            const stacked = result.stackItemsByEdge.get(edge.id);
            (layoutEdge.sections || []).forEach(section => {
                const points = [section.startPoint, ...(section.bendPoints || []), section.endPoint];
                if (stacked) {
                    // Continue down the lane, then turn into the side of the stacked device.
                    const stackLayout = result.layoutById.get(stacked.stack.id);
                    const centreY = stackLayout.y + stacked.item.y + stacked.item.height / 2;
                    points.push({ x: section.endPoint.x, y: centreY }, { x: stackLayout.x + stacked.stack.entryX, y: centreY });
                }
                drawEdge(edge, points, layers.edges);
            });
        });
        result.placements.forEach(placement => drawNode(placement.node, placement.x, placement.y, placement.width, placement.height, layers.nodes, compact));
    };

    // Highlights a device or link and everything connected to it, fading the rest. Pass nothing to clear.
    let currentSvg = null;
    const focusOn = (nodeIds, edgeIds) => {
        if (!currentSvg) {
            return;
        }
        currentSvg.classList.toggle('focusing', nodeIds.size > 0 || edgeIds.size > 0);
        currentSvg.querySelectorAll('[data-node]').forEach(element => element.classList.toggle('related', nodeIds.has(element.dataset.node)));
        currentSvg.querySelectorAll('[data-edge]').forEach(element => element.classList.toggle('related', edgeIds.has(element.dataset.edge)));
        currentSvg.querySelectorAll('[data-edges]').forEach(element => element.classList.toggle('related', element.dataset.edges.split(' ').some(edgeId => edgeIds.has(edgeId))));
    };
    const focusNode = nodeId => {
        const edges = graph.edges.filter(edge => edge.source === nodeId || edge.target === nodeId);
        focusOn(new Set([nodeId, ...edges.flatMap(edge => [edge.source, edge.target])]), new Set(edges.map(edge => edge.id)));
    };
    const focusEdges = edges => focusOn(new Set(edges.flatMap(edge => [edge.source, edge.target])), new Set(edges.map(edge => edge.id)));
    const clearFocus = () => focusOn(new Set(), new Set());

    // All view: floors contain rooms, rooms contain devices, and ELK lays out and routes the whole thing at once. Not to scale.
    const layoutNested = async () => {
        const groupOptions = { 'elk.algorithm': 'layered', 'elk.direction': 'DOWN', 'elk.edgeRouting': 'ORTHOGONAL', 'elk.hierarchyHandling': 'INCLUDE_CHILDREN', 'elk.layered.mergeHierarchyEdges': 'false', 'elk.layered.spacing.nodeNodeBetweenLayers': '50', 'elk.layered.spacing.edgeNodeBetweenLayers': '20', 'elk.layered.spacing.edgeEdgeBetweenLayers': '10', 'elk.spacing.nodeNode': '30', 'elk.spacing.edgeEdge': '10', 'elk.layered.nodePlacement.strategy': 'BRANDES_KOEPF' };
        // Devices without ports, like a rack, get a connection point per cable so ELK can spread the cables out instead of bunching them into one spot.
        const device = node => {
            const size = sizeOf(node);
            if (!node.ports.length) {
                const into = graph.edges.filter(edge => edge.target === node.id && !edge.targetPort);
                const outOf = graph.edges.filter(edge => edge.source === node.id && !edge.sourcePort);
                return {
                    id: node.id,
                    width: size.width,
                    height: size.height,
                    layoutOptions: { 'elk.portConstraints': 'FIXED_SIDE', 'elk.nodeSize.constraints': 'PORTS MINIMUM_SIZE', 'elk.nodeSize.minimum': `(${size.width}, ${size.height})`, 'elk.spacing.portPort': '10' },
                    ports: [...into.map(edge => ({ id: `${edge.id}-to`, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } })), ...outOf.map(edge => ({ id: `${edge.id}-from`, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'SOUTH' } }))]
                };
            }
            return {
                id: node.id,
                width: size.width,
                height: size.height,
                layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
                ports: [
                    { id: `${node.id}-in`, x: size.width / 2, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } },
                    { id: `${node.id}-out`, x: size.width / 2, y: size.height, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'SOUTH' } },
                    ...node.ports.map((port, index) => ({ id: port.id, x: portX(index), y: portY(port, size.height), width: portSize, height: portSize, layoutOptions: { 'elk.port.side': uplinkPortIds.has(port.id) ? 'NORTH' : 'SOUTH' } }))
                ]
            };
        };
        const room = (id, nodes) => ({ id: id, layoutOptions: { ...groupOptions, 'elk.padding': `[top=${roomHeader + 20},left=16,bottom=${portSize + 16},right=16]` }, children: nodes.map(device) });
        const floors = graph.floors.map(floor => ({
            id: `floor-${floor.id}`,
            layoutOptions: { ...groupOptions, 'elk.padding': `[top=${titleHeight + 12},left=16,bottom=16,right=16]` },
            children: floor.rooms.map(item => room(`room-${item.id}`, graph.nodes.filter(node => node.room === item.id))).filter(item => item.children.length)
        })).filter(floor => floor.children.length);
        const unplaced = graph.nodes.filter(node => !roomsById.has(node.room));
        if (unplaced.length) {
            floors.push({ id: 'floor-unplaced', layoutOptions: { ...groupOptions, 'elk.padding': `[top=${titleHeight + 12},left=16,bottom=${portSize + 16},right=16]` }, children: unplaced.map(device) });
        }
        const layout = await new ELK().layout({
            id: 'root',
            layoutOptions: groupOptions,
            children: floors,
            edges: graph.edges.map(edge => ({ id: edge.id, sources: [edge.sourcePort || (nodesById.get(edge.source).ports.length ? `${edge.source}-out` : `${edge.id}-from`)], targets: [edge.targetPort || (nodesById.get(edge.target).ports.length ? `${edge.target}-in` : `${edge.id}-to`)] }))
        });
        return layout;
    };

    const drawNested = (layout, layers) => {
        const floorNames = new Map(graph.floors.map(floor => [`floor-${floor.id}`, floor.name]));
        const roomNames = new Map(graph.floors.flatMap(floor => floor.rooms.map(item => [`room-${item.id}`, item.name])));
        // ELK gives each box relative to its parent, and each line relative to the innermost box holding both its ends.
        const origins = new Map([['root', { x: 0, y: 0 }]]);
        const chain = new Map();
        (layout.children ?? []).forEach(floor => {
            drawFrame(floorNames.get(floor.id) ?? 'Unplaced', floor.x, floor.y, floor.width, floor.height, layers.boxes);
            origins.set(floor.id, { x: floor.x, y: floor.y });
            (floor.children ?? []).forEach(child => {
                const x = floor.x + child.x;
                const y = floor.y + child.y;
                chain.set(child.id, ['root', floor.id]);
                if (roomNames.has(child.id)) {
                    origins.set(child.id, { x: x, y: y });
                    (child.children ?? []).forEach(item => chain.set(item.id, ['root', floor.id, child.id]));
                    drawRoom(roomNames.get(child.id), x, y, child.width, child.height, layers.boxes);
                    (child.children ?? []).forEach(item => drawNode(nodesById.get(item.id), x + item.x, y + item.y, item.width, item.height, layers.nodes, false));
                } else {
                    drawNode(nodesById.get(child.id), x, y, child.width, child.height, layers.nodes, false);
                }
            });
        });
        (layout.edges ?? []).forEach(layoutEdge => {
            const edge = graph.edges.find(candidate => candidate.id === layoutEdge.id);
            const from = chain.get(edge.source);
            const to = chain.get(edge.target);
            const shared = from.filter((containerId, index) => to[index] === containerId);
            const origin = origins.get(shared[shared.length - 1]);
            const shift = point => ({ x: point.x + origin.x, y: point.y + origin.y });
            (layoutEdge.sections ?? []).forEach(section => drawEdge(edge, [section.startPoint, ...(section.bendPoints ?? []), section.endPoint].map(shift), layers.edges));
        });
    };

    // Floor Plan view: rooms exactly as saved, floors side by side. Each room's devices are a small tree of name-only chips, all shrunk by the same amount so every room matches.
    // Cables between rooms aren't drawn one by one; each pair of connected rooms gets a single trunk arc showing how many cables run between them.
    const drawFloorPlan = async (layers, bounds, id) => {
        const scales = [1, 0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.25];
        const plans = [];
        const groupFor = async nodes => {
            const ids = new Set(nodes.map(node => node.id));
            return nodes.length ? layoutGroup({ nodes: nodes, edges: graph.edges.filter(edge => ids.has(edge.source) && ids.has(edge.target)) }, true, null) : null;
        };
        for (const floor of graph.floors) {
            for (const item of floor.rooms) {
                const nodes = graph.nodes.filter(node => node.room === item.id);
                const availWidth = item.width * gridCell - roomPadding * 2;
                const availHeight = item.height * gridCell - roomHeader - roomPadding;
                const ids = new Set(nodes.map(node => node.id));
                const result = nodes.length ? await layoutGroup({ nodes: nodes, edges: graph.edges.filter(edge => ids.has(edge.source) && ids.has(edge.target)) }, true, availWidth / availHeight) : null;
                plans.push({ floor: floor, room: item, key: item.id, nodes: nodes, availWidth: availWidth, availHeight: availHeight, result: result });
            }
        }
        const unplaced = graph.nodes.filter(node => !roomsById.has(node.room));
        const unplacedResult = await groupFor(unplaced);
        if (id !== renderId) {
            return false;
        }
        const scale = Math.min(1, ...plans.filter(plan => plan.result).map(plan => scales.find(candidate => plan.result.width * candidate <= plan.availWidth + 0.5 && plan.result.height * candidate <= plan.availHeight + 0.5) ?? scales[scales.length - 1]));

        // Cables between rooms, grouped by the pair of rooms they join.
        const roomKey = node => roomsById.has(node.room) ? node.room : '';
        const byPair = new Map();
        graph.edges.filter(edge => roomKey(nodesById.get(edge.source)) !== roomKey(nodesById.get(edge.target))).forEach(edge => {
            const keys = [roomKey(nodesById.get(edge.source)), roomKey(nodesById.get(edge.target))].sort();
            const key = keys.join('|');
            byPair.set(key, { keys: keys, edges: [...(byPair.get(key)?.edges ?? []), edge] });
        });
        const trunks = [...byPair.values()];
        const top = 40 + trunks.length * 14;

        // Floors side by side, then the unplaced devices, below the space the arcs need.
        const rects = new Map();
        const centres = new Map();
        let ox = groupPadding;
        const drawContent = (result, left, top) => {
            const transform = `translate(${left}, ${top}) scale(${scale})`;
            drawGroup(result, { edges: create('g', { transform: transform }, layers.edges), nodes: create('g', { transform: transform }, layers.nodes) }, true);
            result.placements.forEach(placement => centres.set(placement.node.id, { x: left + (placement.x + placement.width / 2) * scale, y: top + (placement.y + placement.height / 2) * scale }));
        };
        graph.floors.forEach(floor => {
            const floorPlans = plans.filter(plan => plan.floor === floor);
            if (!floorPlans.length) {
                return;
            }
            const width = Math.max(...floorPlans.map(plan => (plan.room.x + plan.room.width) * gridCell));
            const height = Math.max(...floorPlans.map(plan => (plan.room.y + plan.room.height) * gridCell));
            drawFrame(floor.name, ox - groupPadding, top, width + groupPadding * 2, titleHeight + height + groupPadding, layers.boxes);
            floorPlans.forEach(plan => {
                const rect = { x: ox + plan.room.x * gridCell, y: top + titleHeight + plan.room.y * gridCell, width: plan.room.width * gridCell, height: plan.room.height * gridCell };
                rects.set(plan.key, rect);
                drawRoom(plan.room.name, rect.x, rect.y, rect.width, rect.height, layers.boxes);
                if (plan.result) {
                    drawContent(plan.result, rect.x + roomPadding, rect.y + roomHeader);
                }
            });
            bounds.height = Math.max(bounds.height, top + titleHeight + height + groupPadding);
            ox += width + groupPadding * 2 + groupGap;
        });
        if (unplacedResult) {
            const rect = { x: ox, y: top + titleHeight, width: unplacedResult.width * scale + roomPadding * 2, height: unplacedResult.height * scale + roomPadding * 2 };
            rects.set('', rect);
            drawFrame('Unplaced', ox - groupPadding, top, rect.width + groupPadding * 2, titleHeight + rect.height + groupPadding, layers.boxes);
            drawContent(unplacedResult, rect.x + roomPadding, rect.y + roomPadding);
            bounds.height = Math.max(bounds.height, top + titleHeight + rect.height + groupPadding);
            ox += rect.width + groupPadding * 2 + groupGap;
        }
        bounds.width = ox - groupGap;

        // One trunk per pair of connected rooms: an arc from each room's title bar up over the floors, thicker the more cables it carries.
        const roomName = key => key ? roomsById.get(key).name : 'Unplaced';
        const anchor = key => {
            const rect = rects.get(key);
            return { x: rect.x + rect.width / 2, y: rect.y + (key ? roomHeader / 2 : roomPadding / 2) };
        };
        // Wider arcs fly higher so they nest instead of crossing.
        const ordered = trunks.map(trunk => ({ ...trunk, from: anchor(trunk.keys[0]), to: anchor(trunk.keys[1]) })).map(trunk => trunk.from.x <= trunk.to.x ? trunk : { ...trunk, from: trunk.to, to: trunk.from }).sort((a, b) => (a.to.x - a.from.x) - (b.to.x - b.from.x));
        ordered.forEach((trunk, index) => {
            const apex = top - 24 - index * 14;
            const d = `M ${trunk.from.x} ${trunk.from.y} C ${trunk.from.x} ${apex}, ${trunk.to.x} ${apex}, ${trunk.to.x} ${trunk.to.y}`;
            const group = create('g', { class: 'map-trunk', 'data-edges': trunk.edges.map(edge => edge.id).join(' ') }, layers.trunks);
            create('path', { d: d, class: 'map-trunk-hit' }, group);
            create('path', { d: d, class: 'map-trunk-line', style: `stroke-width: ${Math.min(8, 1.5 + trunk.edges.length)}px` }, group);
            create('circle', { cx: trunk.from.x, cy: trunk.from.y, r: 3, class: 'map-trunk-end' }, group);
            create('circle', { cx: trunk.to.x, cy: trunk.to.y, r: 3, class: 'map-trunk-end' }, group);
            const middle = { x: (trunk.from.x + trunk.to.x) / 2, y: 0.125 * (trunk.from.y + trunk.to.y) + 0.75 * apex };
            create('circle', { cx: middle.x, cy: middle.y, r: 9, class: 'map-trunk-badge' }, group);
            create('text', { x: middle.x, y: middle.y + 4, class: 'map-trunk-count' }, group).textContent = trunk.edges.length;
            group.addEventListener('click', event => {
                event.stopPropagation();
                const describe = edge => `${nodesById.get(edge.source).label} → ${nodesById.get(edge.target).label}`;
                showDetails(`${roomName(trunk.keys[0])} ↔ ${roomName(trunk.keys[1])} (${trunk.edges.length} ${trunk.edges.length === 1 ? 'Cable' : 'Cables'})`, trunk.edges.map(describe).join('\n'));
                focusEdges(trunk.edges);
            });
        });
        return true;
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
        const layers = { boxes: create('g', {}, viewport), trunks: create('g', {}, viewport), edges: create('g', {}, viewport), nodes: create('g', {}, viewport), labels: create('g', { class: 'map-labels' }, viewport) };
        labelLayer = layers.labels;
        const bounds = { width: 0, height: 0 };

        if (mode === 'unifi' || !graph.floors.length) {
            // The network on its own, laid out and routed by ELK.
            const result = await layoutGroup({ nodes: graph.nodes, edges: graph.edges });
            if (id !== renderId) {
                return;
            }
            drawGroup(result, layers, false);
            bounds.width = result.width;
            bounds.height = result.height;
        } else if (mode === 'all') {
            // The network grouped into floors and rooms.
            const layout = await layoutNested();
            if (id !== renderId) {
                return;
            }
            drawNested(layout, layers);
            bounds.width = layout.width;
            bounds.height = layout.height;
        } else if (!await drawFloorPlan(layers, bounds, id)) {
            return;
        }
        currentSvg = svg;

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
            if (event.target.closest('.map-node, .map-edge, .map-trunk')) {
                return;
            }
            drag = { x: event.clientX, y: event.clientY, viewX: view.x, viewY: view.y, moved: false };
            svg.setPointerCapture(event.pointerId);
        });
        svg.addEventListener('pointermove', event => {
            if (drag) {
                drag.moved = drag.moved || Math.abs(event.clientX - drag.x) + Math.abs(event.clientY - drag.y) > 4;
                view.x = drag.viewX + event.clientX - drag.x;
                view.y = drag.viewY + event.clientY - drag.y;
                apply();
            }
        });
        svg.addEventListener('pointerup', () => {
            // A click on empty space, rather than a drag, clears the highlight.
            if (drag && !drag.moved) {
                clearFocus();
                details.textContent = defaultDetails;
            }
            drag = null;
        });
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
