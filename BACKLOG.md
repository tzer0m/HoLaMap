# HoLaMap Backlog

## Done
- Persistent sync and action items (links recorded with LastSeen, auto-assign to free or shared ports, auto-move, trusted uplinks, Actions page with badge, offline devices faded on the map).
- Floors and rooms: floor plan page (drag/resize rooms on a grid, devices shown inside), Assign Devices page. Room assignment is never touched by the sync.
- Merge devices (extra MACs kept in `DeviceMac` so syncs still match, links move to the kept device).
- Racks: rack in a room, 4-column grid of U rows, Rack Details page with tabbed editor and cable map, rack popup on the map.
- Blank, brush and patch panels (12/24/48 ports, front and rear sockets, jumpers between ports, routes drawn through the panel).
- PoE splitters as a property on links, drawn inline with PoE before and plain Ethernet after.
- Wi-Fi links from each client's current AP, shown for clients with a room (SSID, band, channel, signal, rate in the details).
- VLAN filter on the UniFi view (membership by IP subnet, untagged solid, tagged dashed, tagged lists synced from port overrides/profiles).

## Next Up

## Later
- Separate colours for 2.5 and 10 Gbps (not needed while everything is 1 Gbps).
- More layers: services and dependencies, firewall zones and policies, public exposure.
