(async function () {
    const container = document.getElementById('map');
    const details = document.getElementById('details');
    const colours = { 'port-wan': '#1e88c8', 'port-gbe-poe': '#9a5a12', 'port-gbe': '#4a8a1c', 'port-fe': '#f6b3cf', 'port-down': '#7b8794' };

    const response = await fetch(container.dataset.graphUrl);
    const graph = await response.json();
    const roots = graph.nodes.filter(node => node.kind === 'gateway').map(node => '#' + node.id).join(', ');
    const elements = [
        ...graph.nodes.map(node => ({ data: node, classes: node.kind })),
        ...graph.edges.map(edge => ({ data: { ...edge, colour: colours[edge.style] ?? '#7b8794' }, classes: edge.manual ? 'manual' : '' }))
    ];

    const cy = cytoscape({
        container: container,
        elements: elements,
        wheelSensitivity: 0.2,
        layout: { name: 'breadthfirst', directed: true, roots: roots || undefined, spacingFactor: 1.1, padding: 20 },
        style: [
            { selector: 'node', style: { 'label': 'data(label)', 'shape': 'round-rectangle', 'width': node => Math.max(60, node.data('label').length * 7 + 20), 'height': 30, 'background-color': '#2f3640', 'border-width': 2, 'border-color': '#8a90b0', 'color': '#e6e6e6', 'font-size': 11, 'text-valign': 'center', 'text-halign': 'center' } },
            { selector: 'node.gateway, node.switch, node.accesspoint', style: { 'background-color': '#36334a', 'font-weight': 'bold' } },
            { selector: 'node:selected', style: { 'border-color': '#ffffff' } },
            { selector: 'edge', style: { 'width': 3, 'line-color': 'data(colour)', 'curve-style': 'bezier', 'control-point-step-size': 40, 'loop-direction': '90deg', 'loop-sweep': '-60deg', 'label': 'data(label)', 'font-size': 9, 'color': '#e6e6e6', 'text-background-color': '#000000', 'text-background-opacity': 0.8, 'text-background-padding': 2 } },
            { selector: 'edge.manual', style: { 'line-style': 'dashed' } },
            { selector: 'edge:selected', style: { 'width': 5 } }
        ]
    });

    cy.on('tap', 'node, edge', event => {
        const data = event.target.data();
        details.replaceChildren();
        const title = document.createElement('strong');
        title.textContent = event.target.isNode() ? data.label : `${cy.getElementById(data.source).data('label')} → ${cy.getElementById(data.target).data('label')} (${data.label})`;
        const body = document.createElement('p');
        body.textContent = data.detail;
        details.append(title, body);
    });
})();
