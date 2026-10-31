using HoLaMap.Models;

namespace HoLaMap.Zigbee
{
    /// <summary>
    /// Works out a tidy tree from the Zigbee mesh: routers joined back to the coordinator along their strongest links, and each end device under its parent.
    /// </summary>
    public static class ZigbeeMesh
    {
        /// <summary>
        /// Finds each device's upstream device in the tree.
        /// </summary>
        /// <param name="devices">The devices with their neighbour tables.</param>
        /// <returns>For each device that could be placed, its parent and the link quality between them.</returns>
        public static Dictionary<int, (ZigbeeDevice Parent, int? Lqi)> Parents(List<ZigbeeDevice> devices)
        {
            Dictionary<string, ZigbeeDevice> byIeee = devices.GroupBy(device => device.Ieee).ToDictionary(group => group.Key, group => group.First());
            bool IsRouter(ZigbeeDevice device) => device.Role is "Coordinator" or "Router";

            // Every link either end reported, with the best quality either side saw.
            Dictionary<(int, int), int> quality = [];
            foreach (ZigbeeDevice device in devices)
            {
                foreach (ZigbeeNeighbor neighbor in device.Neighbors.Where(item => byIeee.ContainsKey(item.NeighborIeee)))
                {
                    ZigbeeDevice other = byIeee[neighbor.NeighborIeee];
                    (int, int) key = device.Id < other.Id ? (device.Id, other.Id) : (other.Id, device.Id);
                    quality[key] = Math.Max(quality.GetValueOrDefault(key), neighbor.Lqi ?? 0);
                }
            }
            int? Quality(ZigbeeDevice a, ZigbeeDevice b) => quality.TryGetValue(a.Id < b.Id ? (a.Id, b.Id) : (b.Id, a.Id), out int value) ? value : null;

            Dictionary<int, (ZigbeeDevice Parent, int? Lqi)> parents = [];
            ZigbeeDevice? coordinator = devices.FirstOrDefault(device => device.Role == "Coordinator");
            if (coordinator == null)
            {
                return parents;
            }

            // Routers: grow out from the coordinator, always taking the strongest link to a router not yet reached.
            HashSet<int> reached = [coordinator.Id];
            List<ZigbeeDevice> routers = [.. devices.Where(device => device.Role == "Router")];
            while (true)
            {
                (ZigbeeDevice From, ZigbeeDevice To, int Lqi)? best = null;
                foreach (ZigbeeDevice router in routers.Where(item => !reached.Contains(item.Id)))
                {
                    foreach (ZigbeeDevice from in devices.Where(item => reached.Contains(item.Id) && IsRouter(item)))
                    {
                        int? lqi = Quality(from, router);
                        if (lqi != null && (best == null || lqi > best.Value.Lqi))
                        {
                            best = (from, router, lqi.Value);
                        }
                    }
                }
                if (best == null)
                {
                    break;
                }
                parents[best.Value.To.Id] = (best.Value.From, best.Value.Lqi);
                reached.Add(best.Value.To.Id);
            }

            // End devices: the parent they or their router name, otherwise their strongest router link.
            foreach (ZigbeeDevice device in devices.Where(item => !IsRouter(item)))
            {
                ZigbeeNeighbor? named = device.Neighbors.FirstOrDefault(item => item.Relationship == "Parent" && byIeee.ContainsKey(item.NeighborIeee));
                ZigbeeDevice? parent = named != null ? byIeee[named.NeighborIeee] : devices.FirstOrDefault(item => IsRouter(item) && item.Neighbors.Any(neighbor => neighbor.NeighborIeee == device.Ieee && neighbor.Relationship == "Child"));
                parent ??= devices.Where(item => IsRouter(item) && Quality(item, device) != null).OrderByDescending(item => Quality(item, device)).FirstOrDefault();
                if (parent != null)
                {
                    parents[device.Id] = (parent, Quality(parent, device) ?? device.Lqi);
                }
            }
            return parents;
        }
    }
}