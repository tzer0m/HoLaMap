(async function () {
    const svgNs = 'http://www.w3.org/2000/svg';
    const container = document.getElementById('map');
    const details = document.getElementById('details');
    const tabs = document.getElementById('view-tabs');
    const colours = { 'port-wan': '#1e88c8', 'port-1000': '#4a8a1c', 'port-100': '#e6c229', 'port-10': '#e67e22', 'port-down': '#7b8794', 'wifi': '#9b6bd1', 'zigbee': '#2bb5a8' };
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
    const isInfrastructure = node => ['gateway', 'switch', 'accesspoint', 'rack', 'coordinator', 'router'].includes(node.kind);

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

    // The details box: a heading with badges, then labelled rows and lists for whatever was clicked.
    const rawEdgesById = new Map(raw.edges.map(edge => [edge.id, edge]));
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    const kindNames = { gateway: 'Gateway', switch: 'Switch', accesspoint: 'Access Point', client: 'Client', rack: 'Rack' };
    const element = (tag, className, text) => {
        const item = document.createElement(tag);
        if (className) {
            item.className = className;
        }
        if (text !== undefined && text !== null) {
            item.textContent = text;
        }
        return item;
    };
    const button = (text, onClick, className) => {
        const item = element('button', className ?? 'details-link', text);
        item.type = 'button';
        item.addEventListener('click', onClick);
        return item;
    };
    const heading = (title, badges) => {
        details.replaceChildren();
        const head = element('div', 'details-head');
        head.append(element('strong', null, title));
        const tags = element('div', 'details-badges');
        badges.filter(badge => badge).forEach(([text, className]) => tags.append(element('span', `details-badge ${className ?? ''}`, text)));
        head.append(tags);
        details.append(head);
    };
    const section = title => {
        const part = element('section', 'details-section');
        if (title) {
            part.append(element('h3', null, title));
        }
        details.append(part);
        return part;
    };
    const rows = (parent, pairs) => {
        const list = element('dl', 'details-rows');
        pairs.filter(([, value]) => value !== null && value !== undefined && value !== '').forEach(([label, value]) => {
            list.append(element('dt', null, label));
            const cell = element('dd');
            if (value instanceof Node) {
                cell.append(value);
            } else {
                cell.textContent = value;
            }
            list.append(cell);
        });
        parent.append(list);
    };
    const copyable = text => {
        const wrap = element('span', 'details-copy', text);
        const copy = button('Copy', () => navigator.clipboard?.writeText(text).then(() => {
            copy.textContent = 'Copied';
            setTimeout(() => copy.textContent = 'Copy', 1200);
        }), 'details-mini');
        wrap.append(copy);
        return wrap;
    };
    const post = async (url, values) => {
        const response = await fetch(url, { method: 'POST', headers: { RequestVerificationToken: token }, body: new URLSearchParams(values) });
        if (!response.ok) {
            throw new Error(`Saving failed: ${response.status}`);
        }
    };
    const speedText = port => !port ? '' : !port.up ? 'Down' : port.speed >= 1000 ? `${port.speed / 1000} Gbps` : port.speed ? `${port.speed} Mbps` : 'Up';
    const poeText = port => port?.poEPower ? `${port.poEPower.toFixed(1)} W` : port?.poECapable ? 'Available' : '';
    const vlanText = port => port ? [port.network ? `${port.network} (Untagged)` : null, ...(port.tagged ?? []).map(name => `${name} (Tagged)`)].filter(part => part).join(', ') : '';
    const endText = (nodeId, portId) => {
        const port = portsById.get(portId);
        return `${rawById.get(nodeId)?.label ?? 'Unknown'}${port ? ` Port ${port.index}` : ''}`;
    };
    const deviceButton = nodeId => button(rawById.get(nodeId)?.label ?? raw.racks.find(rack => rack.id === nodeId)?.name ?? 'Unknown', () => {
        showDevice(nodeId);
        // In a view where racks are drawn as one block, a racked device lights up its rack.
        focusNode(nodesById.has(nodeId) ? nodeId : rackOf.get(nodeId) ?? nodeId);
    });
    const edgeButton = (edge, text) => button(text, () => {
        showEdge(edge.id);
        focusEdges([graph.edges.find(item => item.id === edge.id) ?? edge]);
    });
    const meta = (parent, text) => {
        if (text) {
            parent.append(element('span', 'details-meta', text));
        }
    };

    // The route back to the gateway, one hop per line, preferring cables over Wi-Fi.
    const upstream = nodeId => {
        const hops = [];
        const seen = new Set([nodeId]);
        let current = nodeId;
        while (rawById.get(current)?.kind !== 'gateway') {
            const into = raw.edges.filter(edge => edge.target === current && edge.source !== current);
            const edge = into.find(item => !item.wireless) ?? into[0];
            if (!edge || seen.has(edge.source)) {
                break;
            }
            hops.unshift(edge);
            seen.add(edge.source);
            current = edge.source;
        }
        return hops;
    };

    // Patch panel ports a cable passes through, found from what's plugged into each panel.
    const patchHops = edge => raw.racks.flatMap(rack => (rack.panels ?? []).filter(panel => panel.kind === 'patch').flatMap(panel => panel.patches.filter(patch => patch.node && [[edge.source, edge.sourcePort], [edge.target, edge.targetPort]].some(([node, port]) => patch.node === node && (!patch.port || !port || patch.port === port))).map(patch => `${rack.name} · ${panel.name} Port ${patch.number} ${patch.side === 'back' ? 'Rear' : 'Front'}`)));

    const showRack = rackId => {
        const rack = raw.racks.find(item => item.id === rackId);
        heading(rack.name, [['Rack'], rack.online ? ['Online', 'online'] : ['Offline', 'offline']]);
        rows(section(), [['Room', roomsById.get(rack.room)?.name ?? ''], ['Height', `${rack.heightU}U`], ['Rack Map', button('Open Rack Map', () => openRack(rackId))]]);
        const list = element('ul', 'details-list');
        raw.nodes.filter(node => node.rack === rackId).sort((a, b) => b.rackU - a.rackU).forEach(node => {
            const item = element('li');
            item.append(deviceButton(node.id));
            meta(item, node.rackU ? `U${node.rackU}` : 'Not Placed');
            list.append(item);
        });
        section('Devices').append(list);
    };

    // Zigbee view: the mesh from Home Assistant's ZHA, fetched the first time the tab is opened.
    let zigbee = null;
    const zigbeeById = new Map();
    const roleNames = { coordinator: 'Coordinator', router: 'Router', client: 'End Device' };
    const zigbeeButton = nodeId => button(zigbeeById.get(nodeId)?.label ?? 'Unknown', () => {
        showDevice(nodeId);
        focusNode(nodeId);
    });
    const lqiText = lqi => lqi === null || lqi === undefined ? '' : `${lqi} (${lqi >= 150 ? 'Good' : lqi >= 80 ? 'Fair' : 'Poor'})`;
    const showZigbee = nodeId => {
        const node = zigbeeById.get(nodeId);
        heading(node.label, [[roleNames[node.kind] ?? node.kind], node.online ? ['Online', 'online'] : ['Offline', 'offline']]);
        let room = roomsById.get(node.room)?.name ?? 'No Room';
        if (details.dataset.zigbeeRoomUrl) {
            room = element('select', 'details-select');
            room.append(new Option('No Room', ''));
            raw.floors.forEach(floor => {
                const group = element('optgroup');
                group.label = floor.name;
                floor.rooms.forEach(item => group.append(new Option(item.name, item.id.substring(1), false, item.id === node.room)));
                room.append(group);
            });
            room.addEventListener('change', async () => {
                await post(details.dataset.zigbeeRoomUrl, { id: nodeId.substring(1), roomId: room.value });
                location.reload();
            });
        }
        rows(section(), [['Manufacturer', node.manufacturer], ['Model', node.model], ['IEEE', node.mac ? copyable(node.mac) : ''], ['Power', node.power], ['LQI', lqiText(node.lqi)], ['RSSI', node.rssi ? `${node.rssi} dBm` : ''], ['Room', room], ['Last Seen', node.lastSeen]]);
        const hops = [];
        for (let current = nodeId, guard = 0; guard < 50; guard++) {
            const edge = zigbee.edges.find(item => item.target === current);
            if (!edge) {
                break;
            }
            hops.unshift(edge);
            current = edge.source;
        }
        if (hops.length) {
            const list = element('ol', 'details-list');
            hops.forEach(edge => {
                const item = element('li');
                item.append(zigbeeButton(edge.source), document.createTextNode(' → '), zigbeeButton(edge.target));
                meta(item, edge.lqi ? `LQI ${edge.lqi}` : edge.label);
                list.append(item);
            });
            section('Route to Coordinator').append(list);
        }
        const children = zigbee.edges.filter(edge => edge.source === nodeId);
        if (children.length) {
            const list = element('ul', 'details-list');
            children.forEach(edge => {
                const item = element('li');
                item.append(zigbeeButton(edge.target));
                meta(item, [roleNames[zigbeeById.get(edge.target)?.kind], edge.lqi ? `LQI ${edge.lqi}` : ''].filter(part => part).join(' · '));
                list.append(item);
            });
            section('Connected Devices').append(list);
        }
    };

    // Services view: what each host reports running, fetched the first time the tab is opened.
    let services = null;
    let serviceLinks = [];
    const servicesById = new Map();
    const serviceGroupId = (hostId, group) => `group:${hostId}:${group}`;
    const serviceKindNames = { Systemd: 'Systemd', Docker: 'Docker', TrueNasApp: 'TrueNAS App', PythonScript: 'Python Script', ApiRoute: 'API Route', Cloud: 'Cloud', Device: 'Device', Other: 'Other' };
    const showService = nodeId => {
        const service = servicesById.get(nodeId);
        heading(service.label, [[serviceKindNames[service.kind] ?? service.kind], service.online ? ['Reported', 'online'] : ['Missing', 'offline']]);
        rows(section(), [['Unit', service.name !== service.label ? service.name : ''], ['Host', rawById.has(service.host.id) ? deviceButton(service.host.id) : service.host.label], ['Category', service.category], ['Compose Project', service.group], ['Owner', service.mine ? 'Mine' : 'Third Party'], ['Source', service.manual ? 'Added by Hand' : 'Reported by Host'], ['Last Reported', service.lastSeen]]);
        const list = (title, links, other) => {
            if (!links.length) {
                return;
            }
            const items = element('ul', 'details-list');
            links.forEach(link => {
                const item = element('li');
                item.append(serviceButton(other(link)));
                meta(item, link.notes);
                items.append(item);
            });
            section(title).append(items);
        };
        list('Uses', serviceLinks.filter(link => link.source === nodeId), link => link.target);
        list('Used By', serviceLinks.filter(link => link.target === nodeId), link => link.source);
    };
    const serviceButton = serviceId => {
        const service = servicesById.get(serviceId);
        return button(service ? `${service.label} (${service.host.label})` : 'Unknown', () => {
            showService(serviceId);
            focusNode(serviceId);
        });
    };
    const showServiceLink = linkId => {
        const link = serviceLinks.find(item => item.id === linkId);
        heading(`${servicesById.get(link.source)?.label ?? 'Unknown'} → ${servicesById.get(link.target)?.label ?? 'Unknown'}`, [['Link']]);
        rows(section(), [['Service', serviceButton(link.source)], ['Uses', serviceButton(link.target)], ['Notes', link.notes]]);
    };

    const showDevice = nodeId => {
        if (servicesById.has(nodeId)) {
            showService(nodeId);
            return;
        }
        if (zigbeeById.has(nodeId) && !rawById.has(nodeId)) {
            showZigbee(nodeId);
            return;
        }
        if (nodeId.startsWith('k')) {
            showRack(nodeId);
            return;
        }
        const node = rawById.get(nodeId);
        if (!node) {
            return;
        }
        heading(node.label, [[kindNames[node.kind] ?? node.kind], node.online ? ['Online', 'online'] : ['Offline', 'offline']]);

        // Where it is, with a picker to move it to another room. A device in a rack moves with its rack.
        const rack = raw.racks.find(item => item.id === node.rack);
        let room = roomsById.get(node.room)?.name ?? 'No Room';
        if (!rack && details.dataset.roomUrl) {
            room = element('select', 'details-select');
            room.append(new Option('No Room', ''));
            raw.floors.forEach(floor => {
                const group = element('optgroup');
                group.label = floor.name;
                floor.rooms.forEach(item => group.append(new Option(item.name, item.id.substring(1), false, item.id === node.room)));
                room.append(group);
            });
            room.addEventListener('change', async () => {
                await post(details.dataset.roomUrl, { id: nodeId.substring(1), roomId: room.value });
                location.reload();
            });
        }
        rows(section(), [
            ['Model', node.model],
            ['IP', node.ip ? copyable(node.ip) : ''],
            ['MAC', node.mac ? copyable(node.mac) : ''],
            ['Room', room],
            ['Rack', rack ? (() => {
                const wrap = element('span');
                wrap.append(button(rack.name, () => openRack(rack.id)));
                meta(wrap, node.rackU ? `U${node.rackU}` : 'Not Placed');
                return wrap;
            })() : ''],
            ['Last Seen', node.lastSeen]
        ]);

        const wifi = raw.edges.find(edge => edge.wireless && edge.target === nodeId);
        if (wifi) {
            rows(section('Wi-Fi'), [['Access Point', deviceButton(wifi.source)], ['SSID', wifi.ssid], ['Band', wifi.band], ['Channel', wifi.channel ?? ''], ['Signal', wifi.signal ? `${wifi.signal} dBm` : ''], ['Rate', wifi.rate ? `${wifi.rate} Mbps` : '']]);
        }

        const hops = upstream(nodeId);
        if (hops.length) {
            const list = element('ol', 'details-list');
            hops.forEach(edge => {
                const item = element('li');
                item.append(edgeButton(edge, `${endText(edge.source, edge.sourcePort)} → ${endText(edge.target, edge.targetPort)}`));
                meta(item, edge.wireless ? 'Wi-Fi' : speedText(portsById.get(edge.sourcePort)));
                list.append(item);
            });
            section('Upstream').append(list);
        }

        const connections = raw.edges.filter(edge => edge.source === nodeId || edge.target === nodeId);
        if (connections.length) {
            const list = element('ul', 'details-list');
            connections.forEach(edge => {
                const outgoing = edge.source === nodeId;
                const mine = portsById.get(outgoing ? edge.sourcePort : edge.targetPort);
                const item = element('li');
                item.append(edgeButton(edge, `${mine ? `Port ${mine.index} ` : ''}${outgoing ? '→' : '←'} ${endText(outgoing ? edge.target : edge.source, outgoing ? edge.targetPort : edge.sourcePort)}`));
                const port = portsById.get(edge.sourcePort);
                meta(item, edge.wireless ? `Wi-Fi${edge.band ? ` · ${edge.band}` : ''}` : [speedText(port), port?.poEPower ? `${port.poEPower.toFixed(1)} W` : '', edge.splitter ? 'Splitter' : ''].filter(part => part).join(' · '));
                list.append(item);
            });
            section('Connections').append(list);
        }

        if (node.ports.length) {
            const table = element('table', 'details-table');
            const head = element('tr');
            ['Port', 'Speed', 'PoE', 'VLANs', 'Connected To'].forEach(text => head.append(element('th', null, text)));
            table.append(head);
            node.ports.forEach(port => {
                const line = element('tr', port.up ? '' : 'down');
                line.append(element('td', null, port.index), element('td', null, speedText(port)), element('td', null, port.poEPower ? `${port.poEPower.toFixed(1)} W` : ''), element('td', null, `${port.network ?? ''}${port.tagged?.length ? ` +${port.tagged.length}` : ''}`));
                const connected = element('td');
                raw.edges.filter(edge => edge.sourcePort === port.id || edge.targetPort === port.id).forEach(edge => connected.append(deviceButton(edge.sourcePort === port.id ? edge.target : edge.source)));
                line.append(connected);
                line.querySelector('td:nth-child(4)').title = vlanText(port);
                table.append(line);
            });
            section('Ports').append(table);
        }
    };

    const showEdge = edgeId => {
        if (edgeId.startsWith('zl') && zigbee) {
            const link = zigbee.edges.find(item => item.id === edgeId);
            heading(`${zigbeeById.get(link.source)?.label} → ${zigbeeById.get(link.target)?.label}`, [[link.zigbee ? 'Zigbee' : 'USB'], link.online ? ['Online', 'online'] : ['Offline', 'offline']]);
            rows(section(), [['From', zigbeeButton(link.source)], ['To', zigbeeButton(link.target)], ['LQI', lqiText(link.lqi)], ['Notes', link.detail]]);
            return;
        }
        const edge = rawEdgesById.get(edgeId);
        if (!edge) {
            return;
        }
        if (edge.wireless) {
            heading(`${rawById.get(edge.source)?.label} → ${rawById.get(edge.target)?.label}`, [['Wi-Fi'], edge.online ? ['Online', 'online'] : ['Offline', 'offline']]);
            rows(section(), [['Access Point', deviceButton(edge.source)], ['Device', deviceButton(edge.target)], ['SSID', edge.ssid], ['Band', edge.band], ['Channel', edge.channel ?? ''], ['Signal', edge.signal ? `${edge.signal} dBm` : ''], ['Rate', edge.rate ? `${edge.rate} Mbps` : '']]);
            return;
        }
        const from = portsById.get(edge.sourcePort);
        heading(`${endText(edge.source, edge.sourcePort)} → ${endText(edge.target, edge.targetPort)}`, [[edge.manual ? 'Manual' : 'UniFi'], edge.online ? ['Online', 'online'] : ['Offline', 'offline']]);
        const end = (nodeId, portId) => {
            const wrap = element('span');
            wrap.append(deviceButton(nodeId));
            const port = portsById.get(portId);
            meta(wrap, port ? `Port ${port.index}${port.name ? ` · ${port.name}` : ''}` : '');
            return wrap;
        };
        let splitter = edge.splitter ? 'Yes' : 'No';
        if (edge.id.startsWith('l') && details.dataset.splitterUrl) {
            splitter = element('span');
            splitter.append(edge.splitter ? 'Yes ' : 'No ');
            splitter.append(button(edge.splitter ? 'Remove' : 'Add', async () => {
                await post(details.dataset.splitterUrl, { id: edge.id.substring(1) });
                location.reload();
            }, 'details-mini'));
        }
        const hops = patchHops(edge);
        rows(section(), [
            ['From', end(edge.source, edge.sourcePort)],
            ['To', end(edge.target, edge.targetPort)],
            ['Speed', speedText(from)],
            ['PoE', poeText(from) || 'No'],
            ['PoE Splitter', splitter],
            ['VLANs', vlanText(from)],
            ['Patch Panel', hops.length ? hops.join('\n') : ''],
            ['Notes', edge.notes]
        ]);
    };

    const showPort = (nodeId, portId) => {
        const port = portsById.get(portId);
        const node = rawById.get(nodeId);
        heading(`${node?.label} Port ${port.index}`, [[port.up ? speedText(port) : 'Down', port.up ? 'online' : 'offline']]);
        const connected = element('span');
        raw.edges.filter(edge => edge.sourcePort === portId || edge.targetPort === portId).forEach(edge => connected.append(edgeButton(edge, endText(edge.sourcePort === portId ? edge.target : edge.source, edge.sourcePort === portId ? edge.targetPort : edge.sourcePort))));
        rows(section(), [['Device', deviceButton(nodeId)], ['Name', port.name], ['Speed', speedText(port)], ['PoE', poeText(port)], ['Untagged VLAN', port.network], ['Tagged VLANs', (port.tagged ?? []).join(', ')], ['Connected To', connected.childNodes.length ? connected : '']]);
    };

    // A group of cables or Wi-Fi links, like an arc between rooms.
    const showEdges = (title, edges) => {
        heading(title, [[`${edges.length} ${edges.length === 1 ? 'Link' : 'Links'}`]]);
        const list = element('ul', 'details-list');
        edges.forEach(edge => {
            const item = element('li');
            item.append(edgeButton(edge, edge.zigbee ? `${zigbeeById.get(edge.source)?.label} → ${zigbeeById.get(edge.target)?.label}` : `${endText(edge.source, edge.sourcePort)} → ${endText(edge.target, edge.targetPort)}`));
            meta(item, edge.zigbee ? edge.lqi ? `LQI ${edge.lqi}` : 'Zigbee' : edge.wireless ? 'Wi-Fi' : speedText(portsById.get(edge.sourcePort)));
            list.append(item);
        });
        section().append(list);
    };

    // A halo is a wider copy of a cable or device in the room colour, drawn under everything else but over the arcs, so an arc passing behind disappears instead of crossing it.
    const drawEdge = (edge, points, parent, halo) => {
        const clean = tidy(points);
        if (clean.length < 2) {
            return;
        }
        if (halo) {
            create('path', { d: roundedPath(clean), class: 'map-halo' }, halo);
        }
        const d = roundedPath(clean);
        const group = create('g', { class: 'map-edge' + (edge.online ? '' : ' offline') + (edge.tagged ? ' tagged' : '') + (edge.zigbee ? ' zigbee' : ''), 'data-edge': edge.id }, parent);
        create('path', { d: d, class: 'map-edge-hit' }, group);
        drawLine(edge, clean, 'map-edge-line', group, true);
        group.addEventListener('click', event => {
            event.stopPropagation();
            showEdge(edge.id);
            focusEdges([edge]);
        });
    };

    // A gentle wave along each straight run, used for Wi-Fi connections.
    const wavePath = points => {
        const amplitude = 2.5;
        const halfWave = 5;
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
            const steps = Math.max(1, Math.round(length / halfWave));
            const step = length / steps;
            for (let s = 0; s < steps; s++) {
                const side = s % 2 === 0 ? amplitude * 2 : -amplitude * 2;
                const middle = (s + 0.5) * step;
                const end = (s + 1) * step;
                path += ` Q ${from.x + unitX * middle - unitY * side} ${from.y + unitY * middle + unitX * side} ${from.x + unitX * end} ${from.y + unitY * end}`;
            }
        }
        return path;
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
        const group = create('g', { class: 'poe-splitter', transform: `translate(${at.x}, ${at.y})` }, parent);
        create('rect', { x: vertical ? -7 : -9, y: vertical ? -9 : -7, width: vertical ? 14 : 18, height: vertical ? 18 : 14, rx: 4, fill: colour }, group);
        create('polygon', { points: '1.5,-6 -3.5,1 -0.5,1 -1.5,6 3.5,-1 0.5,-1' }, group);
        create('title', {}, group).textContent = 'PoE Splitter';
    };
    // Draws a cable's line, split at its PoE splitter when it has one: full colour with the zigzag up to the splitter, the plain darker shade after it.
    const drawLine = (edge, clean, lineClass, parent, splitterAtEnd) => {
        const split = edge.splitter && splitterAtEnd !== null ? splitAt(splitterAtEnd ? clean : [...clean].reverse(), Math.min(40, pathLength(clean) * 0.3)) : null;
        if (edge.wireless) {
            create('path', { d: wavePath(clean), class: `${lineClass} wireless`, stroke: colours.wifi }, parent);
            return;
        }
        if (edge.zigbee) {
            create('path', { d: roundedPath(clean), class: lineClass, stroke: colours.zigbee }, parent);
            return;
        }
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
        const group = create('g', { class: `map-node ${node.kind}${zigbeeById.has(node.id) && !rawById.has(node.id) ? ' zigbee' : ''}${node.hue ? ' hue' : ''}${node.online ? '' : ' offline'}`, transform: `translate(${x}, ${y}) scale(${scale ?? 1})`, 'data-node': node.id }, parent);
        create('rect', { width: width, height: height, rx: compact ? 4 : 8, class: 'map-node-box' }, group);
        const label = create('text', { x: width / 2, y: !compact && node.ports.length ? 18 : height / 2 + 4, class: 'map-node-label' }, group);
        label.textContent = node.label;
        group.addEventListener('click', event => {
            event.stopPropagation();
            showDevice(node.id);
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
                showPort(node.id, port.id);
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

        // On the Floor Plan, devices with no cables in the room are stacked in one column, the same distance apart as a stack under a switch.
        const connectedIds = new Set(group.edges.flatMap(edge => [edge.source, edge.target]));
        // A rack goes at the top of the column, above the other devices.
        const looseNodes = compact ? group.nodes.filter(node => !connectedIds.has(node.id)).sort((a, b) => (b.kind === 'rack') - (a.kind === 'rack')) : [];
        let looseY = 0;
        const loose = looseNodes.length > 1 ? {
            id: 'loose',
            items: looseNodes.map(node => {
                const size = sizeFor(node);
                const item = { node: node, x: 0, y: looseY, width: size.width, height: size.height };
                looseY += size.height + stackGap;
                return item;
            }),
            width: Math.max(...looseNodes.map(node => sizeFor(node).width))
        } : null;

        const run = async leftSide => {
            const stacks = makeStacks(leftSide);
            const stackItemsByEdge = new Map(stacks.flatMap(stack => stack.items.map(item => [item.edge.id, { stack: stack, item: item }])));
            const stackedNodeIds = new Set([...stacks.flatMap(stack => stack.items.map(item => item.node.id)), ...(loose ? looseNodes.map(node => node.id) : [])]);
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
                    ...(loose ? [{ id: loose.id, width: loose.width, height: looseY - stackGap }] : []),
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
            const stack = stacks.find(candidate => candidate.id === child.id) ?? (loose && child.id === loose.id ? loose : null);
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
                drawEdge(edge, points, layers.edges, layers.halos);
            });
        });
        result.placements.forEach(placement => {
            if (layers.halos) {
                create('rect', { x: placement.x, y: placement.y, width: placement.width, height: placement.height, rx: 4, class: 'map-halo-box' }, layers.halos);
            }
            drawNode(placement.node, placement.x, placement.y, placement.width, placement.height, layers.nodes, compact);
        });
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

    // All view: each floor is laid out by ELK on its own with its rooms and devices, and the floors sit side by side like the Floor Plan. Not to scale.
    // A line between floors leaves through the top of its floor, at the spot ELK picks to cross as little as possible, and runs across above the floors.
    // The Zigbee view uses the same layout with just the floors, so the mesh stays one tree.
    const layoutNested = async withRooms => {
        const groupOptions = { 'elk.algorithm': 'layered', 'elk.direction': 'DOWN', 'elk.edgeRouting': 'ORTHOGONAL', 'elk.hierarchyHandling': 'INCLUDE_CHILDREN', 'elk.layered.mergeHierarchyEdges': 'false', 'elk.layered.spacing.nodeNodeBetweenLayers': '50', 'elk.layered.spacing.edgeNodeBetweenLayers': '20', 'elk.layered.spacing.edgeEdgeBetweenLayers': '10', 'elk.spacing.nodeNode': '30', 'elk.spacing.edgeEdge': '10', 'elk.layered.nodePlacement.strategy': 'BRANDES_KOEPF' };
        const groups = [
            ...graph.floors.map(floor => ({ id: `floor-${floor.id}`, rooms: floor.rooms, nodes: graph.nodes.filter(node => floor.rooms.some(item => item.id === node.room)) })),
            { id: 'floor-unplaced', rooms: [], nodes: graph.nodes.filter(node => !roomsById.has(node.room)) }
        ].filter(group => group.nodes.length);
        const groupOf = new Map(groups.flatMap(group => group.nodes.map(node => [node.id, group.id])));
        const crossing = new Set(graph.edges.filter(edge => groupOf.get(edge.source) !== groupOf.get(edge.target)).map(edge => edge.id));
        // Devices without ports, like a rack, get a connection point per cable so ELK can spread the cables out instead of bunching them into one spot.
        // A line to another floor always connects to the top, since that's the way out.
        const device = (node, edges) => {
            const size = sizeOf(node);
            if (!node.ports.length) {
                const into = edges.filter(edge => edge.target === node.id && !edge.targetPort);
                const outOf = edges.filter(edge => edge.source === node.id && !edge.sourcePort);
                return {
                    id: node.id,
                    width: size.width,
                    height: size.height,
                    layoutOptions: { 'elk.portConstraints': 'FIXED_SIDE', 'elk.nodeSize.constraints': 'PORTS MINIMUM_SIZE', 'elk.nodeSize.minimum': `(${size.width}, ${size.height})`, 'elk.spacing.portPort': '10' },
                    ports: [...into.map(edge => ({ id: `${edge.id}-to`, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } })), ...outOf.map(edge => ({ id: `${edge.id}-from`, width: 0, height: 0, layoutOptions: { 'elk.port.side': crossing.has(edge.id) ? 'NORTH' : 'SOUTH' } }))]
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
        const end = (edge, nodeId) => {
            const port = nodeId === edge.source ? edge.sourcePort : edge.targetPort;
            if (port) {
                return port;
            }
            if (nodeId === edge.target && stackOfEdge.has(edge.id)) {
                const stacked = stackOfEdge.get(edge.id);
                return `${stacked.stack.id}-lane-${stacked.item.lane}`;
            }
            if (nodesById.get(nodeId).ports.length) {
                return `${nodeId}-${crossing.has(edge.id) || nodeId === edge.target ? 'in' : 'out'}`;
            }
            return `${edge.id}-${nodeId === edge.target ? 'to' : 'from'}`;
        };
        // Zigbee end devices hanging off the same parent are stacked in a column under it, like clients under a switch on the Network view.
        // The lanes run down the right of the column and each line turns into its device from that side, so the devices sit on the left.
        const stacks = new Map();
        const stackOfEdge = new Map();
        const makeStack = (parentId, edges) => {
            edges.sort((a, b) => a.source.localeCompare(b.source) || nodesById.get(a.target).label.localeCompare(nodesById.get(b.target).label));
            const leafWidth = Math.max(...edges.map(edge => sizeOf(nodesById.get(edge.target)).width));
            let y = 0;
            const items = edges.map((edge, lane) => {
                const size = sizeOf(nodesById.get(edge.target));
                const item = { edge: edge, node: nodesById.get(edge.target), lane: lane, x: leafWidth - size.width, y: y, width: size.width, height: size.height };
                y += size.height + stackGap;
                return item;
            });
            // The top device takes the lane nearest the devices so no two lines cross.
            const stack = { id: `stack-${parentId}`, items: items, laneX: lane => leafWidth + 16 + lane * laneGap, entryX: leafWidth, width: leafWidth + edges.length * laneGap + 16, height: y - stackGap };
            stacks.set(stack.id, stack);
            items.forEach(item => stackOfEdge.set(item.edge.id, { stack: stack, item: item }));
            return stack;
        };
        // Hue devices have no lines, so they're kept out of the layout and stacked in a column down the right of their room or floor, in space kept for them.
        const hueColumns = new Map();
        const needsSpace = new Set();
        const hueColumn = (id, nodes) => {
            const hue = nodes.filter(node => node.hue).sort((a, b) => a.label.localeCompare(b.label));
            if (!hue.length) {
                return null;
            }
            const column = { nodes: hue, width: Math.max(...hue.map(node => sizeOf(node).width)), height: hue.reduce((total, node) => total + sizeOf(node).height + stackGap, -stackGap) };
            hueColumns.set(id, column);
            return column;
        };
        const roomSpace = (column, top, bottom) => column ? { 'elk.padding': `[top=${top},left=16,bottom=${bottom},right=${column.width + 40}]`, 'elk.nodeSize.constraints': 'MINIMUM_SIZE', 'elk.nodeSize.minimum': `(${column.width + 56}, ${top + column.height + bottom})` } : { 'elk.padding': `[top=${top},left=16,bottom=${bottom},right=16]` };
        // Other Zigbee devices with no lines here are stacked in a column of their own at the bottom of their room or floor instead of spreading out in one long row.
        const loose = (node, edges) => zigbeeById.has(node.id) && !rawById.has(node.id) && !edges.some(edge => edge.source === node.id || edge.target === node.id);
        const contents = (id, nodes, edges) => {
            const rest = nodes.filter(node => !node.hue);
            const here = new Set(rest.map(node => node.id));
            const byParent = new Map();
            // Every end device is stacked, even an only child. Those whose parents are on other floors share one stack, grouped by parent, which their lines reach through the top of the floor.
            const away = `away-${id}`;
            edges.filter(edge => edge.zigbee && (here.has(edge.source) || crossing.has(edge.id)) && here.has(edge.target) && nodesById.get(edge.target).kind === 'client' && edges.filter(other => other.source === edge.target || other.target === edge.target).length === 1).forEach(edge => {
                const key = here.has(edge.source) ? edge.source : away;
                byParent.set(key, [...(byParent.get(key) ?? []), edge]);
            });
            const made = [...byParent].map(([parentId, list]) => makeStack(parentId, list));
            const stacked = new Set(made.flatMap(stack => stack.items.map(item => item.node.id)));
            const packed = rest.filter(node => !stacked.has(node.id) && loose(node, edges));
            const pack = { id: `loose-${id}`, layoutOptions: { 'elk.algorithm': 'box', 'elk.hierarchyHandling': 'SEPARATE_CHILDREN', 'elk.layered.layering.layerConstraint': 'LAST', 'elk.aspectRatio': '0.01', 'elk.spacing.nodeNode': '8', 'elk.padding': '[top=0,left=0,bottom=0,right=0]' }, children: packed.map(node => device(node, edges)) };
            const column = hueColumns.get(id);
            return [
                ...(column && needsSpace.has(id) ? [{ id: `space-${id}`, width: 1, height: column.height, layoutOptions: { 'elk.layered.layering.layerConstraint': 'FIRST' } }] : []),
                ...rest.filter(node => !stacked.has(node.id) && !loose(node, edges)).map(node => device(node, edges)),
                ...made.map(stack => ({ id: stack.id, width: stack.width, height: stack.height, layoutOptions: { 'elk.portConstraints': 'FIXED_POS' }, ports: stack.items.map(item => ({ id: `${stack.id}-lane-${item.lane}`, x: stack.laneX(item.lane), y: 0, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } })) })),
                ...(packed.length ? [pack] : [])
            ];
        };
        const room = (id, nodes, edges) => {
            const column = hueColumn(id, nodes);
            const children = contents(id, nodes, edges);
            const top = roomHeader + 20;
            const bottom = portSize + 16;
            // A room with only Hue devices has nothing for ELK to size it by, so it takes the column's size.
            const size = children.length ? {} : { width: column.width + 56, height: top + column.height + bottom };
            return { id: id, layoutOptions: { ...groupOptions, ...roomSpace(column, top, bottom) }, children: children, ...size };
        };
        // Each floor is the one child of its own root, with a port on its top edge for every line leaving it. The lines inside run from that port to the device.
        const results = await Promise.all(groups.map(async group => {
            const outside = graph.edges.filter(edge => crossing.has(edge.id) && (groupOf.get(edge.source) === group.id || groupOf.get(edge.target) === group.id));
            const run = inside => {
                const touching = [...inside, ...outside];
                const children = withRooms && group.rooms.length ? group.rooms.map(item => ({ id: `room-${item.id}`, nodes: group.nodes.filter(node => node.room === item.id) })).filter(item => item.nodes.length).map(item => room(item.id, item.nodes, touching)) : contents(group.id, group.nodes, touching);
                const floorColumn = withRooms && group.rooms.length ? null : hueColumn(group.id, group.nodes);
                return new ELK().layout({
                    id: 'root',
                    layoutOptions: { ...groupOptions, 'elk.padding': '[top=0,left=0,bottom=0,right=0]' },
                    children: [{
                        id: group.id,
                        layoutOptions: { ...groupOptions, 'elk.portConstraints': 'FIXED_SIDE', ...roomSpace(floorColumn, titleHeight + 12, withRooms && group.rooms.length ? 16 : portSize + 16) },
                        ports: outside.map(edge => ({ id: `${edge.id}@${group.id}`, width: 0, height: 0, layoutOptions: { 'elk.port.side': 'NORTH' } })),
                        children: children
                    }],
                    edges: [
                        ...inside.map(edge => ({ id: edge.id, sources: [end(edge, edge.source)], targets: [end(edge, edge.target)] })),
                        ...outside.map(edge => ({ id: `${edge.id}@${group.id}`, sources: [`${edge.id}@${group.id}`], targets: [end(edge, groupOf.get(edge.source) === group.id ? edge.source : edge.target)] }))
                    ]
                });
            };
            const inside = graph.edges.filter(edge => groupOf.get(edge.source) === group.id && groupOf.get(edge.target) === group.id);
            // The Zigbee view is one tree already running downwards from the coordinator, so it needs no turning.
            const loose = edge => withRooms && (edge.wireless || edge.zigbee) && roomsById.get(nodesById.get(edge.source).room) !== roomsById.get(nodesById.get(edge.target).room);
            // ELK doesn't hold a room to a minimum size, so a room too short for its Hue column is laid out again with a spacer as tall as the column.
            // A floor is simply drawn taller.
            const fit = async edges => {
                const layout = await run(edges);
                const short = [];
                const check = box => (box.children ?? []).forEach(child => {
                    const column = hueColumns.get(child.id);
                    if (column && child.id !== group.id && child.height < roomHeader + 20 + column.height + 16) {
                        short.push(child.id);
                    }
                    check(child);
                });
                check(layout);
                if (!short.length) {
                    return layout;
                }
                short.forEach(id => needsSpace.add(id));
                return run(edges);
            };
            if (!inside.some(loose)) {
                return fit(inside);
            }
            // Wi-Fi and Zigbee links between rooms have no real direction, but ELK runs every line downwards and loops any line pointing back up the long way round.
            // So the floor is laid out first without them, then each one is turned to run from the higher device to the lower before the real layout.
            const first = await run(inside.filter(edge => !loose(edge)));
            const heights = new Map();
            const walk = (box, y) => (box.children ?? []).forEach(child => {
                heights.set(child.id, y + child.y + child.height / 2);
                walk(child, y + child.y);
            });
            walk(first, 0);
            return fit(inside.map(edge => loose(edge) && heights.get(edge.source) > heights.get(edge.target) ? { ...edge, source: edge.target, target: edge.source, sourcePort: edge.targetPort, targetPort: edge.sourcePort } : edge));
        }));
        return { groups: groups.map((group, index) => ({ ...group, layout: results[index] })), groupOf: groupOf, crossing: crossing, stacks: stacks, stackOfEdge: stackOfEdge, hueColumns: hueColumns };
    };

    const drawNested = (nested, layers) => {
        const floorNames = new Map(graph.floors.map(floor => [`floor-${floor.id}`, floor.name]));
        const roomNames = new Map(graph.floors.flatMap(floor => floor.rooms.map(item => [`room-${item.id}`, item.name])));
        // The floors in a row, in their saved order, then the unplaced devices.
        let ox = 0;
        const placed = nested.groups.map(group => {
            const box = group.layout.children[0];
            const column = nested.hueColumns.get(group.id);
            const place = { ...group, box: box, x: ox, height: Math.max(box.height, column ? titleHeight + 12 + column.height + 16 : 0) };
            ox += box.width + groupGap;
            return place;
        });
        const placeOf = new Map(placed.map(place => [place.id, place]));
        // Each line between floors gets its own lane above them. Shorter lines get the lower lanes so the longer ones pass over them instead of crossing.
        const exitX = (edgeId, groupId) => {
            const place = placeOf.get(groupId);
            return place.x + place.box.ports.find(port => port.id === `${edgeId}@${groupId}`).x;
        };
        const lanes = [];
        const laneOf = new Map();
        graph.edges.filter(edge => nested.crossing.has(edge.id)).map(edge => {
            const a = exitX(edge.id, nested.groupOf.get(edge.source));
            const b = exitX(edge.id, nested.groupOf.get(edge.target));
            return { id: edge.id, from: Math.min(a, b), to: Math.max(a, b) };
        }).sort((a, b) => (a.to - a.from) - (b.to - b.from)).forEach(span => {
            let lane = lanes.findIndex(taken => taken.every(other => span.to < other.from - 6 || span.from > other.to + 6));
            if (lane < 0) {
                lane = lanes.length;
                lanes.push([]);
            }
            lanes[lane].push(span);
            laneOf.set(span.id, lane);
        });
        const top = 24 + lanes.length * laneGap;
        const laneY = edgeId => top - 14 - laneOf.get(edgeId) * laneGap;

        const boxes = new Map();
        const routes = [];
        // ELK gives each line relative to the innermost box holding both its ends: the floor, or a room when both ends are in it.
        const roomOfNode = new Map();
        placed.forEach(place => (place.box.children ?? []).filter(child => roomNames.has(child.id)).forEach(child => (child.children ?? []).forEach(item => [item.id, ...(nested.stacks.get(item.id)?.items.map(stacked => stacked.node.id) ?? [])].forEach(nodeId => roomOfNode.set(nodeId, child)))));
        const stackBoxes = new Map();
        // Draws a column of Hue devices down the right of a room or floor, in the space kept for it.
        const drawHue = (containerId, right, y) => {
            const column = nested.hueColumns.get(containerId);
            let at = y;
            column?.nodes.forEach(node => {
                const size = sizeOf(node);
                drawNode(node, right - 16 - column.width, at, size.width, size.height, layers.nodes, false);
                boxes.set(node.id, { x: right - 16 - column.width, y: at, width: size.width, height: size.height });
                at += size.height + stackGap;
            });
        };
        const pointsOf = (layoutEdge, place, edge) => {
            const inRoom = edge && roomOfNode.get(edge.source) && roomOfNode.get(edge.source) === roomOfNode.get(edge.target) ? roomOfNode.get(edge.source) : null;
            const origin = { x: place.x + (inRoom?.x ?? 0), y: top + (inRoom?.y ?? 0) };
            return (layoutEdge?.sections ?? []).flatMap(section => [section.startPoint, ...(section.bendPoints ?? []), section.endPoint]).map(point => ({ x: point.x + origin.x, y: point.y + origin.y }));
        };
        placed.forEach(place => {
            const floor = place.box;
            drawFrame(floorNames.get(place.id) ?? 'Unplaced', place.x, top, floor.width, place.height, layers.boxes);
            (floor.children ?? []).forEach(child => {
                const x = place.x + child.x;
                const y = top + child.y;
                // Draws a device, each device in a packed box of loose ones, or each device in a stack.
                const draw = (item, left, top) => {
                    if (item.id.startsWith('space-')) {
                        return;
                    }
                    const stack = nested.stacks.get(item.id);
                    if (stack) {
                        stackBoxes.set(item.id, { x: left + item.x, y: top + item.y });
                        stack.items.forEach(stacked => {
                            drawNode(stacked.node, left + item.x + stacked.x, top + item.y + stacked.y, stacked.width, stacked.height, layers.nodes, false);
                            boxes.set(stacked.node.id, { x: left + item.x + stacked.x, y: top + item.y + stacked.y, width: stacked.width, height: stacked.height });
                        });
                        return;
                    }
                    if (item.id.startsWith('loose-')) {
                        (item.children ?? []).forEach(inner => draw(inner, left + item.x, top + item.y));
                        return;
                    }
                    drawNode(nodesById.get(item.id), left + item.x, top + item.y, item.width, item.height, layers.nodes, false);
                    boxes.set(item.id, { x: left + item.x, y: top + item.y, width: item.width, height: item.height });
                };
                if (roomNames.has(child.id)) {
                    drawRoom(roomNames.get(child.id), x, y, child.width, child.height, layers.boxes);
                    (child.children ?? []).forEach(item => draw(item, x, y));
                    drawHue(child.id, x + child.width, y + roomHeader + 20);
                } else {
                    draw(child, place.x, top);
                }
            });
            drawHue(place.id, place.x + floor.width, top + titleHeight + 12);
            (place.layout.edges ?? []).filter(layoutEdge => !layoutEdge.id.includes('@')).forEach(layoutEdge => {
                const edge = graph.edges.find(item => item.id === layoutEdge.id);
                const points = pointsOf(layoutEdge, place, edge);
                // A line into a stack carries on down its lane, then turns into the side of its device.
                const stacked = nested.stackOfEdge.get(edge.id);
                if (stacked && points.length && stackBoxes.has(stacked.stack.id)) {
                    const at = stackBoxes.get(stacked.stack.id);
                    const centreY = at.y + stacked.item.y + stacked.item.height / 2;
                    points.push({ x: points[points.length - 1].x, y: centreY }, { x: at.x + stacked.stack.entryX, y: centreY });
                }
                routes.push({ edge: edge, points: points });
            });
        });
        // A line between floors: from its device out of the top of the floor, across its lane, and down into the other floor to its other device.
        graph.edges.filter(edge => nested.crossing.has(edge.id)).forEach(edge => {
            const half = groupId => {
                const place = placeOf.get(groupId);
                return pointsOf(place.layout.edges.find(layoutEdge => layoutEdge.id === `${edge.id}@${groupId}`), place);
            };
            const out = [...half(nested.groupOf.get(edge.source))].reverse();
            const back = half(nested.groupOf.get(edge.target));
            if (!out.length || !back.length) {
                return;
            }
            const stacked = nested.stackOfEdge.get(edge.id);
            if (stacked && stackBoxes.has(stacked.stack.id)) {
                const at = stackBoxes.get(stacked.stack.id);
                const centreY = at.y + stacked.item.y + stacked.item.height / 2;
                back.push({ x: back[back.length - 1].x, y: centreY }, { x: at.x + stacked.stack.entryX, y: centreY });
            }
            const y = laneY(edge.id);
            routes.push({ edge: edge, points: [...out, { x: out[out.length - 1].x, y: y }, { x: back[0].x, y: y }, ...back] });
        });

        // A cable leaving the bottom of a rack only to loop round and climb past it is moved to leave from the top instead,
        // running across just above the rack to join its climb.
        const racks = new Map([...boxes].filter(([nodeId]) => nodesById.get(nodeId).kind === 'rack'));
        racks.forEach((box, rackId) => {
            const lifts = routes.map(route => {
                const atStart = route.edge.source === rackId;
                if (!atStart && route.edge.target !== rackId) {
                    return null;
                }
                const points = atStart ? route.points : [...route.points].reverse();
                const climb = points.findIndex((point, index) => index > 0 && points[index - 1].x === point.x && point.y < box.y - 12 && points[index - 1].y > box.y && (point.x < box.x || point.x > box.x + box.width));
                return climb > 0 && points[0].y >= box.y + box.height - 1 ? { route: route, atStart: atStart, points: points, climb: climb } : null;
            }).filter(lift => lift);
            // A rack has no real ports, so the cables leaving its top are spaced out in the same order as the climbs they join, which keeps them from crossing.
            [...lifts].sort((a, b) => a.points[a.climb].x - b.points[b.climb].x).forEach((lift, index) => {
                lift.points = [{ x: box.x + (index + 1) * box.width / (lifts.length + 1), y: lift.points[0].y }, ...lift.points.slice(1)];
            });
            // Each cable rises from its port, runs across at its own height and joins its climb. The heights are ordered so as few of these cross as possible.
            const between = (value, a, b) => value > Math.min(a, b) && value < Math.max(a, b);
            const cost = (upper, lower) => (between(upper.points[0].x, lower.points[0].x, lower.points[lower.climb].x) ? 1 : 0) + (between(lower.points[lower.climb].x, upper.points[0].x, upper.points[upper.climb].x) ? 1 : 0);
            const ordered = [];
            lifts.forEach(lift => {
                let best = 0;
                let bestCost = Infinity;
                for (let at = 0; at <= ordered.length; at++) {
                    const total = ordered.slice(0, at).reduce((sum, other) => sum + cost(other, lift), 0) + ordered.slice(at).reduce((sum, other) => sum + cost(lift, other), 0);
                    if (total < bestCost) {
                        best = at;
                        bestCost = total;
                    }
                }
                ordered.splice(best, 0, lift);
            });
            ordered.forEach((lift, order) => {
                const across = box.y - 12 - (ordered.length - 1 - order) * laneGap;
                const start = lift.points[0];
                const join = { x: lift.points[lift.climb].x, y: across };
                const points = [{ x: start.x, y: box.y }, { x: start.x, y: across }, join, ...lift.points.slice(lift.climb)];
                lift.route.points = lift.atStart ? points : points.reverse();
            });
        });
        routes.forEach(route => drawEdge(route.edge, route.points, layers.edges));

        // An end device's Zigbee link to another room is only shown, as an arc, while the pointer is over a device at either end.
        // The arc runs from the top of one device to the top of the other, rising only as high as the distance needs. When one device is mostly above the other,
        // it leaves the upper one's bottom and bows out to the side instead.
        const arcsByNode = new Map();
        (graph.zigbeeArcs ?? []).filter(edge => boxes.has(edge.source) && boxes.has(edge.target)).forEach(edge => {
            const [a, b] = [boxes.get(edge.source), boxes.get(edge.target)];
            const centre = box => box.x + box.width / 2;
            const across = Math.abs(centre(b) - centre(a));
            let d;
            if (across >= Math.abs(b.y - a.y)) {
                // The curve's highest point is a quarter of the way from its ends to the apex, and it shouldn't leave the top of the map.
                const apex = Math.max(Math.min(a.y, b.y) - Math.max(30, across * 0.25), (8 - 0.25 * Math.min(a.y, b.y)) / 0.75);
                d = `M ${centre(a)} ${a.y} C ${centre(a)} ${apex}, ${centre(b)} ${apex}, ${centre(b)} ${b.y}`;
            } else {
                const [upper, lower] = a.y < b.y ? [a, b] : [b, a];
                const from = { x: centre(upper), y: upper.y + upper.height };
                const to = { x: centre(lower), y: lower.y };
                const down = to.y - from.y;
                const bow = (to.x >= from.x ? 1 : -1) * Math.max(40, down * 0.2);
                d = `M ${from.x} ${from.y} C ${from.x + bow} ${from.y + down / 3}, ${to.x + bow} ${to.y - down / 3}, ${to.x} ${to.y}`;
            }
            const group = create('g', { class: 'map-edge zigbee map-hover-arc' + (edge.online ? '' : ' offline'), 'data-edge': edge.id }, layers.trunks);
            create('path', { d: d, class: 'map-edge-line', stroke: colours.zigbee }, group);
            [edge.source, edge.target].forEach(nodeId => arcsByNode.set(nodeId, [...(arcsByNode.get(nodeId) ?? []), group]));
        });
        arcsByNode.forEach((arcs, nodeId) => {
            const node = layers.nodes.querySelector(`[data-node="${CSS.escape(nodeId)}"]`);
            node?.addEventListener('mouseenter', () => arcs.forEach(arc => arc.classList.add('shown')));
            node?.addEventListener('mouseleave', () => arcs.forEach(arc => arc.classList.remove('shown')));
        });
        return { width: Math.max(0, ox - groupGap), height: top + Math.max(0, ...placed.map(place => place.height)) };
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

        // Cables between rooms, grouped by the pair of devices they join. A device in a rack is the rack here.
        const roomKey = node => roomsById.has(node.room) ? node.room : '';
        const byPair = new Map();
        // Only cables run between rooms. Wi-Fi to devices in other rooms gets one purple arc per AP and room instead.
        graph.edges.filter(edge => !edge.wireless && roomKey(nodesById.get(edge.source)) !== roomKey(nodesById.get(edge.target))).forEach(edge => {
            const keys = [edge.source, edge.target].sort();
            const key = keys.join('|');
            byPair.set(key, { keys: keys, edges: [...(byPair.get(key)?.edges ?? []), edge] });
        });
        const trunks = [...byPair.values()];
        const wifiByRoom = new Map();
        graph.edges.filter(edge => edge.wireless && roomKey(nodesById.get(edge.source)) !== roomKey(nodesById.get(edge.target))).forEach(edge => {
            const key = `${edge.source}|${roomKey(nodesById.get(edge.target))}`;
            wifiByRoom.set(key, { ap: edge.source, room: roomKey(nodesById.get(edge.target)), edges: [...(wifiByRoom.get(key)?.edges ?? []), edge] });
        });
        const wifiArcs = [...wifiByRoom.values()];
        const top = 40 + trunks.length * 14;

        // Floors side by side, then the unplaced devices, below the space the arcs need.
        const rects = new Map();
        const centres = new Map();
        let ox = groupPadding;
        const drawContent = (result, left, top) => {
            const transform = `translate(${left}, ${top}) scale(${scale})`;
            drawGroup(result, { halos: create('g', { transform: transform }, layers.halos), edges: create('g', { transform: transform }, layers.edges), nodes: create('g', { transform: transform }, layers.nodes) }, true);
            result.placements.forEach(placement => centres.set(placement.node.id, { x: left + (placement.x + placement.width / 2) * scale, y: top + (placement.y + placement.height / 2) * scale, top: top + placement.y * scale, bottom: top + (placement.y + placement.height) * scale, left: left + placement.x * scale, right: left + (placement.x + placement.width) * scale }));
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
        const drawTrunk = (trunk, from, controlA, controlB, to) => {
            const d = `M ${from.x} ${from.y} C ${controlA.x} ${controlA.y}, ${controlB.x} ${controlB.y}, ${to.x} ${to.y}`;
            const group = create('g', { class: 'map-trunk' + (trunk.wifi ? ' wifi' : ''), 'data-edges': trunk.edges.map(edge => edge.id).join(' ') }, layers.trunks);
            create('path', { d: d, class: 'map-trunk-hit' }, group);
            create('path', { d: d, class: 'map-trunk-line', style: `stroke-width: ${Math.min(8, 1.5 + trunk.edges.length)}px` }, group);
            create('circle', { cx: from.x, cy: from.y, r: 3, class: 'map-trunk-end' }, group);
            create('circle', { cx: to.x, cy: to.y, r: 3, class: 'map-trunk-end' }, group);
            const middle = { x: 0.125 * (from.x + to.x) + 0.375 * (controlA.x + controlB.x), y: 0.125 * (from.y + to.y) + 0.375 * (controlA.y + controlB.y) };
            create('circle', { cx: middle.x, cy: middle.y, r: 9, class: 'map-trunk-badge' }, group);
            create('text', { x: middle.x, y: middle.y + 4, class: 'map-trunk-count' }, group).textContent = trunk.edges.length;
            group.addEventListener('click', event => {
                event.stopPropagation();
                if (trunk.wifi) {
                    showEdges(`${nodesById.get(trunk.wifi.ap).label} → ${roomName(trunk.wifi.room)}`, trunk.edges.map(edge => rawEdgesById.get(edge.id) ?? edge));
                } else {
                    showEdges(`${nodesById.get(trunk.keys[0]).label} ↔ ${nodesById.get(trunk.keys[1]).label}`, trunk.edges.map(edge => rawEdgesById.get(edge.id) ?? edge));
                }
                focusEdges(trunk.edges);
            });
        };

        // Cable arcs run from the top of one device up over the floors and down to the top of the other. Wider arcs fly higher so they nest instead of crossing.
        const deviceTop = id => centres.has(id) ? { x: centres.get(id).x, y: centres.get(id).top } : anchor(roomKey(nodesById.get(id)));
        const ordered = trunks.map(trunk => ({ ...trunk, from: deviceTop(trunk.keys[0]), to: deviceTop(trunk.keys[1]) })).map(trunk => trunk.from.x <= trunk.to.x ? trunk : { ...trunk, from: trunk.to, to: trunk.from }).sort((a, b) => (a.to.x - a.from.x) - (b.to.x - b.from.x));
        // Two devices mostly above one another, like a rack over the room below, get a curve bowing out to the side from the bottom of the upper one to the top of the lower one instead.
        const upright = trunk => Math.abs(trunk.to.x - trunk.from.x) < Math.abs(trunk.to.y - trunk.from.y);
        ordered.filter(trunk => !upright(trunk)).forEach((trunk, index) => {
            const apex = top - 24 - index * 14;
            drawTrunk(trunk, trunk.from, { x: trunk.from.x, y: apex }, { x: trunk.to.x, y: apex }, trunk.to);
        });
        ordered.filter(upright).forEach(trunk => {
            const [upper, lower] = [...trunk.keys].sort((a, b) => deviceTop(a).y - deviceTop(b).y);
            const from = centres.has(upper) ? { x: centres.get(upper).x, y: centres.get(upper).bottom } : deviceTop(upper);
            const to = deviceTop(lower);
            const down = to.y - from.y;
            const bow = (to.x >= from.x ? 1 : -1) * Math.max(40, Math.abs(down) * 0.2);
            drawTrunk(trunk, from, { x: from.x + bow, y: from.y + down / 3 }, { x: to.x + bow, y: to.y - down / 3 }, to);
        });

        // A Wi-Fi arc starts from the AP itself and only rises as high as the distance it covers needs. When the room is mostly below or above the AP,
        // it leaves the AP's bottom or top and bows out to the side instead.
        wifiArcs.filter(arc => centres.has(arc.ap) && rects.has(arc.room)).forEach(arc => {
            const trunk = { wifi: arc, edges: arc.edges };
            const ap = centres.get(arc.ap);
            const to = anchor(arc.room);
            const across = Math.abs(to.x - ap.x);
            if (across >= Math.abs(to.y - ap.y)) {
                const from = { x: ap.x, y: ap.top };
                const apex = Math.min(from.y, to.y) - Math.max(30, across * 0.25);
                drawTrunk(trunk, from, { x: from.x, y: apex }, { x: to.x, y: apex }, to);
                return;
            }
            const from = { x: ap.x, y: to.y > ap.y ? ap.bottom : ap.top };
            const down = to.y - from.y;
            const bow = (to.x >= ap.x ? 1 : -1) * Math.max(40, Math.abs(down) * 0.2);
            drawTrunk(trunk, from, { x: from.x + bow, y: from.y + down / 3 }, { x: to.x + bow, y: to.y - down / 3 }, to);
        });

        return true;
    };

    // The Network view can show one network at a time, picked from the box at its top left. A device is on the network when its IP is in the network's subnet.
    // A cable carries the network untagged when it's its port's native network, or tagged when the port allows it tagged and the far end is a switch, AP or gateway, or is itself on the network.
    // Switches and cables that lead to nothing on the network are left out.
    const vlanSelect = document.getElementById('vlan-select');
    const toNumber = ip => ip.split('.').reduce((total, part) => total * 256 + Number(part), 0);
    const inSubnet = (ip, subnet) => {
        const [base, bits] = subnet.split('/');
        if (!ip || !base || !/^\d+\.\d+\.\d+\.\d+$/.test(ip)) {
            return false;
        }
        const size = 2 ** (32 - Number(bits ?? 32));
        return Math.floor(toNumber(ip) / size) === Math.floor(toNumber(base) / size);
    };
    const sameName = (a, b) => (a ?? '').toLowerCase() === (b ?? '').toLowerCase();
    const vlanView = name => {
        const network = raw.networks.find(item => item.name === name);
        if (!network) {
            return { ...raw, nodes: [], edges: [] };
        }
        // The gateway routes every network, so it's always shown, whatever its own IP is.
        const members = new Set(raw.nodes.filter(node => node.kind === 'gateway' || inSubnet(node.ip, network.subnet)).map(node => node.id));
        // UniFi names the gateway's own ports' networks differently, like LAN for Default, so a native network that matches no known network counts as the default one.
        const defaultName = raw.networks.find(item => !item.vlanId)?.name;
        const nativeOf = port => raw.networks.some(item => sameName(item.name, port.network)) ? port.network : defaultName;
        let edges = raw.edges.map(edge => {
            const target = rawById.get(edge.target);
            if (edge.wireless) {
                return members.has(edge.target) ? edge : null;
            }
            const port = portsById.get(edge.sourcePort);
            if (!port) {
                return null;
            }
            if (sameName(nativeOf(port), name)) {
                return edge;
            }
            return port.tagged.some(tagged => sameName(tagged, name)) && (isInfrastructure(target) || members.has(edge.target)) ? { ...edge, tagged: true } : null;
        }).filter(edge => edge);
        for (let pruned = true; pruned;) {
            const degree = new Map();
            edges.forEach(edge => [edge.source, edge.target].forEach(nodeId => degree.set(nodeId, (degree.get(nodeId) ?? 0) + 1)));
            const before = edges.length;
            edges = edges.filter(edge => [edge.source, edge.target].every(nodeId => members.has(nodeId) || degree.get(nodeId) > 1));
            pruned = edges.length < before;
        }
        const shown = new Set([...members, ...edges.flatMap(edge => [edge.source, edge.target])]);
        return { ...raw, nodes: raw.nodes.filter(node => shown.has(node.id)), edges: edges };
    };
    const allOption = document.createElement('option');
    allOption.value = '';
    allOption.textContent = 'All';
    vlanSelect.append(allOption);
    raw.networks.forEach(network => {
        const option = document.createElement('option');
        option.value = network.name;
        option.textContent = network.vlanId ? `${network.name} (VLAN ${network.vlanId})` : network.name;
        vlanSelect.append(option);
    });
    try {
        const storedVlan = localStorage.getItem('holamap.vlan');
        if (raw.networks.some(network => network.name === storedVlan)) {
            vlanSelect.value = storedVlan;
        }
    } catch (error) {
        // Storage is only a convenience for remembering the network.
    }

    // The All and Floor Plan views with the Zigbee devices that have a room; the Floor Plan leaves out their links. The coordinator sits with its host, and a device whose parent is hidden hangs off the nearest shown device above it.
    const withZigbee = (links = true) => {
        if (!zigbee?.nodes.length) {
            return collapsed;
        }
        const usb = zigbee.edges.find(edge => !edge.zigbee);
        const host = usb ? rackOf.get(usb.source) ?? usb.source : '';
        const hostNode = collapsed.nodes.find(node => node.id === host);
        const nodes = zigbee.nodes.filter(node => node.id.startsWith('z')).map(node => node.kind === 'coordinator' && !roomsById.has(node.room) && hostNode ? { ...node, room: hostNode.room } : node).filter(node => roomsById.has(node.room));
        const cable = hostNode && nodes.some(node => node.id === usb.target) ? [{ ...usb, source: host }] : [];
        if (!links) {
            return { ...collapsed, nodes: [...collapsed.nodes, ...nodes], edges: [...collapsed.edges, ...cable] };
        }
        // Only the ZHA mesh is linked up here. Hue devices all hang straight off the bridge, which says nothing about the mesh, so they're left unlinked.
        const shown = new Set(nodes.map(node => node.id));
        const linkTo = new Map(zigbee.edges.filter(edge => edge.zigbee && edge.source.startsWith('z')).map(edge => [edge.target, edge]));
        const shownParent = id => {
            let current = linkTo.get(id)?.source;
            while (current && !shown.has(current)) {
                current = linkTo.get(current)?.source;
            }
            return current;
        };
        const edges = nodes.filter(node => shownParent(node.id)).map(node => ({ ...linkTo.get(node.id), source: shownParent(node.id) }));
        const roomOf = new Map(nodes.map(node => [node.id, node.room]));
        // Links inside a room, and the backbone between the coordinator and routers, are laid out with everything else like cables.
        // An end device's link to another room is left for an arc that only shows on hover.
        const kindOf = new Map(nodes.map(node => [node.id, node.kind]));
        const laidOut = edge => roomOf.get(edge.source) === roomOf.get(edge.target) || kindOf.get(edge.target) !== 'client';
        return { ...collapsed, nodes: [...collapsed.nodes, ...nodes], edges: [...collapsed.edges, ...edges.filter(laidOut), ...cable], zigbeeArcs: edges.filter(edge => !laidOut(edge)) };
    };

    // The Zigbee view on its own. A coordinator without a room is put in its host's room so it lands on the right floor.
    // Hue devices are shown on their floors without links, since the bridge says nothing about its mesh.
    // The picker at its top left shows both networks, just ZHA, or just the Hue Bridge and its devices.
    const zigbeeSelect = document.getElementById('zigbee-select');
    const zigbeeView = () => {
        const host = zigbee.nodes.find(node => node.id === zigbee.edges.find(edge => !edge.zigbee)?.source);
        // The Hue Bridge is the one coordinator that's a device on the map rather than a Zigbee device of its own.
        const isHue = node => node.hue || (node.kind === 'coordinator' && !node.id.startsWith('z'));
        const nodes = zigbee.nodes.filter(node => zigbeeSelect.value === 'hue' ? isHue(node) : zigbeeSelect.value === 'zha' ? !isHue(node) : true);
        const shown = new Set(nodes.map(node => node.id));
        return { ...raw, nodes: nodes.map(node => node.kind === 'coordinator' && !roomsById.has(node.room) && host ? { ...node, room: host.room } : node), edges: zigbee.edges.filter(edge => (!edge.zigbee || edge.source.startsWith('z')) && shown.has(edge.source) && shown.has(edge.target)) };
    };
    try {
        const storedZigbee = localStorage.getItem('holamap.zigbee');
        if ([...zigbeeSelect.options].some(option => option.value === storedZigbee)) {
            zigbeeSelect.value = storedZigbee;
        }
    } catch (error) {
        // Storage is only a convenience for remembering the choice.
    }

    // The Services view: each host as a box holding its services. Containers from the same Docker Compose project share a box inside the host, titled with the project.
    // Services and project boxes fill columns top to bottom, about ten chips tall, in name order.
    // A project with just one container shows as a chip named after the project, since its container can have a generic name like postgres.
    // A service named the same as a group on its host, like the Api app and its routes, is the group's box itself, so it isn't drawn again as a chip.
    const servicesView = () => {
        const hosts = services.filter(host => host.services.length).map(host => {
            const groups = new Set(host.services.map(service => service.group).filter(group => group));
            return { ...host, services: host.services.filter(service => service.group || !groups.has(service.label)) };
        });
        // A link to a service that's the group's box itself, like Api, ends at the box.
        const ends = new Map();
        services.forEach(host => host.services.forEach(service => ends.set(service.id, !service.group && host.services.some(other => other.group === service.label) ? serviceGroupId(host.id, service.label) : service.id)));
        const edges = serviceLinks.filter(link => ends.has(link.source) && ends.has(link.target)).map(link => ({ ...link, source: ends.get(link.source), target: ends.get(link.target), online: true }));
        const single = host => service => service.group && host.services.filter(other => other.group === service.group).length === 1;
        return { ...raw, nodes: hosts.flatMap(host => host.services.map(service => ({ id: service.id, label: single(host)(service) ? service.group : service.label, kind: service.kind === 'Docker' ? 'docker' : service.kind === 'PythonScript' ? 'python' : service.kind === 'Other' ? 'other' : service.kind === 'Cloud' ? 'cloud' : service.kind === 'Device' ? 'hardware' : service.mine ? 'app' : 'service', online: service.online, room: '', rack: '', ports: [] }))), edges: edges, hosts: hosts };
    };
    const drawServices = async (layers, bounds, id) => {
        const maxHeight = 10 * (chipHeight + stackGap) - stackGap;
        const hostGap = 32;
        const endpoints = new Set(graph.edges.flatMap(edge => [edge.source, edge.target]));

        // A host's services and project boxes, as items sized to fill columns.
        const itemsOf = host => {
            const counts = new Map();
            host.services.filter(service => service.group).forEach(service => counts.set(service.group, (counts.get(service.group) ?? 0) + 1));
            const items = [];
            host.services.forEach(service => {
                const node = nodesById.get(service.id);
                if (counts.get(service.group) > 1) {
                    let item = items.find(entry => entry.group === service.group);
                    if (!item) {
                        item = { group: service.group, label: service.group, nodes: [] };
                        items.push(item);
                    }
                    item.nodes.push(node);
                } else {
                    items.push({ label: node.label, nodes: [node] });
                }
            });
            items.sort((a, b) => a.label.localeCompare(b.label));
            items.forEach(item => {
                const chipWidth = Math.max(...item.nodes.map(node => chipSize(node).width));
                item.width = item.group ? Math.max(chipWidth, textWidth(item.label, true) + 16) + roomPadding * 2 : chipWidth;
                item.height = item.group ? roomHeader + item.nodes.length * (chipHeight + stackGap) - stackGap + roomPadding : chipHeight;
            });
            return items;
        };

        // Items fill columns top to bottom, about ten chips tall.
        const columnsOf = items => {
            const columns = [];
            items.forEach(item => {
                const column = columns[columns.length - 1];
                if (column && column.height + stackGap + item.height <= maxHeight) {
                    column.items.push(item);
                    column.height += stackGap + item.height;
                } else {
                    columns.push({ items: [item], height: item.height });
                }
            });
            columns.forEach(column => column.width = Math.max(...column.items.map(item => item.width)));
            return columns;
        };
        const sizeOfColumns = columns => ({ width: columns.reduce((sum, column) => sum + column.width, 0) + (columns.length - 1) * groupPadding, height: Math.max(...columns.map(column => column.height)) });
        const drawColumns = (columns, left, top) => {
            let x = left;
            columns.forEach(column => {
                let y = top;
                column.items.forEach(entry => {
                    if (entry.group) {
                        drawRoom(entry.label, x, y, column.width, entry.height, layers.boxes);
                        entry.nodes.forEach((node, row) => drawNode(node, x + roomPadding, y + roomHeader + row * (chipHeight + stackGap), column.width - roomPadding * 2, chipHeight, layers.nodes, true));
                    } else {
                        drawNode(entry.nodes[0], x, y, column.width, chipHeight, layers.nodes, true);
                    }
                    y += entry.height + stackGap;
                });
                x += column.width + groupPadding;
            });
        };

        // Hosts with a linked service are laid out by ELK, so linked services sit close together and the lines cross as little as possible.
        // Only the linked services, and the project boxes holding them, take part. The rest of each host is packed into one block beside them.
        const hosts = graph.hosts.map(host => {
            const items = itemsOf(host);
            const isLinked = item => item.nodes.some(node => endpoints.has(node.id)) || (item.group !== undefined && endpoints.has(serviceGroupId(host.id, item.group)));
            return { host: host, items: items, isLinked: isLinked, linked: items.some(isLinked) };
        });
        const linked = hosts.filter(entry => entry.linked);
        let top = 0;
        if (linked.length) {
            const blocks = new Map();
            const ids = new Set();
            // Lines into or out of the same service share one point on it, so they join on the way in and split on the way out.
            const direction = { 'elk.direction': 'RIGHT', 'elk.layered.mergeEdges': 'true' };
            const children = linked.map(entry => {
                const rest = columnsOf(entry.items.filter(item => !entry.isLinked(item)));
                const block = rest.length ? { id: `block:${entry.host.id}`, ...sizeOfColumns(rest) } : null;
                if (block) {
                    blocks.set(block.id, rest);
                }
                ids.add(entry.host.id);
                return {
                    id: entry.host.id,
                    layoutOptions: { ...direction, 'elk.padding': `[top=${titleHeight},left=${groupPadding},bottom=${groupPadding},right=${groupPadding}]`, 'elk.spacing.nodeNode': '12', 'elk.layered.spacing.nodeNodeBetweenLayers': '40' },
                    children: [
                        ...entry.items.filter(entry.isLinked).map(item => {
                            if (!item.group) {
                                ids.add(item.nodes[0].id);
                                return { id: item.nodes[0].id, ...chipSize(item.nodes[0]) };
                            }
                            const groupId = serviceGroupId(entry.host.id, item.group);
                            ids.add(groupId);
                            item.nodes.forEach(node => ids.add(node.id));
                            return { id: groupId, layoutOptions: { ...direction, 'elk.padding': `[top=${roomHeader},left=${roomPadding},bottom=${roomPadding},right=${roomPadding}]`, 'elk.spacing.nodeNode': String(stackGap) }, children: item.nodes.map(node => ({ id: node.id, ...chipSize(node) })) };
                        }),
                        ...(block ? [block] : [])
                    ]
                };
            });
            const edges = graph.edges.filter(edge => ids.has(edge.source) && ids.has(edge.target));
            const layout = await new ELK().layout({
                id: 'root',
                layoutOptions: { ...direction, 'elk.algorithm': 'layered', 'elk.edgeRouting': 'ORTHOGONAL', 'elk.hierarchyHandling': 'INCLUDE_CHILDREN', 'elk.layered.spacing.nodeNodeBetweenLayers': '60', 'elk.layered.spacing.edgeNodeBetweenLayers': '20', 'elk.spacing.nodeNode': '32', 'elk.spacing.edgeEdge': '10', 'elk.padding': '[top=0,left=0,bottom=0,right=0]' },
                children: children,
                edges: edges.map(edge => ({ id: edge.id, sources: [edge.source], targets: [edge.target] }))
            });
            if (id !== renderId) {
                return false;
            }

            // ELK places each box relative to the one holding it, so walk down adding up the offsets.
            const placed = new Map();
            const walk = (box, x, y) => (box.children ?? []).forEach(child => {
                placed.set(child.id, { x: x + child.x, y: y + child.y, width: child.width, height: child.height });
                walk(child, x + child.x, y + child.y);
            });
            walk(layout, 0, 0);
            linked.forEach(entry => {
                const hostBox = placed.get(entry.host.id);
                drawFrame(entry.host.label, hostBox.x, hostBox.y, hostBox.width, hostBox.height, layers.boxes);
                entry.items.filter(entry.isLinked).forEach(item => {
                    if (item.group) {
                        const box = placed.get(serviceGroupId(entry.host.id, item.group));
                        drawRoom(item.label, box.x, box.y, box.width, box.height, layers.boxes);
                    }
                    item.nodes.forEach(node => {
                        const box = placed.get(node.id);
                        drawNode(node, box.x, box.y, box.width, box.height, layers.nodes, true);
                    });
                });
                const block = placed.get(`block:${entry.host.id}`);
                if (block) {
                    drawColumns(blocks.get(`block:${entry.host.id}`), block.x, block.y);
                }
            });

            // A line comes back relative to the box ELK names as its container: a project box, a host, or the whole map.
            const defs = create('defs', {}, layers.edges);
            const marker = create('marker', { id: 'service-arrow', viewBox: '0 0 10 10', refX: 9, refY: 5, markerWidth: 8, markerHeight: 8, markerUnits: 'userSpaceOnUse', orient: 'auto-start-reverse' }, defs);
            create('path', { d: 'M 0 0 L 10 5 L 0 10 z', class: 'service-arrow' }, marker);
            (layout.edges ?? []).forEach(layoutEdge => {
                const edge = edges.find(item => item.id === layoutEdge.id);
                const origin = placed.get(layoutEdge.container) ?? { x: 0, y: 0 };
                (layoutEdge.sections ?? []).forEach(section => drawServiceLink(edge, [section.startPoint, ...(section.bendPoints ?? []), section.endPoint].map(point => ({ x: point.x + origin.x, y: point.y + origin.y })), layers));
            });
            bounds.width = layout.width;
            bounds.height = layout.height;
            top = layout.height + hostGap * 2;
        }

        // Hosts with nothing linked stack into piles no taller than the tallest of them, keeping name order, so small hosts sit on top of each other beside the big ones.
        const measured = hosts.filter(entry => !entry.linked).map(entry => {
            const columns = columnsOf(entry.items);
            const size = sizeOfColumns(columns);
            const innerWidth = Math.max(size.width, textWidth(entry.host.label, true) * 14 / 12 + groupPadding);
            return { host: entry.host, columns: columns, width: innerWidth + groupPadding * 2, height: titleHeight + size.height + groupPadding };
        });
        if (!measured.length) {
            return true;
        }
        const tallest = Math.max(...measured.map(item => item.height));
        const piles = [];
        measured.forEach(item => {
            const pile = piles[piles.length - 1];
            if (pile && pile.height + hostGap + item.height <= tallest) {
                pile.items.push(item);
                pile.height += hostGap + item.height;
                pile.width = Math.max(pile.width, item.width);
            } else {
                piles.push({ items: [item], height: item.height, width: item.width });
            }
        });

        // Piles wrap into rows roughly the shape of the map area, below anything ELK laid out, so it fills the screen rather than running off to one side.
        const box = container.getBoundingClientRect();
        const area = piles.reduce((sum, pile) => sum + (pile.width + hostGap) * (pile.height + hostGap), 0);
        const rowWidth = Math.max(bounds.width, ...piles.map(pile => pile.width), Math.sqrt(area * box.width / Math.max(box.height, 1)));
        let left = 0;
        let rowHeight = 0;
        piles.forEach(pile => {
            if (left > 0 && left + pile.width > rowWidth) {
                left = 0;
                top += rowHeight + hostGap;
                rowHeight = 0;
            }
            let y = top;
            pile.items.forEach(item => {
                drawFrame(item.host.label, left, y, pile.width, item.height, layers.boxes);
                drawColumns(item.columns, left + groupPadding, y + titleHeight);
                y += item.height + hostGap;
            });
            bounds.width = Math.max(bounds.width, left + pile.width);
            bounds.height = Math.max(bounds.height, top + pile.height);
            rowHeight = Math.max(rowHeight, pile.height);
            left += pile.width + hostGap;
        });
        return true;
    };

    // A link between services: a grey line with an arrow pointing at the service being used.
    const drawServiceLink = (edge, points, layers) => {
        const clean = tidy(points);
        if (clean.length < 2) {
            return;
        }
        const d = roundedPath(clean);
        create('path', { d: d, class: 'map-halo' }, layers.halos);
        const group = create('g', { class: `map-edge service-link ${edge.kind.toLowerCase()}`, 'data-edge': edge.id }, layers.edges);
        create('path', { d: d, class: 'map-edge-hit' }, group);
        create('path', { d: d, class: 'map-edge-line', 'marker-end': 'url(#service-arrow)' }, group);
        group.addEventListener('click', event => {
            event.stopPropagation();
            showServiceLink(edge.id);
            focusEdges([edge]);
        });
    };

    // The key only lists what the current view can show. Port colours count too where ports are drawn, which isn't on the Floor Plan's name-only chips.
    const legend = document.getElementById('legend');
    const updateKey = mode => {
        if (!legend) {
            return;
        }
        const shown = new Set();
        const lines = [...graph.edges, ...(graph.zigbeeArcs ?? [])];
        // The coordinator's USB cable is drawn grey but isn't a cable that's down, so it isn't counted.
        lines.filter(edge => edge.id !== 'zl-usb').forEach(edge => shown.add(edge.zigbee ? 'zigbee' : edge.wireless ? 'wifi' : edge.style));
        if (lines.some(edge => edge.poE)) {
            shown.add('poe');
        }
        if (mode !== 'floorplan') {
            graph.nodes.forEach(node => node.ports.forEach(port => shown.add(port.style)));
        }
        graph.nodes.forEach(node => {
            const zigbeeOnly = zigbeeById.has(node.id) && !rawById.has(node.id);
            shown.add(node.hue ? 'hue' : ['gateway', 'switch', 'accesspoint'].includes(node.kind) ? 'unifi' : node.kind === 'client' ? zigbeeOnly ? 'end-device' : 'client' : node.kind);
        });
        legend.querySelectorAll('[data-key]').forEach(item => item.hidden = !shown.has(item.dataset.key));
        legend.querySelectorAll('ul').forEach(list => list.hidden = !list.querySelector('[data-key]:not([hidden])'));
    };

    let renderId = 0;
    const render = async mode => {
        const id = ++renderId;
        if ((mode === 'zigbee' || mode === 'all' || mode === 'floorplan') && !zigbee) {
            try {
                zigbee = await (await fetch(container.dataset.zigbeeUrl)).json();
            } catch (error) {
                // Without Home Assistant the All view still works, just without the Zigbee devices.
                zigbee = { nodes: [], edges: [] };
            }
            zigbee.nodes.forEach(node => zigbeeById.set(node.id, node));
            if (id !== renderId) {
                return;
            }
        }
        if (mode === 'services' && !services) {
            try {
                const data = await (await fetch(container.dataset.servicesUrl)).json();
                services = data.hosts;
                serviceLinks = data.links;
            } catch (error) {
                services = [];
                serviceLinks = [];
            }
            services.forEach(host => host.services.forEach(service => servicesById.set(service.id, { ...service, host: host })));
            if (id !== renderId) {
                return;
            }
        }
        useView(mode === 'services' ? servicesView() : mode === 'network' ? (vlanSelect.value ? vlanView(vlanSelect.value) : raw) : mode === 'zigbee' ? zigbeeView() : mode === 'all' ? withZigbee() : withZigbee(false));
        tabs.querySelectorAll('button').forEach(button => button.classList.toggle('active', button.dataset.view === mode));
        updateKey(mode);
        vlanSelect.hidden = mode !== 'network' || !raw.networks.length;
        zigbeeSelect.hidden = mode !== 'zigbee';
        details.textContent = defaultDetails;
        if (mode === 'services' && !graph.nodes.length) {
            container.replaceChildren();
            container.textContent = 'No services yet. Hosts report them to /api/services/{host}/systemd.';
            return;
        }
        if ((mode === 'network' || mode === 'zigbee') && !graph.nodes.length) {
            container.replaceChildren();
            container.textContent = mode === 'zigbee' ? zigbeeSelect.value === 'hue' ? 'No Hue devices yet. They come from the Hue Bridge once its app key is set.' : 'No Zigbee devices yet. They come from Home Assistant once its URL and token are set.' : 'Nothing found on this network.';
            return;
        }
        if (mode === 'floorplan' && !graph.floors.length) {
            container.replaceChildren();
            container.textContent = 'No floors yet. Add floors and rooms on the Floor Plan page.';
            return;
        }
        const svg = create('svg', { class: 'map-svg' });
        const viewport = create('g', {}, svg);
        const layers = { boxes: create('g', {}, viewport), trunks: create('g', {}, viewport), halos: create('g', {}, viewport), edges: create('g', {}, viewport), nodes: create('g', {}, viewport), labels: create('g', { class: 'map-labels' }, viewport) };
        labelLayer = layers.labels;
        const bounds = { width: 0, height: 0 };

        if (mode === 'services') {
            if (!await drawServices(layers, bounds, id)) {
                return;
            }
        } else if (mode === 'network' || !graph.floors.length) {
            // The network on its own, laid out and routed by ELK.
            const result = await layoutGroup({ nodes: graph.nodes, edges: graph.edges });
            if (id !== renderId) {
                return;
            }
            drawGroup(result, layers, false);
            bounds.width = result.width;
            bounds.height = result.height;
        } else if (mode === 'all' || mode === 'zigbee') {
            // The network grouped into floors and rooms.
            const layout = await layoutNested(mode === 'all');
            if (id !== renderId) {
                return;
            }
            const size = drawNested(layout, layers);
            bounds.width = size.width;
            bounds.height = size.height;
        } else if (!await drawFloorPlan(layers, bounds, id)) {
            return;
        }
        currentSvg = svg;

        // Pan and zoom.
        const view = { scale: 1, x: 0, y: 0 };
        const apply = () => viewport.setAttribute('transform', `translate(${view.x}, ${view.y}) scale(${view.scale})`);
        const fit = () => {
            const box = container.getBoundingClientRect();
            // Leave room at the top for the picker when one is showing.
            const reserved = vlanSelect.hidden && zigbeeSelect.hidden ? 0 : 44;
            view.scale = Math.min(box.width / (bounds.width + 40), (box.height - reserved) / (bounds.height + 40), 1.5);
            view.x = (box.width - bounds.width * view.scale) / 2;
            view.y = 20 + reserved;
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

    const modes = ['all', 'floorplan', 'network', 'zigbee', 'services'];
    let stored = null;
    try {
        stored = localStorage.getItem('holamap.view');
    } catch (error) {
        stored = null;
    }
    let mode = stored === 'unifi' ? 'network' : modes.includes(stored) ? stored : 'all';
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
    vlanSelect.addEventListener('change', () => {
        try {
            localStorage.setItem('holamap.vlan', vlanSelect.value);
        } catch (error) {
            // Storage is only a convenience for remembering the network.
        }
        render('network');
    });
    zigbeeSelect.addEventListener('change', () => {
        try {
            localStorage.setItem('holamap.zigbee', zigbeeSelect.value);
        } catch (error) {
            // Storage is only a convenience for remembering the choice.
        }
        render('zigbee');
    });
    await render(mode);
})();
