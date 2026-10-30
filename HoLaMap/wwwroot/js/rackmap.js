// Draws one rack to scale with the ports on each device's front, the cables between them, and the cables leaving the rack with a label saying where each one goes.
window.RackMap = (function () {
    const svgNs = 'http://www.w3.org/2000/svg';
    const colours = { 'patch': '#b0b4bc', 'port-wan': '#1e88c8', 'port-1000': '#4a8a1c', 'port-100': '#e6c229', 'port-10': '#e67e22', 'port-down': '#7b8794' };
    const darkText = ['patch', 'port-100', 'port-down'];
    const unitHeight = 44;
    const columnWidth = 150;
    const columns = 4;
    const inset = 2;
    const portSize = 14;
    const portPitch = 18;
    const labelHeight = 18;
    const trackGap = 14;
    const channelGap = 8;
    const laneGap = 10;
    const railWidth = 28;
    const frame = 10;
    const labelGap = 28;
    const cornerRadius = 5;

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

    // A subtle zigzag along each straight run, used for cables carrying PoE.
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

    // Gives each interval the lowest track no overlapping interval already holds, shortest first so short runs stay close in.
    const assignTracks = items => {
        const taken = [];
        [...items].sort((a, b) => (a.to - a.from) - (b.to - b.from)).forEach(item => {
            let track = 0;
            while ((taken[track] ?? []).some(other => other.from < item.to + 6 && item.from < other.to + 6)) {
                track++;
            }
            (taken[track] ??= []).push(item);
            item.track = track;
        });
        return taken.length;
    };

    const draw = (host, raw, rackId) => {
        host.replaceChildren();
        const rack = raw.racks.find(item => item.id === rackId);
        if (!rack) {
            host.textContent = 'Rack not found.';
            return;
        }
        const nodesById = new Map(raw.nodes.map(node => [node.id, node]));
        const portsById = new Map(raw.nodes.flatMap(node => node.ports.map(port => [port.id, { ...port, node: node }])));
        const roomsById = new Map(raw.floors.flatMap(floor => floor.rooms.map(room => [room.id, room])));
        const racksById = new Map(raw.racks.map(item => [item.id, item]));
        const devices = raw.nodes.filter(node => node.rack === rackId && node.rackU > 0);

        // Patch panels are drawn like devices with a socket per port. A cable on the front and one on the back of the same port carry a link between those two ends, so that link is drawn through the panel instead of straight across.
        // With only the front patched, the link from that port to somewhere outside the rack is taken to run out of the back.
        const patchNodes = (rack.panels ?? []).filter(panel => panel.kind === 'patch').map(panel => ({ id: panel.id, label: panel.name, kind: 'patch', patch: panel, detail: `${panel.ports} Port Patch Panel`, online: true, rackU: panel.rackU, rackColumn: panel.rackColumn, rackWidth: panel.rackWidth, rackHeight: panel.rackHeight, ports: Array.from({ length: panel.ports }, (unused, index) => ({ id: `${panel.id}-${index + 1}`, index: index + 1, style: 'patch', detail: `${panel.name} Port ${index + 1}` })) }));
        patchNodes.forEach(node => {
            nodesById.set(node.id, node);
            node.ports.forEach(port => portsById.set(port.id, { ...port, node: node }));
        });
        devices.push(...patchNodes);
        const inRack = new Set(devices.map(node => node.id));
        const rackOf = id => nodesById.get(id)?.rack === rackId || patchNodes.some(node => node.id === id);
        const matches = (edge, end, patch) => edge[end] === patch.node && (!patch.port || !edge[`${end}Port`] || edge[`${end}Port`] === patch.port);
        const routed = new Set();
        const patchEdges = patchNodes.flatMap(node => node.ports.flatMap(port => {
            const front = node.patch.patches.find(patch => patch.number === port.index && patch.side === 'front');
            let back = node.patch.patches.find(patch => patch.number === port.index && patch.side === 'back');
            let carried = front && back ? raw.edges.filter(edge => (matches(edge, 'source', front) && matches(edge, 'target', back)) || (matches(edge, 'source', back) && matches(edge, 'target', front))) : [];
            if (front && !back) {
                carried = raw.edges.filter(edge => (matches(edge, 'source', front) && !rackOf(edge.target)) || (matches(edge, 'target', front) && !rackOf(edge.source)));
                const far = carried[0];
                back = far ? (matches(far, 'source', front) ? { node: far.target, port: far.targetPort } : { node: far.source, port: far.sourcePort }) : null;
            }
            carried.forEach(edge => routed.add(edge.id));
            const base = carried[0];
            const style = { style: base?.style ?? 'patch', poE: base?.poE ?? false, online: base?.online ?? true, detail: base ? base.detail : 'Patch Cable' };
            return [
                ...(front ? [{ ...style, id: `${port.id}-front`, source: front.node, sourcePort: front.port, target: node.id, targetPort: port.id }] : []),
                ...(back ? [{ ...style, id: `${port.id}-back`, source: node.id, sourcePort: port.id, target: back.node, targetPort: back.port, back: true }] : [])
            ];
        }));
        const edges = [...raw.edges.filter(edge => !routed.has(edge.id)), ...patchEdges].filter(edge => edge.source !== edge.target && (inRack.has(edge.source) || inRack.has(edge.target)));
        const height = rack.heightU;

        // Where a device ends up: a box per device and a square per port, in rack coordinates before the cable channels are added.
        const measure = document.createElement('canvas').getContext('2d');
        const fontFamily = getComputedStyle(host).fontFamily;
        const textWidth = (text, size, bold) => {
            measure.font = `${bold ? 600 : 400} ${size}px ${fontFamily}`;
            return measure.measureText(text).width;
        };

        // Devices without ports get a socket for each cable so every cable has somewhere to plug in.
        const ends = [];
        const boxes = devices.map(node => {
            const sockets = node.ports.length ? node.ports.map(port => ({ ...port, node: node })) : edges.filter(edge => edge.source === node.id || edge.target === node.id).map(edge => ({ id: `${edge.id}-${edge.source === node.id ? 'from' : 'to'}`, index: '', style: edge.style, detail: `${node.label} Network Port`, node: node }));
            const width = node.rackWidth * columnWidth - inset * 2;
            const boxHeight = node.rackHeight * unitHeight - inset * 2;
            const perRow = Math.max(1, Math.floor((width - 12) / portPitch));
            const besideLabel = Math.max(1, Math.floor((width - textWidth(node.label, 12, true) - 24) / portPitch));
            const rowsBeside = Math.max(1, Math.floor((boxHeight - 2) / portPitch));
            // Ports sit to the right of the name, in as many rows as the face allows, or under the name on a narrow device.
            const labelWidth = textWidth(node.label, 12, true);
            let rows = Math.ceil(sockets.length / besideLabel);
            let left = Math.ceil(labelWidth) + 20;
            let top = (Math.min(boxHeight, rows * portPitch + 12) - rows * portPitch) / 2 + 2;
            let labelY = top + rows * portPitch / 2 + 2;
            if (rows > rowsBeside || !sockets.length) {
                rows = Math.ceil(sockets.length / perRow);
                left = 8;
                top = labelHeight + 2;
                labelY = 14;
            }
            if (!sockets.length) {
                labelY = Math.min(boxHeight, unitHeight - inset * 2) / 2 + 4;
            }
            // Two rows are numbered odd on top and even below, like the front of a switch. Patch panels count along the top row first.
            const perColumn = Math.max(1, rows);
            const oddEven = rows === 2 && !node.patch;
            const box = { node: node, width: width, height: boxHeight, rows: rows, sockets: sockets, labelY: labelY };
            sockets.forEach((socket, index) => {
                const column = oddEven ? Math.floor(index / 2) : index % Math.ceil(sockets.length / perColumn);
                const row = oddEven ? index % 2 : Math.floor(index / Math.ceil(sockets.length / perColumn));
                const end = { socket: socket, box: box, x: left + column * portPitch, y: top + row * portPitch, row: row, cables: [] };
                socket.end = end;
                ends.push(end);
            });
            return box;
        });
        const endFor = (nodeId, portId, edge, side) => ends.find(end => end.socket.id === portId) ?? ends.find(end => end.socket.id === `${edge.id}-${side}`) ?? ends.find(end => end.box.node.id === nodeId);

        // Each cable leaves its port up or down into the channel along the device's top or bottom edge. Channel b sits between unit b and unit b + 1.
        const channels = Array.from({ length: height + 1 }, () => []);
        const cables = edges.map(edge => {
            const fromInside = inRack.has(edge.source);
            const toInside = inRack.has(edge.target);
            const from = fromInside ? endFor(edge.source, edge.sourcePort, edge, 'from') : null;
            const to = toInside ? endFor(edge.target, edge.targetPort, edge, 'to') : null;
            const cable = { edge: edge, from: from, to: to, external: !(from && to) };
            if (cable.external) {
                const inside = from ?? to;
                const otherId = from ? edge.target : edge.source;
                const otherPort = portsById.get(from ? edge.targetPort : edge.sourcePort);
                const other = nodesById.get(otherId);
                const room = roomsById.get(other?.room)?.name;
                const otherRack = racksById.get(other?.rack)?.name;
                cable.text = `→ ${other?.label ?? 'Unknown'}${otherPort ? ` Port ${otherPort.index}` : ''}${room || otherRack ? ` · ${[otherRack, room].filter(part => part).join(', ')}` : ''}`;
                cable.inside = inside;
            }
            [from, to].filter(end => end).forEach(end => end.cables.push(cable));
            return cable;
        });

        const topOf = box => box.node.rackU + box.node.rackHeight - 1;
        const bottomOf = box => box.node.rackU - 1;
        const channelOf = (end, up) => up ? topOf(end.box) : bottomOf(end.box);
        const centreU = end => end.box.node.rackU + end.box.node.rackHeight / 2;
        // A port in the top row of two exits upwards and one in the bottom row downwards; a single row picks the side the cable is heading.
        // A patch panel port with cables on both sides sends them out opposite ways so they don't share a drop.
        const pickUp = (end, wantUp) => end.taken !== undefined && end.box.node.patch ? !end.taken : end.box.rows === 2 && !end.box.node.patch ? end.row === 0 : wantUp;
        const take = (end, up) => {
            end.taken = up;
            return up;
        };
        [...cables].sort((a, b) => a.external - b.external).forEach(cable => {
            if (cable.external) {
                const end = cable.inside;
                const above = channels[topOf(end.box)].length;
                const below = channels[bottomOf(end.box)].length;
                cable.up = take(end, pickUp(end, above <= below));
                cable.channel = channelOf(end, cable.up);
                cable.run = { cable: cable, kind: 'out' };
                channels[cable.channel].push(cable.run);
                return;
            }
            const sourceAbove = centreU(cable.from) < centreU(cable.to);
            const sameLevel = centreU(cable.from) === centreU(cable.to);
            cable.fromUp = take(cable.from, pickUp(cable.from, sameLevel || sourceAbove));
            cable.toUp = take(cable.to, pickUp(cable.to, sameLevel || !sourceAbove));
            const fromChannel = channelOf(cable.from, cable.fromUp);
            const toChannel = channelOf(cable.to, cable.toUp);
            if (fromChannel === toChannel) {
                cable.direct = true;
                cable.run = { cable: cable, kind: 'direct' };
                channels[fromChannel].push(cable.run);
                cable.channel = fromChannel;
            } else {
                cable.runs = [{ cable: cable, kind: 'lane', end: cable.from }, { cable: cable, kind: 'lane', end: cable.to }];
                channels[fromChannel].push(cable.runs[0]);
                channels[toChannel].push(cable.runs[1]);
                cable.channels = [fromChannel, toChannel];
            }
        });

        // Horizontal positions: U numbers on the left rail, then the cable manager lanes, then the devices.
        const portCentre = end => ({ x: deviceLeft + end.box.node.rackColumn * columnWidth + inset + end.x + portSize / 2 });
        const internal = cables.filter(cable => !cable.external && !cable.direct);
        // Lanes are worked out after the channel heights, so lay out the channels with lane cables running to a placeholder left edge.
        const laneSpan = cable => [cable.channels[0], cable.channels[1]].sort((a, b) => a - b);
        const laneItems = internal.map(cable => {
            const [low, high] = laneSpan(cable);
            return { cable: cable, from: low, to: high };
        });
        const laneCount = assignTracks(laneItems);
        laneItems.forEach(item => {
            item.cable.lane = item.track;
        });
        const laneWidth = laneCount ? laneCount * laneGap + 8 : 0;
        const deviceLeft = frame + railWidth + laneWidth;
        const deviceRight = deviceLeft + columns * columnWidth;
        const frameRight = deviceRight + frame;
        const laneX = lane => deviceLeft - 8 - lane * laneGap;

        // Tracks within each channel, so no two cables share a horizontal run.
        const channelTracks = channels.map(runs => {
            const items = runs.map(run => {
                const cable = run.cable;
                let from;
                let to;
                if (run.kind === 'out') {
                    from = portCentre(cable.inside).x;
                    to = frameRight + labelGap;
                } else if (run.kind === 'direct') {
                    [from, to] = [portCentre(cable.from).x, portCentre(cable.to).x].sort((a, b) => a - b);
                } else {
                    from = laneX(cable.lane);
                    to = portCentre(run.end).x;
                }
                run.from = from;
                run.to = to;
                return run;
            });
            return assignTracks(items);
        });
        // Track 0 goes next to the device most of the channel's cables drop from, so the drops don't cross each other's runs.
        const downwards = channels.map(runs => {
            const fromBelow = runs.filter(run => run.kind === 'out' ? run.cable.up : run.kind === 'lane' ? (run.end === run.cable.from ? run.cable.fromUp : run.cable.toUp) : run.cable.fromUp).length;
            return fromBelow * 2 < runs.length;
        });

        // Vertical positions: each channel is as tall as the cables running through it.
        const channelHeight = channelTracks.map(count => channelGap + count * trackGap);
        const channelTop = [];
        const unitTop = [];
        let y = frame;
        for (let b = height; b >= 0; b--) {
            channelTop[b] = y;
            y += channelHeight[b];
            if (b > 0) {
                unitTop[b] = y;
                y += unitHeight;
            }
        }
        const frameBottom = y + frame;
        const trackY = (channel, track) => downwards[channel] ? channelTop[channel] + channelGap / 2 + trackGap / 2 + track * trackGap : channelTop[channel] + channelHeight[channel] - channelGap / 2 - trackGap / 2 - track * trackGap;
        const boxTop = box => unitTop[topOf(box)] + inset;
        const boxBottom = box => unitTop[box.node.rackU] + unitHeight - inset;
        boxes.forEach(box => {
            box.x = deviceLeft + box.node.rackColumn * columnWidth + inset;
            box.y = boxTop(box);
            box.drawHeight = boxBottom(box) - box.y;
        });
        // Ports stay on the device's top face even when the device stretches over channels inside its span.
        const portPoint = end => ({ x: end.box.x + end.x + portSize / 2, y: end.box.y + end.y + portSize / 2 });
        const exitPoint = (end, up) => ({ x: portPoint(end).x, y: up ? end.box.y + end.y : end.box.y + end.y + portSize });

        // Labels go to the right of the rack, so measure them to size the drawing.
        const externals = cables.filter(cable => cable.external);
        const labelsWidth = externals.reduce((widest, cable) => Math.max(widest, textWidth(cable.text, 12, false)), 0);
        const totalWidth = frameRight + (externals.length ? labelGap + labelsWidth + 12 : frame);

        const svg = create('svg', { class: 'rackmap-svg', viewBox: `0 0 ${totalWidth} ${frameBottom}`, width: totalWidth, height: frameBottom });
        const info = document.createElement('p');
        info.className = 'rackmap-info';
        const defaultInfo = 'Select a cable, port or device to see its details.';
        info.textContent = defaultInfo;
        host.append(svg, info);

        // The rack frame, the U numbers on its left rail, and a faint line at each unit.
        create('rect', { x: 0, y: 0, width: frameRight, height: frameBottom, rx: 8, class: 'rackmap-frame' }, svg);
        create('rect', { x: frame, y: frame, width: frameRight - frame * 2, height: frameBottom - frame * 2, rx: 4, class: 'rackmap-inside' }, svg);
        if (laneWidth) {
            create('rect', { x: frame + railWidth, y: frame, width: laneWidth - 4, height: frameBottom - frame * 2, class: 'rackmap-manager' }, svg);
        }
        // A divider through the middle of each gap between units, drawn first so the devices and cables sit on top of it.
        for (let b = 0; b <= height; b++) {
            const middle = channelTop[b] + channelHeight[b] / 2;
            create('line', { x1: frame, y1: middle, x2: frameRight - frame, y2: middle, class: 'rackmap-divider' }, svg);
        }
        for (let unit = 1; unit <= height; unit++) {
            create('rect', { x: deviceLeft, y: unitTop[unit], width: deviceRight - deviceLeft, height: unitHeight, class: 'rackmap-unit' }, svg);
            create('text', { x: frame + railWidth - 8, y: unitTop[unit] + unitHeight / 2 + 4, class: 'rackmap-u' }, svg).textContent = unit;
        }

        const deviceLayer = create('g', {}, svg);
        const cableLayer = create('g', {}, svg);
        const labelLayer = create('g', {}, svg);

        // Selecting something fades everything not connected to it.
        const clear = () => {
            svg.classList.remove('focusing');
            svg.querySelectorAll('.related').forEach(element => element.classList.remove('related'));
            info.textContent = defaultInfo;
        };
        const focus = (cableList, deviceIds, text) => {
            clear();
            svg.classList.add('focusing');
            cableList.forEach(cable => svg.querySelectorAll(`[data-edge="${cable.edge.id}"]`).forEach(element => element.classList.add('related')));
            deviceIds.forEach(id => svg.querySelectorAll(`[data-node="${id}"]`).forEach(element => element.classList.add('related')));
            info.textContent = text;
        };
        svg.addEventListener('click', clear);
        const describe = cable => {
            const source = nodesById.get(cable.edge.source);
            const target = nodesById.get(cable.edge.target);
            const sourcePort = portsById.get(cable.edge.sourcePort);
            const targetPort = portsById.get(cable.edge.targetPort);
            return `${source?.label ?? 'Unknown'}${sourcePort ? ` Port ${sourcePort.index}` : ''} → ${target?.label ?? 'Unknown'}${targetPort ? ` Port ${targetPort.index}` : ''} · ${cable.edge.detail}`;
        };
        const endsOf = cable => [cable.from, cable.to].filter(end => end).map(end => end.box.node.id);

        // Blank and brush panels sit behind the cables like the devices, but have no ports.
        (rack.panels ?? []).filter(panel => panel.kind !== 'patch').forEach(panel => {
            const x = deviceLeft + panel.rackColumn * columnWidth + inset;
            const top = unitTop[panel.rackU + panel.rackHeight - 1] + inset;
            const width = panel.rackWidth * columnWidth - inset * 2;
            const panelHeight = unitTop[panel.rackU] + unitHeight - inset - top;
            const name = panel.kind === 'brush' ? 'Brush Panel' : 'Blank Panel';
            const group = create('g', { class: `rackmap-panel ${panel.kind}`, transform: `translate(${x}, ${top})` }, deviceLayer);
            create('rect', { width: width, height: panelHeight, rx: 4, class: 'rackmap-panel-box' }, group);
            create('title', {}, group).textContent = `${name} · U${panel.rackU}${panel.rackHeight > 1 ? `–U${panel.rackU + panel.rackHeight - 1}` : ''}`;
            [10, width - 10].forEach(cx => create('circle', { cx: cx, cy: panelHeight / 2, r: 2.5, class: 'rackmap-panel-screw' }, group));
            if (panel.kind === 'brush') {
                const slotHeight = Math.min(16, panelHeight - 16);
                const slotTop = (panelHeight - slotHeight) / 2;
                create('rect', { x: 22, y: slotTop, width: width - 44, height: slotHeight, rx: slotHeight / 2, class: 'rackmap-brush-slot' }, group);
                let bristles = '';
                for (let bx = 28; bx < width - 28; bx += 3) {
                    bristles += `M ${bx} ${slotTop + 2} L ${bx + 1} ${slotTop + slotHeight / 2 - 1} M ${bx} ${slotTop + slotHeight - 2} L ${bx + 1} ${slotTop + slotHeight / 2 + 1} `;
                }
                create('path', { d: bristles, class: 'rackmap-brush-bristles' }, group);
            }
            group.addEventListener('click', event => {
                event.stopPropagation();
                clear();
                info.textContent = group.querySelector('title').textContent;
            });
        });

        boxes.forEach(box => {
            const group = create('g', { class: `rackmap-device ${box.node.kind}${box.node.online ? '' : ' offline'}`, transform: `translate(${box.x}, ${box.y})`, 'data-node': box.node.id }, deviceLayer);
            create('rect', { width: box.width, height: box.drawHeight, rx: 4, class: 'rackmap-device-box' }, group);
            const label = create('text', { x: 8, y: box.labelY, class: 'rackmap-device-label' }, group);
            label.textContent = box.node.label;
            create('title', {}, group).textContent = box.node.detail;
            group.addEventListener('click', event => {
                event.stopPropagation();
                const related = cables.filter(cable => endsOf(cable).includes(box.node.id));
                focus(related, [box.node.id, ...related.flatMap(endsOf)], `${box.node.label} · ${box.node.detail}`);
            });
            box.sockets.forEach(socket => {
                const end = socket.end;
                const square = create('g', { class: 'rackmap-port', transform: `translate(${end.x}, ${end.y})` }, group);
                end.cables.forEach(cable => square.setAttribute('data-edge', cable.edge.id));
                create('rect', { width: portSize, height: portSize, rx: 2, fill: end.cables.length ? colours[box.node.patch ? end.cables[0].edge.style : socket.style] ?? '#7b8794' : '#3a414d', class: 'rackmap-port-box' }, square);
                const number = create('text', { x: portSize / 2, y: portSize / 2 + 3, class: 'rackmap-port-label' + (darkText.includes(box.node.patch ? end.cables[0]?.edge.style ?? 'patch' : socket.style) && end.cables.length ? ' dark' : '') }, square);
                number.textContent = socket.index;
                create('title', {}, square).textContent = socket.detail;
                square.addEventListener('click', event => {
                    event.stopPropagation();
                    focus(end.cables, [box.node.id, ...end.cables.flatMap(endsOf)], `${box.node.label}${socket.index ? ` Port ${socket.index}` : ''} · ${socket.detail}`);
                });
            });
        });

        const drawCable = (cable, points) => {
            const clean = tidy(points);
            const group = create('g', { class: 'rackmap-cable' + (cable.edge.online ? '' : ' offline') + (cable.edge.back ? ' back' : ''), 'data-edge': cable.edge.id }, cableLayer);
            create('path', { d: roundedPath(clean), class: 'rackmap-cable-hit' }, group);
            create('path', { d: cable.edge.poE ? zigzagPath(clean) : roundedPath(clean), class: 'rackmap-cable-line', stroke: colours[cable.edge.style] ?? '#7b8794' }, group);
            group.addEventListener('click', event => {
                event.stopPropagation();
                focus([cable], endsOf(cable), describe(cable));
            });
            return group;
        };

        cables.forEach(cable => {
            if (cable.external) {
                const end = cable.inside;
                const trackAt = trackY(cable.channel, cable.run.track);
                const exit = exitPoint(end, cable.up);
                const labelX = frameRight + labelGap;
                const group = drawCable(cable, [exit, { x: exit.x, y: trackAt }, { x: labelX - 6, y: trackAt }]);
                create('circle', { cx: labelX - 6, cy: trackAt, r: 3, fill: colours[cable.edge.style] ?? '#7b8794' }, group);
                const label = create('text', { x: labelX, y: trackAt + 4, class: 'rackmap-out-label', 'data-edge': cable.edge.id }, labelLayer);
                label.textContent = cable.text;
                label.addEventListener('click', event => {
                    event.stopPropagation();
                    focus([cable], endsOf(cable), describe(cable));
                });
                return;
            }
            if (cable.direct) {
                const trackAt = trackY(cable.channel, cable.run.track);
                const fromExit = exitPoint(cable.from, cable.fromUp);
                const toExit = exitPoint(cable.to, cable.toUp);
                drawCable(cable, [fromExit, { x: fromExit.x, y: trackAt }, { x: toExit.x, y: trackAt }, toExit]);
                return;
            }
            const fromY = trackY(cable.channels[0], cable.runs[0].track);
            const toY = trackY(cable.channels[1], cable.runs[1].track);
            const fromExit = exitPoint(cable.from, cable.fromUp);
            const toExit = exitPoint(cable.to, cable.toUp);
            const x = laneX(cable.lane);
            drawCable(cable, [fromExit, { x: fromExit.x, y: fromY }, { x: x, y: fromY }, { x: x, y: toY }, { x: toExit.x, y: toY }, toExit]);
        });

        if (!devices.length) {
            info.textContent = 'No devices placed in this rack yet.';
        }
    };

    // Any element with data-rack-map draws that rack from the map's graph data.
    const start = async () => {
        const hosts = [...document.querySelectorAll('[data-rack-map]')];
        if (!hosts.length) {
            return;
        }
        const response = await fetch(hosts[0].dataset.graphUrl);
        const raw = await response.json();
        hosts.forEach(host => draw(host, raw, host.dataset.rackMap));
    };
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }

    return { draw: draw };
})();
