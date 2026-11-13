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
- Services view with service dependencies: manual links between services, ELK layout with hosts and project boxes as nested containers, categories (Custom, 3rd Party, Cloud, Cloudflare, Semaphore, GitHub, Asterisk, Home, Media, Monitoring, Documents/Data, Other) with a category picker.
- Cloudflare tunnels from the API (synced hourly): Cloudflare view (tunnel to domain to hostname to service), Domains page for the tunnel host and manual service overrides, routes matched to services by the listening ports hosts report.
- All view shows each device's services in its card, with dependency and tunnel lines on click, and rows arranged to fit the box.
- Details pane walks with the selection, switching views when a link points elsewhere (never back to All), and the rack popup reports picks to it.
- Gaps where lines cross, consistent across every view.
- Deployed to Tyrion on port 7170 (LAN and Tailscale, plain HTTP), deploy by GitHub Actions on push to master, `/health` monitored in Uptime Kuma.
- Automated host reports: `holamap-report.yml` in the Ansible repo posts each host's systemd services, Docker containers and listening ports from Semaphore daily at midday (token in `~/.config/holamap-token.json` on Tyrion).

## Next Up
- More layers: firewall zones and policies, Tailscale, GitHub.
- Schedule page (own nav link): everything scheduled across the fleet on three bars. Daily runs noon to noon so overnight jobs sit in the middle, weekly Mon to Sun, monthly 1 to 31. Overlapping jobs stack, block width from run duration where known (fixed minimum otherwise), click for host, command and schedule in words. Own `ScheduledTask` table reported per host like services; sources: TrueNAS cron jobs first (RebelBackup on Davos), then Semaphore schedules, TrueNAS Cloud Syncs and systemd timers.
- README (what it does, settings, secrets).
- Install node_exporter on Jorah, the only host reporting services without one (spotted on the Services view).