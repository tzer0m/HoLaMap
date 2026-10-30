// Draws one rack to scale with the ports on each device's front, the cables between them, and the cables leaving the rack with a label saying where each one goes.
window.RackMap = (function () {
    const svgNs = 'http://www.w3.org/2000/svg';
    const colours = { 'patch': '#b0b4bc', 'port-wan': '#1e88c8', 'port-1000': '#4a8a1c', 'port-100': '#e6c229', 'port-10': '#e67e22', 'port-down': '#7b8794' };
    const darkText = ['patch', 'port-100', 'port-down'];
    // Cables are drawn in a darker shade of their speed colour, and PoE cables in the full colour so they stand out.
    const darken = hex => '#' + [1, 3, 5].map(i => Math.round(parseInt(hex.substr(i, 2), 16) * 0.65).toString(16).padStart(2, '0')).join('');
    const cableColour = edge => edge.poE ? colours[edge.style] ?? '#7b8794' : darken(colours[edge.style] ?? '#7b8794');
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
        const patchNodes = (rack.panels ?? []).filter(panel => panel.kind === 'patch').map(panel => ({ id: panel.id, label: panel.name, kind: 'patch', patch: panel, detail: `${panel.ports} Port Patch Panel`, online: true, rackU: panel.rackU, rackColumn: panel.rackColumn, rackWidth: panel.rackWidth, rackHeight: panel.rackHeight, ports: [...Array.from({ length: panel.ports }, (unused, index) => ({ id: `${panel.id}-${index + 1}`, index: index + 1, style: 'patch', detail: `${panel.name} Front Port ${index + 1}` })), ...Array.from({ length: panel.ports }, (unused, index) => ({ id: `${panel.id}-${index + 1}-r`, index: index + 1, style: 'patch', rear: true, detail: `${panel.name} Rear Port ${index + 1}` }))] }));
        patchNodes.forEach(node => {
            nodesById.set(node.id, node);
            node.ports.forEach(port => portsById.set(port.id, { ...port, node: node }));
        });
        devices.push(...patchNodes);
        const inRack = new Set(devices.map(node => node.id));
        const rackOf = id => nodesById.get(id)?.rack === rackId || patchNodes.some(node => node.id === id);
        const matches = (edge, end, patch) => edge[end] === patch.node && (!patch.port || !edge[`${end}Port`] || edge[`${end}Port`] === patch.port);
        const routed = new Set();
        // Every patch panel port has a front and a rear socket. A socket holds a cable to a device, or a jumper to another socket on a patch panel.
        const socketOf = (panel, number, side) => `${panel}-${number}${side === 'back' ? '-r' : ''}`;
        const pairOf = socket => socket.endsWith('-r') ? socket.slice(0, -2) : `${socket}-r`;
        const panelOf = socket => socket.substring(0, socket.indexOf('-'));
        const devicesAt = new Map();
        const jumpers = new Map();
        patchNodes.forEach(node => node.patch.patches.forEach(patch => {
            const socket = socketOf(node.id, patch.number, patch.side);
            if (patch.panel) {
                jumpers.set(socket, socketOf(patch.panel, patch.targetNumber, patch.targetSide));
            } else {
                devicesAt.set(socket, { node: patch.node, port: patch.port });
            }
        }));
        // Follows a cable through the panel from one socket: across to the other side of its port, then along any jumper, until it reaches a device or a socket with nothing in it.
        const walk = start => {
            const path = [start];
            let socket = pairOf(start);
            while (path.length < 500) {
                path.push(socket);
                if (devicesAt.has(socket)) {
                    return { path: path, far: devicesAt.get(socket), end: socket };
                }
                const next = jumpers.get(socket);
                if (!next || path.includes(next)) {
                    break;
                }
                path.push(next);
                socket = pairOf(next);
            }
            return { path: path, far: null, end: socket };
        };
        // The link between the devices at either end of a route through the panel is drawn along that route instead of straight across.
        // A route from a front socket that ends with nothing plugged in is taken to carry that device's link out of the rack from where the route stops.
        const defaultStyle = { style: 'patch', poE: false, online: true, detail: 'Patch Cable' };
        const styleOf = new Map();
        const inferred = [];
        const visited = new Set();
        devicesAt.forEach((near, start) => {
            if (visited.has(start)) {
                return;
            }
            const route = walk(start);
            route.path.forEach(socket => visited.add(socket));
            let carried = route.far ? raw.edges.filter(edge => (matches(edge, 'source', near) && matches(edge, 'target', route.far)) || (matches(edge, 'source', route.far) && matches(edge, 'target', near))) : [];
            if (!route.far && !start.endsWith('-r')) {
                carried = raw.edges.filter(edge => (matches(edge, 'source', near) && !rackOf(edge.target)) || (matches(edge, 'target', near) && !rackOf(edge.source)));
            }
            carried.forEach(edge => routed.add(edge.id));
            const base = carried[0];
            const style = base ? { style: base.style, poE: base.poE, online: base.online, detail: base.detail, route: start, splitter: base.splitter, splitterAt: base.target } : { ...defaultStyle, route: start };
            route.path.forEach(socket => styleOf.set(socket, style));
            if (!route.far && base) {
                inferred.push({ ...style, id: `${route.end}-inferred`, source: panelOf(route.end), sourcePort: route.end, target: matches(base, 'source', near) ? base.target : base.source, targetPort: matches(base, 'source', near) ? base.targetPort : base.sourcePort, back: route.end.endsWith('-r') });
            }
        });
        const patchEdges = [
            ...[...devicesAt].map(([socket, far]) => socket.endsWith('-r') ? { ...(styleOf.get(socket) ?? defaultStyle), id: `${socket}-cable`, source: panelOf(socket), sourcePort: socket, target: far.node, targetPort: far.port, back: true } : { ...(styleOf.get(socket) ?? defaultStyle), id: `${socket}-cable`, source: far.node, sourcePort: far.port, target: panelOf(socket), targetPort: socket }),
            ...[...jumpers].filter(([from, to]) => from < to || !jumpers.has(to)).map(([from, to]) => ({ ...(styleOf.get(from) ?? defaultStyle), id: `${from}-jumper`, source: panelOf(from), sourcePort: from, target: panelOf(to), targetPort: to, back: from.endsWith('-r') && to.endsWith('-r') })),
            ...inferred
        ];
        const edges = [...raw.edges.filter(edge => !routed.has(edge.id)), ...patchEdges].filter(edge => (edge.source !== edge.target || (edge.sourcePort && edge.targetPort && edge.sourcePort !== edge.targetPort)) && (inRack.has(edge.source) || inRack.has(edge.target)));
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
            // A patch panel has a column per port with its front and rear sockets stacked, in rows of up to 24 ports. Which socket goes on top is settled once the cables are known.
            if (node.patch) {
                const perPatchRow = Math.min(node.patch.ports, 24);
                const sideRows = Math.ceil(node.patch.ports / perPatchRow);
                const patchLeft = Math.ceil(textWidth(node.label, 12, true)) + 20;
                const patchRows = sideRows * 2;
                const box = { node: node, width: width, height: boxHeight, rows: patchRows, sockets: sockets, labelY: 2 + ((patchRows - 1) * portPitch + portSize) / 2 + 4, need: 4 + (patchRows - 1) * portPitch + portSize };
                sockets.forEach(socket => {
                    const number = socket.index - 1;
                    const row = Math.floor(number / perPatchRow) * 2 + (socket.rear ? 1 : 0);
                    const end = { socket: socket, box: box, x: patchLeft + (number % perPatchRow) * portPitch, y: 2 + row * portPitch, row: row, cables: [] };
                    socket.end = end;
                    ends.push(end);
                });
                return box;
            }
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
        const cables = edges.map(edge => {
            const fromInside = inRack.has(edge.source);
            const toInside = inRack.has(edge.target);
            const from = fromInside ? endFor(edge.source, edge.sourcePort, edge, 'from') : null;
            const to = toInside ? endFor(edge.target, edge.targetPort, edge, 'to') : null;
            const cable = { edge: edge, from: from, to: to, external: !(from && to), sideKey: { side: edge.id } };
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

        // Lays the rack out for one set of choices. A choice flips a free cable to leave its port the other way, or swaps which socket of a patch panel port goes on top.
        const plan = choice => {
            const channels = Array.from({ length: height + 1 }, () => []);
            cables.forEach(cable => {
                cable.direct = false;
                cable.run = null;
                cable.runs = null;
                cable.channels = null;
            });
            // On each patch panel port, the socket whose cable heads up goes on top and the other below, so neither cable has to cross the port. With no preference the front goes on top.
            const middleU = box => box.node.rackU + box.node.rackHeight / 2;
            const heading = end => end.cables.map(cable => {
                const other = cable.from === end ? cable.to : cable.from;
                return cable.external || !other || other.box === end.box ? null : middleU(other.box) > middleU(end.box);
            }).find(up => up !== null) ?? null;
            // Jumpers between two sockets on the same panel run in a gap between its two rows of sockets, each on its own track, so they never leave the panel.
            boxes.filter(box => box.node.patch).forEach(box => {
                const jumpers = cables.filter(cable => !cable.external && cable.from.box === box && cable.to.box === box);
                jumpers.forEach(cable => cable.jumper = true);
                const jumperItems = jumpers.map(cable => ({ cable: cable, from: Math.min(cable.from.x, cable.to.x), to: Math.max(cable.from.x, cable.to.x) }));
                const jumperTracks = assignTracks(jumperItems);
                jumperItems.forEach(item => item.cable.jumperTrack = item.track);
                const gap = jumperTracks ? 8 + jumperTracks * 5 : portPitch - portSize;
                const rowY = row => 2 + Math.floor(row / 2) * (portSize * 2 + gap + 4) + (row % 2) * (portSize + gap);
                box.jumperY = (row, track) => rowY(row - row % 2) + portSize + 4 + 2 + track * 5;
                const byNumber = new Map();
                ends.filter(end => end.box === box).forEach(end => byNumber.set(end.socket.index, [...(byNumber.get(end.socket.index) ?? []), end]));
                const pairs = [...byNumber.values()].map(pair => ({ front: pair.find(end => !end.socket.rear), rear: pair.find(end => end.socket.rear) }));
                pairs.sort((a, b) => (heading(a.front) === null && heading(a.rear) === null) - (heading(b.front) === null && heading(b.rear) === null));
                pairs.forEach(({ front, rear }) => {
                    const frontUp = heading(front);
                    const rearUp = heading(rear);
                    const frontOnTop = (frontUp !== null ? frontUp : rearUp !== null ? !rearUp : true) !== choice.has(front.socket.id);
                    const baseRow = front.row - front.row % 2;
                    front.row = baseRow + (frontOnTop ? 0 : 1);
                    rear.row = baseRow + (frontOnTop ? 1 : 0);
                    front.y = rowY(front.row);
                    rear.y = rowY(rear.row);
                });
                const lastRow = Math.max(...pairs.flatMap(pair => [pair.front.row, pair.rear.row]));
                box.need = rowY(lastRow) + portSize + 4;
                box.labelY = box.need / 2 + 4;
            });

            const topOf = box => box.node.rackU + box.node.rackHeight - 1;
            const bottomOf = box => box.node.rackU - 1;
            const channelOf = (end, up) => up ? topOf(end.box) : bottomOf(end.box);
            const centreU = end => end.box.node.rackU + end.box.node.rackHeight / 2;
            // A port in the top row of two exits upwards and one in the bottom row downwards; a single row picks the side the cable is heading.
            // A patch panel socket on top leaves upwards and one underneath leaves downwards.
            const pickUp = (end, wantUp) => end.box.node.patch ? end.row % 2 === 0 : end.box.rows === 2 ? end.row === 0 : wantUp;
            const take = (end, up) => {
                end.taken = up;
                return up;
            };
            [...cables].filter(cable => !cable.jumper).sort((a, b) => a.external - b.external).forEach(cable => {
                if (cable.external) {
                    const end = cable.inside;
                    const above = channels[topOf(end.box)].length;
                    const below = channels[bottomOf(end.box)].length;
                    cable.up = take(end, pickUp(end, (above <= below) !== choice.has(cable)));
                    cable.channel = channelOf(end, cable.up);
                    cable.run = { cable: cable, kind: 'out' };
                    channels[cable.channel].push(cable.run);
                    return;
                }
                const sourceAbove = centreU(cable.from) < centreU(cable.to);
                const sameLevel = centreU(cable.from) === centreU(cable.to);
                cable.fromUp = take(cable.from, pickUp(cable.from, (sameLevel || sourceAbove) !== choice.has(cable)));
                cable.toUp = take(cable.to, pickUp(cable.to, (sameLevel || !sourceAbove) !== choice.has(cable)));
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

            // Horizontal positions: U numbers on the left rail, a cable manager down each side, and the devices between them.
            // A cable between two units takes the manager nearer its ports, unless a choice sends it round the other side.
            const portCentre = end => ({ x: deviceLeft + end.box.node.rackColumn * columnWidth + inset + end.x + portSize / 2 });
            const internal = cables.filter(cable => !cable.external && !cable.direct && !cable.jumper);
            // Lanes are worked out after the channel heights, so lay out the channels with lane cables running to a placeholder left edge.
            const laneSpan = cable => [cable.channels[0], cable.channels[1]].sort((a, b) => a - b);
            const across = end => end.box.node.rackColumn * columnWidth + inset + end.x;
            internal.forEach(cable => {
                cable.right = (across(cable.from) + across(cable.to)) / 2 > columns * columnWidth / 2 !== choice.has(cable.sideKey);
            });
            const laneItems = internal.map(cable => {
                const [low, high] = laneSpan(cable);
                return { cable: cable, from: low, to: high };
            });
            const leftCount = assignTracks(laneItems.filter(item => !item.cable.right));
            const rightCount = assignTracks(laneItems.filter(item => item.cable.right));
            laneItems.forEach(item => {
                item.cable.lane = item.track;
            });
            const laneWidth = leftCount ? leftCount * laneGap + 8 : 0;
            const rightLaneWidth = rightCount ? rightCount * laneGap + 8 : 0;
            const deviceLeft = frame + railWidth + laneWidth;
            const deviceRight = deviceLeft + columns * columnWidth;
            const frameRight = deviceRight + rightLaneWidth + frame;
            const laneX = cable => cable.right ? deviceRight + 8 + cable.lane * laneGap : deviceLeft - 8 - cable.lane * laneGap;

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
                        [from, to] = [laneX(cable), portCentre(run.end).x].sort((a, b) => a - b);
                    }
                    run.from = from;
                    run.to = to;
                    return run;
                });
                // Cables dropping in from the device above take the top of the channel and ones rising from below take the bottom, each with the shortest run nearest its device, so no drop crosses another cable's run.
                // Cables joining two devices across the channel sit in the middle.
                const risesFrom = run => run.kind === 'out' ? run.cable.up : run.kind === 'lane' ? (run.end === run.cable.from ? run.cable.fromUp : run.cable.toUp) : null;
                const above = items.filter(run => risesFrom(run) === false);
                const middle = items.filter(run => risesFrom(run) === null);
                const below = items.filter(run => risesFrom(run) === true);
                const aboveCount = assignTracks(above);
                // Each cable across the channel gets its own track, ordered so that as few drops as possible cut through another cable's run.
                middle.forEach(run => {
                    const ends = [run.cable.from, run.cable.to];
                    run.top = ends.map(end => (end === run.cable.from ? run.cable.fromUp : run.cable.toUp) ? null : portCentre(end).x).find(x => x !== null) ?? run.from;
                    run.bottom = ends.map(end => (end === run.cable.from ? run.cable.fromUp : run.cable.toUp) ? portCentre(end).x : null).find(x => x !== null) ?? run.to;
                });
                const inside = (x, run) => x > run.from && x < run.to;
                const cost = (upper, lower) => (inside(lower.top, upper) ? 1 : 0) + (inside(upper.bottom, lower) ? 1 : 0);
                const ordered = [];
                middle.forEach(run => {
                    let best = 0;
                    let bestCost = Infinity;
                    for (let at = 0; at <= ordered.length; at++) {
                        const total = ordered.slice(0, at).reduce((sum, other) => sum + cost(other, run), 0) + ordered.slice(at).reduce((sum, other) => sum + cost(run, other), 0);
                        if (total < bestCost) {
                            best = at;
                            bestCost = total;
                        }
                    }
                    ordered.splice(best, 0, run);
                });
                ordered.forEach((run, index) => run.track = index);
                const middleCount = ordered.length;
                const belowCount = assignTracks(below);
                const total = aboveCount + middleCount + belowCount;
                above.forEach(run => run.slot = run.track);
                middle.forEach(run => run.slot = aboveCount + run.track);
                below.forEach(run => run.slot = total - 1 - run.track);
                return total;
            });

            // Vertical positions: each channel is as tall as the cables running through it.
            const channelHeight = channelTracks.map(count => channelGap + count * trackGap);
            // A unit grows when a device in it needs more room for its ports, like a 48 port patch panel with its rear ports.
            const unitHeights = Array.from({ length: height + 1 }, () => unitHeight);
            boxes.filter(box => box.need > box.height).forEach(box => {
                unitHeights[topOf(box)] = Math.max(unitHeights[topOf(box)], unitHeight + box.need - box.height);
            });
            const channelTop = [];
            const unitTop = [];
            let y = frame;
            for (let b = height; b >= 0; b--) {
                channelTop[b] = y;
                y += channelHeight[b];
                if (b > 0) {
                    unitTop[b] = y;
                    y += unitHeights[b];
                }
            }
            const frameBottom = y + frame;
            const trackY = (channel, slot) => channelTop[channel] + channelGap / 2 + trackGap / 2 + slot * trackGap;
            const boxTop = box => unitTop[topOf(box)] + inset;
            const boxBottom = box => unitTop[box.node.rackU] + unitHeights[box.node.rackU] - inset;
            boxes.forEach(box => {
                box.x = deviceLeft + box.node.rackColumn * columnWidth + inset;
                box.y = boxTop(box);
                box.drawHeight = boxBottom(box) - box.y;
            });
            // Ports stay on the device's top face even when the device stretches over channels inside its span.
            const portPoint = end => ({ x: end.box.x + end.x + portSize / 2, y: end.box.y + end.y + portSize / 2 });
            const exitPoint = (end, up) => ({ x: portPoint(end).x, y: up ? end.box.y + end.y : end.box.y + end.y + portSize });
            // The path of every cable, start to finish.
            cables.forEach(cable => {
                if (cable.jumper) {
                    const box = cable.from.box;
                    const trackAt = box.y + box.jumperY(cable.from.row, cable.jumperTrack);
                    const edgeOf = end => ({ x: box.x + end.x + portSize / 2, y: box.y + end.y + (end.row % 2 === 0 ? portSize : 0) });
                    const start = edgeOf(cable.from);
                    const finish = edgeOf(cable.to);
                    cable.points = [start, { x: start.x, y: trackAt }, { x: finish.x, y: trackAt }, finish];
                } else if (cable.external) {
                    const trackAt = trackY(cable.channel, cable.run.slot);
                    const exit = exitPoint(cable.inside, cable.up);
                    cable.points = [exit, { x: exit.x, y: trackAt }, { x: frameRight + labelGap - 6, y: trackAt }];
                } else if (cable.direct) {
                    const trackAt = trackY(cable.channel, cable.run.slot);
                    const fromExit = exitPoint(cable.from, cable.fromUp);
                    const toExit = exitPoint(cable.to, cable.toUp);
                    cable.points = [fromExit, { x: fromExit.x, y: trackAt }, { x: toExit.x, y: trackAt }, toExit];
                } else {
                    const fromY = trackY(cable.channels[0], cable.runs[0].slot);
                    const toY = trackY(cable.channels[1], cable.runs[1].slot);
                    const fromExit = exitPoint(cable.from, cable.fromUp);
                    const toExit = exitPoint(cable.to, cable.toUp);
                    const x = laneX(cable);
                    cable.points = [fromExit, { x: fromExit.x, y: fromY }, { x: x, y: fromY }, { x: x, y: toY }, { x: toExit.x, y: toY }, toExit];
                }
            });
            return { deviceLeft: deviceLeft, deviceRight: deviceRight, frameRight: frameRight, frameBottom: frameBottom, laneWidth: laneWidth, rightLaneWidth: rightLaneWidth, channelTop: channelTop, channelHeight: channelHeight, unitTop: unitTop, unitHeights: unitHeights };
        };

        // Counts how messy a layout is: cables running along each other count heavily, cables crossing count once, and every cable's length counts a little so cables don't take the long way round to dodge a crossing.
        const score = layout => {
            const segments = cables.flatMap(cable => {
                const points = tidy(cable.points);
                return points.slice(1).map((point, index) => ({ cable: cable, a: points[index], b: point }));
            });
            const horizontal = segments.filter(segment => segment.a.y === segment.b.y).map(segment => ({ cable: segment.cable, y: segment.a.y, low: Math.min(segment.a.x, segment.b.x), high: Math.max(segment.a.x, segment.b.x) }));
            const vertical = segments.filter(segment => segment.a.x === segment.b.x && segment.a.y !== segment.b.y).map(segment => ({ cable: segment.cable, x: segment.a.x, low: Math.min(segment.a.y, segment.b.y), high: Math.max(segment.a.y, segment.b.y) }));
            let total = layout.frameBottom / 1000 + segments.reduce((sum, segment) => sum + Math.abs(segment.a.x - segment.b.x) + Math.abs(segment.a.y - segment.b.y), 0) / 300;
            const alongside = (list, at) => {
                for (let i = 0; i < list.length; i++) {
                    for (let j = i + 1; j < list.length; j++) {
                        if (list[i].cable !== list[j].cable && Math.abs(list[i][at] - list[j][at]) < 1 && Math.min(list[i].high, list[j].high) - Math.max(list[i].low, list[j].low) > 1) {
                            total += 10;
                        }
                    }
                }
            };
            alongside(horizontal, 'y');
            alongside(vertical, 'x');
            horizontal.forEach(across => vertical.forEach(down => {
                if (across.cable !== down.cable && down.x > across.low + 0.5 && down.x < across.high - 0.5 && across.y > down.low + 0.5 && across.y < down.high - 0.5) {
                    total += 1;
                }
            }));
            return total;
        };

        // Tries each choice in turn and keeps any that makes the layout tidier, until nothing more helps or time runs out.
        const forced = end => end.box.node.patch || end.box.rows === 2;
        const candidates = [
            ...ends.filter(end => end.box.node.patch && !end.socket.rear && ends.some(other => other.box === end.box && other.socket.index === end.socket.index && other.cables.length)).map(end => end.socket.id),
            ...cables.filter(cable => !cable.jumper && (cable.external ? !forced(cable.inside) : !forced(cable.from) || !forced(cable.to))),
            ...cables.filter(cable => !cable.jumper && !cable.external).map(cable => cable.sideKey)
        ];
        let choice = new Set();
        let best = score(plan(choice));
        const started = performance.now();
        for (let pass = 0; pass < 6 && performance.now() - started < 600; pass++) {
            let improved = false;
            candidates.forEach(candidate => {
                if (performance.now() - started > 600) {
                    return;
                }
                const trial = new Set(choice);
                if (trial.has(candidate)) {
                    trial.delete(candidate);
                } else {
                    trial.add(candidate);
                }
                const trialScore = score(plan(trial));
                if (trialScore < best - 0.0001) {
                    choice = trial;
                    best = trialScore;
                    improved = true;
                }
            });
            if (!improved) {
                break;
            }
        }
        const { deviceLeft, deviceRight, frameRight, frameBottom, laneWidth, rightLaneWidth, channelTop, channelHeight, unitTop, unitHeights } = plan(choice);


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
        if (rightLaneWidth) {
            create('rect', { x: deviceRight + 4, y: frame, width: rightLaneWidth - 4, height: frameBottom - frame * 2, class: 'rackmap-manager' }, svg);
        }
        // A divider through the middle of each gap between units, drawn first so the devices and cables sit on top of it.
        for (let b = 0; b <= height; b++) {
            const middle = channelTop[b] + channelHeight[b] / 2;
            create('line', { x1: frame, y1: middle, x2: frameRight - frame, y2: middle, class: 'rackmap-divider' }, svg);
        }
        for (let unit = 1; unit <= height; unit++) {
            create('rect', { x: deviceLeft, y: unitTop[unit], width: deviceRight - deviceLeft, height: unitHeights[unit], class: 'rackmap-unit' }, svg);
            create('text', { x: frame + railWidth - 8, y: unitTop[unit] + unitHeights[unit] / 2 + 4, class: 'rackmap-u' }, svg).textContent = unit;
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
        // Selecting a cable that runs through a patch panel lights up its whole route, from device to device.
        const focus = (picked, picks, text) => {
            clear();
            svg.classList.add('focusing');
            const routes = new Set(picked.map(cable => cable.edge.route).filter(route => route));
            const cableList = [...new Set([...picked, ...cables.filter(cable => routes.has(cable.edge.route))])];
            const deviceIds = [...picks, ...cableList.flatMap(cable => [cable.from, cable.to].filter(end => end).map(end => end.box.node.id))];
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
            const panelHeight = unitTop[panel.rackU] + unitHeights[panel.rackU] - inset - top;
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
                const square = create('g', { class: 'rackmap-port' + (socket.rear ? ' rear' : ''), transform: `translate(${end.x}, ${end.y})` }, group);
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
            // The splitter goes at the end of the cable nearest the device the link feeds.
            const splitterNode = cable.edge.splitterAt ?? cable.edge.target;
            const startNode = cable.external ? cable.inside.box.node.id : cable.from.box.node.id;
            const endNode = cable.external ? (cable.inside === cable.from ? cable.edge.target : cable.edge.source) : cable.to.box.node.id;
            drawLine(cable.edge, clean, 'rackmap-cable-line', group, endNode === splitterNode ? true : startNode === splitterNode ? false : null);
            group.addEventListener('click', event => {
                event.stopPropagation();
                focus([cable], endsOf(cable), describe(cable));
            });
            return group;
        };

        cables.forEach(cable => {
            const group = drawCable(cable, cable.points);
            if (cable.external) {
                const last = cable.points[cable.points.length - 1];
                create('circle', { cx: last.x, cy: last.y, r: 3, fill: cableColour(cable.edge) }, group);
                const label = create('text', { x: last.x + 6, y: last.y + 4, class: 'rackmap-out-label', 'data-edge': cable.edge.id }, labelLayer);
                label.textContent = cable.text;
                label.addEventListener('click', event => {
                    event.stopPropagation();
                    focus([cable], endsOf(cable), describe(cable));
                });
            }
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
