# HoLaMap Backlog

## Done
- Persistent sync and action items (links recorded with LastSeen, auto-assign to free or shared ports, auto-move, trusted uplinks, Actions page with badge, offline devices faded on the map).
- Floors and rooms: floor plan page (drag/resize rooms on a grid, devices shown inside), Assign Devices page. Room assignment is never touched by the sync.
- Merge devices (extra MACs kept in `DeviceMac` so syncs still match, links move to the kept device).
- Racks: rack in a room, 4-column grid of U rows, Rack Details page with tabbed editor and cable map, rack popup on the map.
- Blank, brush and patch panels (12/24/48 ports, front and rear sockets, jumpers between ports, routes drawn through the panel).
- PoE splitters as a property on links, drawn inline with PoE before and plain Ethernet after.

## Next Up
- Manual devices (unmanaged switch), then wireless links; both need links that start from a device.

## Later
- Wireless links from APs to Wi-Fi clients (needs links that start from a device rather than a port).
- Separate colours for 2.5 and 10 Gbps.
- More layers: networks/VLANs, firewall zones and policies, services, public exposure, dependencies.
