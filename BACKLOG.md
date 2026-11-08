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
- Zigbee view from ZHA over the Home Assistant WebSocket API: devices and neighbour tables, drawn as a tree (routers back to the coordinator along their strongest links, end devices under their parents), coordinator hung off its host by USB, LQI on the links, rooms set by hand, grouped by floor, and shown on the All view.
- Hue Bridge as a second Zigbee network over its local API, every device hanging off the bridge, with All, Zigbee and Hue tabs on the Zigbee view.
- Key that only lists what the current view shows, including the rack view.
- Devices that leave ZHA or the Hue Bridge stay greyed out with their room, and untouched UniFi clients (no room, rack, manual link, patch or merge) are forgotten, both after `ForgetAfterDays` (30 by default).

## Next Up
- More layers: services and dependencies, firewall zones and policies, public exposure.
- Pocket ID sign-in (OIDC in the app) when it's deployed to Tyrion.
- README (what it does, settings, secrets) when it's deployed.