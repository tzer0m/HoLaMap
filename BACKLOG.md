# HoLaMap Backlog

## Done
- Persistent sync and action items (links recorded with LastSeen, auto-assign to free or shared ports, auto-move, trusted uplinks, Actions page with badge, offline devices faded on the map).

## Next Up

### Merge Devices
- Merge two or more devices into one, e.g. the ONT appearing with two MAC addresses on Living Room Switch port 5.
- Merged device keeps all its MACs so future syncs match any of them (MACs move from `Device` to their own table or a list).
- Links from the merged devices move to the surviving device.

## Later
- Backfill offline devices from UniFi's client history (`v2/api/site/default/clients/history`), if it returns last uplink switch and port.
- Wireless links from APs to Wi-Fi clients (needs links that start from a device rather than a port).
- Manual devices for things UniFi can't see (patch panel, PoE splitters, unmanaged switches).
- Rooms and rack positions.
- Separate colours for 2.5 and 10 Gbps.
- More layers: networks/VLANs, firewall zones and policies, services, public exposure, dependencies.
