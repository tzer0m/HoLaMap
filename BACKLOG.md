# HoLaMap Backlog

## Done
- Persistent sync and action items (links recorded with LastSeen, auto-assign to free or shared ports, auto-move, trusted uplinks, Actions page with badge, offline devices faded on the map).
- Merge devices (extra MACs kept in `DeviceMac` so syncs still match, links move to the kept device).

## Next Up

## Later
- Backfill offline devices from UniFi's client history (`v2/api/site/default/clients/history`), if it returns last uplink switch and port.
- Wireless links from APs to Wi-Fi clients (needs links that start from a device rather than a port).
- Manual devices for things UniFi can't see (patch panel, PoE splitters, unmanaged switches).
- Rooms and rack positions.
- Separate colours for 2.5 and 10 Gbps.
- More layers: networks/VLANs, firewall zones and policies, services, public exposure, dependencies.
