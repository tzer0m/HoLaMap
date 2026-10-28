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
    const chipGap = 8;
    const rowGap = 20;
    const unplacedWidth = 480;
    const laneGap = 10;
    const stackGap = 8;
    const step = 4;
    const margin = 40;
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

    // Lays out a set of devices with ELK and routes their lines. Client devices at the end of a chain are stacked in a column under their parent instead of spreading sideways.
    const layoutGroup = async group => {
        const stackEdgesByParent = new Map();
        group.edges.filter(edge => edge.source !== edge.target && !sources.has(edge.target) && nodesById.get(edge.target).kind === 'client' && incoming.get(edge.target).length === 1).forEach(edge => stackEdgesByParent.set(edge.source, [...(stackEdgesByParent.get(edge.source) ?? []), edge]));
        const stacks = [...stackEdgesByParent.entries()].filter(([, edges]) => edges.length > 1).map(([parentId, edges]) => {
            edges.sort((a, b) => ((portsById.get(a.sourcePort)?.index ?? 0) - (portsById.get(b.sourcePort)?.index ?? 0)) || nodesById.get(a.target).label.localeCompare(nodesById.get(b.target).label));
            const leafWidth = Math.max(...edges.map(edge => sizeOf(nodesById.get(edge.target)).width));
            let y = 0;
            const items = edges.map((edge, lane) => {
                const size = sizeOf(nodesById.get(edge.target));
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
                'elk.layered.considerModelOrder.strategy': 'NODES_AND_EDGES'
            },
            children: [
                ...group.nodes.filter(node => !stackedNodeIds.has(node.id)).map(node => {
                    const size = sizeOf(node);
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
                    ports: stack.items.map(item => ({ id: `${stack.id}-lane-${item.lane}`, x: stack.laneStart + item.lane * laneGap, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } }))
                }))
            ],
            edges: group.edges.map(edge => {
                const stacked = stackItemsByEdge.get(edge.id);
                const target = stacked ? `${stacked.stack.id}-lane-${stacked.item.lane}` : edge.targetPort || `${edge.target}-in`;
                return { id: edge.id, sources: [edge.sourcePort || `${edge.source}-out`], targets: [target] };
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

    // Wraps compact device boxes into rows no wider than maxWidth. Sizes are multiples of 8 so lines can land on the routing grid.
    const flowChips = (nodes, maxWidth) => {
        let x = 8;
        let y = 0;
        const sorted = [...nodes].sort((a, b) => (isInfrastructure(b) - isInfrastructure(a)) || a.label.localeCompare(b.label));
        const items = sorted.map(node => {
            const width = roundUp(textWidth(node.label, isInfrastructure(node)) + 16, 8);
            if (x > 8 && x + width > maxWidth) {
                x = 8;
                y += chipHeight + rowGap;
            }
            const item = { node: node, x: x, y: y, width: width, height: chipHeight, compact: true, stacked: false };
            x += width + chipGap;
            return item;
        });
        return { items: items, width: maxWidth, height: nodes.length ? y + chipHeight : 0 };
    };

    // How far a device reaches beyond its box: port squares hang below it, or above it for the ports it is fed from.
    const extents = item => {
        if (item.compact || !item.node.ports.length) {
            return { top: 0, bottom: 0 };
        }
        return { top: item.node.ports.some(port => uplinkPortIds.has(port.id)) ? portSize : 0, bottom: item.node.ports.some(port => !uplinkPortIds.has(port.id)) ? portSize : 0 };
    };

    // Puts the devices of one room (or the unplaced ones) in their own space.
    const contentFor = async (mode, nodes, maxWidth) => {
        if (mode === 'floorplan') {
            return flowChips(nodes, maxWidth);
        }
        if (!nodes.length) {
            return { items: [], width: 0, height: 0 };
        }
        const ids = new Set(nodes.map(node => node.id));
        const result = await layoutGroup({ nodes: nodes, edges: graph.edges.filter(edge => ids.has(edge.source) && ids.has(edge.target)) });
        const items = result.placements.map(placement => ({ ...placement, x: roundUp(placement.x, step), y: roundUp(placement.y, step), compact: false }));
        return { items: items, width: Math.max(...items.map(item => item.x + item.width)), height: Math.max(...items.map(item => item.y + item.height + extents(item).bottom)) };
    };

    // A room that grew to fit its devices can run into its neighbours, so push the later room right or down, whichever moves it less.
    const separate = rooms => {
        const gap = 24;
        for (let pass = 0; pass < rooms.length * rooms.length; pass++) {
            const ordered = [...rooms].sort((a, b) => (a.y - b.y) || (a.x - b.x));
            let moved = false;
            for (let i = 0; i < ordered.length && !moved; i++) {
                for (let j = i + 1; j < ordered.length && !moved; j++) {
                    const a = ordered[i];
                    const b = ordered[j];
                    if (a.x < b.x + b.width && b.x < a.x + a.width && a.y < b.y + b.height && b.y < a.y + a.height) {
                        const right = a.x + a.width + gap - b.x;
                        const down = a.y + a.height + gap - b.y;
                        if (right <= down) {
                            b.x += right;
                        } else {
                            b.y += down;
                        }
                        moved = true;
                    }
                }
            }
            if (!moved) {
                break;
            }
        }
    };

    // Rooms at their saved floor plan positions, floors side by side. Rooms grow if their devices don't fit.
    const buildFrames = async mode => {
        const frames = [];
        for (const floor of graph.floors) {
            const rooms = [];
            for (const room of floor.rooms) {
                const savedWidth = room.width * gridCell;
                const content = await contentFor(mode, graph.nodes.filter(node => node.room === room.id), savedWidth - 8);
                const left = mode === 'floorplan' ? 0 : roomPadding;
                const width = roundUp(Math.max(savedWidth, left + content.width + roomPadding), 8);
                const height = roundUp(Math.max(room.height * gridCell, roomHeader + content.height + roomPadding + portSize), 8);
                rooms.push({ name: room.name, x: room.x * gridCell, y: room.y * gridCell, width: width, height: height, items: content.items.map(item => ({ ...item, x: item.x + left, y: item.y + roomHeader })) });
            }
            if (rooms.length) {
                separate(rooms);
                frames.push({ title: floor.name, rooms: rooms });
            }
        }
        const unplaced = graph.nodes.filter(node => !roomsById.has(node.room));
        if (unplaced.length) {
            const content = await contentFor(mode, unplaced, unplacedWidth);
            const left = mode === 'floorplan' ? 0 : roomPadding;
            frames.push({ title: 'Unplaced', rooms: [{ name: '', x: 0, y: 0, width: roundUp(Math.max(mode === 'floorplan' ? unplacedWidth : 0, left + content.width + roomPadding), 8), height: roundUp(content.height + portSize + roomPadding, 8), items: content.items.map(item => ({ ...item, x: item.x + left, y: item.y + 8 })) }] });
        }
        return frames;
    };

    // Routes lines on a 4px grid around the devices, spreading them out so they don't run on top of each other.
    const createRouter = (width, height, obstacles) => {
        const cols = Math.ceil((width + margin * 2) / step) + 1;
        const rows = Math.ceil((height + margin * 2) / step) + 1;
        const blocked = new Uint8Array(cols * rows);
        const usedH = new Int32Array(cols * rows).fill(-1);
        const usedV = new Int32Array(cols * rows).fill(-1);
        const stateCount = cols * rows * 4;
        const cost = new Float32Array(stateCount);
        const seen = new Uint32Array(stateCount);
        const parent = new Int32Array(stateCount);
        const vectors = [{ x: 0, y: -1 }, { x: 1, y: 0 }, { x: 0, y: 1 }, { x: -1, y: 0 }];
        let stamp = 0;
        const colOf = x => Math.round((x + margin) / step);
        const rowOf = y => Math.round((y + margin) / step);
        obstacles.forEach(box => {
            const firstCol = Math.max(0, Math.floor((box.x - step + margin) / step) + 1);
            const lastCol = Math.min(cols - 1, Math.ceil((box.x + box.width + step + margin) / step) - 1);
            const firstRow = Math.max(0, Math.floor((box.y - step + margin) / step) + 1);
            const lastRow = Math.min(rows - 1, Math.ceil((box.y + box.height + step + margin) / step) - 1);
            for (let row = firstRow; row <= lastRow; row++) {
                for (let col = firstCol; col <= lastCol; col++) {
                    blocked[row * cols + col] = 1;
                }
            }
        });

        // Binary heap of [priority, state] pairs.
        const route = (starts, goals, owner) => {
            stamp++;
            const goalCells = new Map(goals.map(goal => [rowOf(goal.stub.y) * cols + colOf(goal.stub.x), goal]));
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
            const estimate = cell => {
                const col = cell % cols;
                const row = (cell - col) / cols;
                let best = Infinity;
                goalCells.forEach((goal, goalCell) => {
                    const goalCol = goalCell % cols;
                    best = Math.min(best, Math.abs(goalCol - col) + Math.abs((goalCell - goalCol) / cols - row));
                });
                return best;
            };
            const startByState = new Map();
            starts.forEach(start => {
                const cell = rowOf(start.stub.y) * cols + colOf(start.stub.x);
                const state = cell * 4 + start.dir;
                cost[state] = 0;
                seen[state] = stamp;
                parent[state] = -1;
                startByState.set(state, start);
                push(estimate(cell), state);
            });
            let pops = 0;
            while (heap.length && pops < 400000) {
                pops++;
                const { priority, state } = pop();
                const cell = state >> 2;
                const dir = state & 3;
                if (priority - estimate(cell) > cost[state] + 0.001) {
                    continue;
                }
                if (goalCells.has(cell)) {
                    const cells = [];
                    let walk = state;
                    let root = state;
                    while (walk !== -1) {
                        cells.push(walk >> 2);
                        root = walk;
                        walk = parent[walk];
                    }
                    cells.reverse();
                    const start = startByState.get(root);
                    const points = cells.map(item => ({ x: (item % cols) * step - margin, y: Math.floor(item / cols) * step - margin }));
                    for (let i = 1; i < cells.length; i++) {
                        const horizontal = points[i].y === points[i - 1].y;
                        const index = cells[i];
                        const previous = cells[i - 1];
                        if (horizontal) {
                            usedH[index] = owner;
                            usedH[previous] = owner;
                        } else {
                            usedV[index] = owner;
                            usedV[previous] = owner;
                        }
                    }
                    return { points: points, start: start, goal: goalCells.get(cell) };
                }
                for (const nextDir of [dir, (dir + 1) % 4, (dir + 3) % 4]) {
                    const col = (cell % cols) + vectors[nextDir].x;
                    const row = Math.floor(cell / cols) + vectors[nextDir].y;
                    if (col < 0 || row < 0 || col >= cols || row >= rows) {
                        continue;
                    }
                    const next = row * cols + col;
                    if (blocked[next] && !goalCells.has(next)) {
                        continue;
                    }
                    let added = 1 + (nextDir === dir ? 0 : 4);
                    const horizontal = nextDir === 1 || nextDir === 3;
                    const same = horizontal ? usedH[next] : usedV[next];
                    const cross = horizontal ? usedV[next] : usedH[next];
                    if (same !== -1 && same !== owner) {
                        added += 10;
                    }
                    if (cross !== -1) {
                        added += 2;
                    }
                    const nextState = next * 4 + nextDir;
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
    const withStub = anchor => ({ ...anchor, stub: { x: anchor.x + [0, 1, 0, -1][anchor.dir] * step, y: anchor.y + [-1, 0, 1, 0][anchor.dir] * step } });
    const sideAnchors = box => [{ x: box.x + box.width / 2, y: box.y, dir: 0 }, { x: box.x + box.width, y: box.y + box.height / 2, dir: 1 }, { x: box.x + box.width / 2, y: box.y + box.height, dir: 2 }, { x: box.x, y: box.y + box.height / 2, dir: 3 }].map(withStub);
    const portAnchor = (box, portId) => {
        const index = box.node.ports.findIndex(port => port.id === portId);
        if (box.compact || index < 0) {
            return null;
        }
        const top = uplinkPortIds.has(portId);
        return withStub({ x: box.x + portX(index) + portSize / 2, y: top ? box.y - portSize : box.y + box.height + portSize, dir: top ? 0 : 2 });
    };

    const routeEdges = (edges, boxes, bounds) => {
        const obstacles = [...boxes.values()].map(box => {
            const reach = extents(box);
            return { x: box.x, y: box.y - reach.top, width: box.width, height: box.height + reach.top + reach.bottom };
        });
        const router = createRouter(bounds.width, bounds.height, obstacles);
        const owners = new Map();
        const centre = box => ({ x: box.x + box.width / 2, y: box.y + box.height / 2 });
        const routable = edges.filter(edge => boxes.has(edge.source) && boxes.has(edge.target));
        const distance = edge => Math.abs(centre(boxes.get(edge.source)).x - centre(boxes.get(edge.target)).x) + Math.abs(centre(boxes.get(edge.source)).y - centre(boxes.get(edge.target)).y);
        routable.sort((a, b) => distance(a) - distance(b));
        return routable.map((edge, lane) => {
            const from = boxes.get(edge.source);
            const to = boxes.get(edge.target);
            if (!owners.has(edge.sourcePort)) {
                owners.set(edge.sourcePort, owners.size);
            }
            const starts = portAnchor(from, edge.sourcePort) ? [portAnchor(from, edge.sourcePort)] : sideAnchors(from);
            const goals = portAnchor(to, edge.targetPort) ? [portAnchor(to, edge.targetPort)] : to.stacked ? [withStub({ x: to.x + to.width, y: to.y + to.height / 2, dir: 1 })] : to.compact || from === to ? sideAnchors(to) : [withStub({ x: to.x + to.width / 2, y: to.y, dir: 0 })];
            const found = router.route(starts, goals, owners.get(edge.sourcePort));
            if (!found) {
                return { edge: edge, points: elbow(from, to, lane) };
            }
            return { edge: edge, points: [found.start, found.start.stub, ...found.points, found.goal.stub, found.goal] };
        });
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
            const boxes = new Map();
            let ox = groupPadding;
            frames.forEach(frame => {
                const width = Math.max(...frame.rooms.map(room => room.x + room.width));
                const height = Math.max(...frame.rooms.map(room => room.y + room.height));
                drawFrame(frame.title, ox - groupPadding, width + groupPadding * 2, titleHeight + height + groupPadding, layers.boxes);
                frame.rooms.forEach(room => {
                    if (room.name) {
                        drawRoom(room.name, ox + room.x, titleHeight + room.y, room.width, room.height, layers.boxes);
                    }
                    room.items.forEach(item => boxes.set(item.node.id, { ...item, x: ox + room.x + item.x, y: titleHeight + room.y + item.y }));
                });
                bounds.height = Math.max(bounds.height, titleHeight + height + groupPadding);
                ox += width + groupPadding * 2 + groupGap;
            });
            bounds.width = ox - groupGap;
            boxes.forEach(box => drawNode(box.node, box.x, box.y, box.width, box.height, layers.nodes, box.compact));
            routeEdges(graph.edges, boxes, bounds).forEach(routed => drawEdge(routed.edge, routed.points, layers.edges));
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
