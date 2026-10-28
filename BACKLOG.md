# HoLaMap Backlog

## Next Up

### Persistent Sync and Action Items
- Sync records links instead of deleting and rebuilding them; `Link` gains `LastSeen`.
- Same device on the same port: update `LastSeen` only.
- New device on a free port: add the link automatically.
- Known device seen on a different, free port: move it automatically.
- Device on a port that already has a different assignment: create an action item instead of changing anything.
- Ports can be marked `Shared` (e.g. the multicast relay on Tyrion's port, unmanaged switches); new devices on a shared port are added alongside automatically.
- Switch-to-switch uplinks from UniFi are trusted and always update automatically.
- Manual links count as assignments, so the sync can't overwrite them.
- Nothing is deleted automatically; devices that disappear keep their link, drawn faded with their LastSeen time.
- New Actions page with Replace, Add Alongside (optionally mark port shared) and Dismiss.
- Nav link shows a count badge when there are action items.

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
