(async function () {
    const svgNs = 'http://www.w3.org/2000/svg';
    const container = document.getElementById('map');
    const details = document.getElementById('details');
    const colours = { 'port-wan': '#1e88c8', 'port-gbe-poe': '#9a5a12', 'port-gbe': '#4a8a1c', 'port-fe': '#f6b3cf', 'port-down': '#7b8794' };
    const darkText = ['port-fe', 'port-down'];
    const portSize = 16;
    const portGap = 3;
    const padding = 12;
    const cornerRadius = 6;

    const response = await fetch(container.dataset.graphUrl);
    const graph = await response.json();
    const nodesById = new Map(graph.nodes.map(node => [node.id, node]));
    const portsById = new Map(graph.nodes.flatMap(node => node.ports.map(port => [port.id, port])));

    // Size each device to fit its name and a row of port squares along the bottom.
    const sizeOf = node => {
        const portsWidth = node.ports.length * (portSize + portGap) - portGap + padding * 2;
        return { width: Math.max(node.label.length * 7.5 + 28, node.ports.length ? portsWidth : 0), height: node.ports.length ? 44 : 32 };
    };

    // Client devices at the end of a chain are stacked in a column under their parent instead of spreading sideways.
    const laneGap = 10;
    const stackGap = 8;
    const sources = new Set(graph.edges.map(edge => edge.source));
    const incoming = new Map();
    graph.edges.filter(edge => edge.source !== edge.target).forEach(edge => incoming.set(edge.target, [...(incoming.get(edge.target) ?? []), edge]));
    const leafEdges = graph.edges.filter(edge => edge.source !== edge.target && !sources.has(edge.target) && nodesById.get(edge.target).kind === 'client' && incoming.get(edge.target).length === 1);
    const stackEdgesByParent = new Map();
    leafEdges.forEach(edge => stackEdgesByParent.set(edge.source, [...(stackEdgesByParent.get(edge.source) ?? []), edge]));
    const stacks = [...stackEdgesByParent.entries()].filter(([, edges]) => edges.length > 1).map(([parentId, edges]) => {
        edges.sort((a, b) => (portsById.get(a.sourcePort)?.index ?? 0) - (portsById.get(b.sourcePort)?.index ?? 0));
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
    const stacksById = new Map(stacks.map(stack => [stack.id, stack]));
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
            ...graph.nodes.filter(node => !stackedNodeIds.has(node.id)).map(node => {
                const size = sizeOf(node);
                return {
                    id: node.id,
                    width: size.width,
                    height: size.height,
                    layoutOptions: { 'elk.portConstraints': 'FIXED_POS' },
                    ports: [
                        { id: `${node.id}-in`, x: size.width / 2, y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } },
                        ...node.ports.map((port, index) => ({ id: port.id, x: padding + index * (portSize + portGap), y: size.height, width: portSize, height: portSize, layoutOptions: { 'elk.port.side': 'SOUTH' } }))
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
        edges: graph.edges.map(edge => {
            const stacked = stackItemsByEdge.get(edge.id);
            const target = stacked ? `${stacked.stack.id}-lane-${stacked.item.lane}` : edge.targetPort || `${edge.target}-in`;
            return { id: edge.id, sources: [edge.sourcePort], targets: [target] };
        })
    };

    const layout = await new ELK().layout(elkGraph);
    const layoutById = new Map(layout.children.map(child => [child.id, child]));

    // Build the SVG.
    const create = (name, attributes, parent) => {
        const element = document.createElementNS(svgNs, name);
        Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
        if (parent) {
            parent.appendChild(element);
        }
        return element;
    };
    const svg = create('svg', { class: 'map-svg' }, container);
    const viewport = create('g', {}, svg);
    const edgeLayer = create('g', {}, viewport);
    const nodeLayer = create('g', {}, viewport);

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

    const showDetails = (title, body) => {
        details.replaceChildren();
        const heading = document.createElement('strong');
        heading.textContent = title;
        const text = document.createElement('p');
        text.textContent = body;
        details.append(heading, text);
    };

    // Lines first so the devices sit on top of them.
    layout.edges.forEach(layoutEdge => {
        const edge = graph.edges.find(candidate => candidate.id === layoutEdge.id);
        const stacked = stackItemsByEdge.get(edge.id);
        (layoutEdge.sections || []).forEach(section => {
            const points = [section.startPoint, ...(section.bendPoints || []), section.endPoint];
            if (stacked) {
                // Continue down the lane, then turn into the side of the stacked device.
                const stackLayout = layoutById.get(stacked.stack.id);
                const centreY = stackLayout.y + stacked.item.y + stacked.item.height / 2;
                points.push({ x: section.endPoint.x, y: centreY }, { x: stackLayout.x + stacked.stack.leafWidth, y: centreY });
            }
            const d = roundedPath(points);
            const group = create('g', { class: 'map-edge' }, edgeLayer);
            create('path', { d: d, class: 'map-edge-hit' }, group);
            create('path', { d: d, class: 'map-edge-line' + (edge.manual ? ' manual' : ''), stroke: colours[edge.style] ?? '#7b8794' }, group);
            if (edge.targetPortLabel && !stacked) {
                const label = create('text', { x: section.endPoint.x + 6, y: section.endPoint.y - 6, class: 'map-edge-label' }, group);
                label.textContent = edge.targetPortLabel;
            }
            group.addEventListener('click', event => {
                event.stopPropagation();
                showDetails(`${nodesById.get(edge.source).label} → ${nodesById.get(edge.target).label} (${edge.label})`, edge.detail);
            });
        });
    });

    const drawNode = (node, x, y, width, height) => {
        const group = create('g', { class: `map-node ${node.kind}`, transform: `translate(${x}, ${y})` }, nodeLayer);
        create('rect', { width: width, height: height, rx: 8, class: 'map-node-box' }, group);
        const label = create('text', { x: width / 2, y: node.ports.length ? 18 : height / 2 + 4, class: 'map-node-label' }, group);
        label.textContent = node.label;
        group.addEventListener('click', event => {
            event.stopPropagation();
            showDetails(node.label, node.detail);
        });
        node.ports.forEach((port, index) => {
            const square = create('g', { class: 'map-port', transform: `translate(${padding + index * (portSize + portGap)}, ${height})` }, group);
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

    layout.children.forEach(layoutNode => {
        const stack = stacksById.get(layoutNode.id);
        if (stack) {
            stack.items.forEach(item => drawNode(item.node, layoutNode.x + stack.leafWidth - item.width, layoutNode.y + item.y, item.width, item.height));
        } else {
            drawNode(nodesById.get(layoutNode.id), layoutNode.x, layoutNode.y, layoutNode.width, layoutNode.height);
        }
    });

    // Pan and zoom.
    const view = { scale: 1, x: 0, y: 0 };
    const apply = () => viewport.setAttribute('transform', `translate(${view.x}, ${view.y}) scale(${view.scale})`);
    const fit = () => {
        const bounds = container.getBoundingClientRect();
        view.scale = Math.min(bounds.width / (layout.width + 40), bounds.height / (layout.height + 40), 1.5);
        view.x = (bounds.width - layout.width * view.scale) / 2;
        view.y = 20;
        apply();
    };
    fit();

    svg.addEventListener('wheel', event => {
        event.preventDefault();
        const bounds = svg.getBoundingClientRect();
        const mouseX = event.clientX - bounds.left;
        const mouseY = event.clientY - bounds.top;
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
})();
