using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe.Fantasy.Map
{
    internal class Map6Routes
    {
        private MapJobs map { get; set; }

        public Map6Routes(MapJobs map)
        { this.map = map; }

        public List<int[]> getRoads()
        {
            var pack = map.pack;
            var cells = pack.cells;
            var burgs = pack.burgs.Where(b => b.i > 0 && !b.removed);
            var capitals = burgs.Where(b => b.capital > 0).ToArray();
            if (capitals.Length < 2)
                return null;

            var paths = new List<int[]>();
            foreach (var b in capitals)
            {
                var connect = capitals.Where(c => c.i > b.i && c.feature == b.feature).ToArray();
                if (0 == connect.Length)
                    continue;
                var farthest = D3.scan(connect, (a, c) => ((c.y - b.y).pow(2) + (c.x - b.x).pow(2)).CompareTo((a.y - b.y).pow(2) + (a.x - b.x).pow(2)));
                var ret = findLandPath(b.cell, connect[farthest].cell);
                var from = ret.Item1;
                var exit = ret.Item2;
                var segments = restorePath(b.cell, exit, "main", from);
                //foreach (var s in segments) paths.Add(s);
                paths.AddRange(segments);
            }

            foreach (var i in cells.i)
                cells.s[i] += (short)(cells.road[i] / 2); // add roads to suitability score
            return paths;
        }
        public List<int[]> getTrails()
        {
            var pack = map.pack;
            var cells = pack.cells;
            var burgs = pack.burgs.Where(b => b != null && b.i > 0 && !b.removed).ToArray();
            if (burgs.Length < 2)
                return null;

            var paths = new List<int[]>();
            var features = pack.features.Where(f => f != null && f.land);
            foreach (var f in features)
            {
                var isle = burgs.Where(b => b.feature == f.i).ToArray(); // burgs on island
                if (isle.Length < 2)
                    continue;

                for (var i = 0; i < isle.Length; ++i)
                {
                    var b = isle[i];
                    List<int[]> path = null;
                    if (0 == i)
                    {
                        // build trail from the first burg on island to the farthest one on the same island
                        var farthest = D3.scan(isle, (a, c) => ((c.y - b.y).pow(2) + (c.x - b.x).pow(2)).CompareTo((a.y - b.y).pow(2) + (a.x - b.x).pow(2)));
                        var to = isle[farthest].cell;
                        if (cells.road[to] > 0)
                            continue;
                        var ret = findLandPath(b.cell, to);
                        var from = ret.Item1;
                        var exit = ret.Item2;
                        path = restorePath(b.cell, exit, "small", from);
                    }
                    else
                    {
                        // build trail from all other burgs to the closest road on the same island
                        if (cells.road[b.cell] > 0)
                            continue;
                        var ret = findLandPath(b.cell, -1, true);
                        var from = ret.Item1;
                        var exit = ret.Item2;
                        if (exit == -1)
                            continue;
                        path = restorePath(b.cell, exit, "small", from);
                    }
                    if (path != null)
                        paths.AddRange(path);
                }
            }
            return paths;
        }
        public List<int[]> getSearoutes()
        {
            var pack = map.pack;
            var cells = pack.cells;
            var allPorts = pack.burgs.Where(b => null != b && b.port > 0 && !b.removed).ToArray();
            if (allPorts.Length < 2)
                return null;

            var bodies = new HashSet<ushort>(allPorts.Select(b => b.port)); // features with ports
            var paths = new List<int[]>();

            //var msg = new List<string>();

            foreach (var f in bodies)
            {

                var ports = allPorts.Where(b => b.port == f).ToArray();
                if (ports.Length < 2)
                    continue;
                var first = ports[0].cell;

                // directly connect first port with the farthest one on the same island to remove gap
                if (pack.features[f].type != "lake")
                {
                    var portsOnIsland = ports.Where(b => cells.f[b.cell] == cells.f[first]).ToArray();
                    if (portsOnIsland.Length > 3)
                    {
                        var idx = D3.scan(portsOnIsland, (a, b) => ((b.y - ports[0].y).pow(2) + (b.x - ports[0].x).pow(2)).CompareTo((a.y - ports[0].y).pow(2) + (a.x - ports[0].x).pow(2)));
                        var opposite = ports[idx].cell;
                        var ret = findOceanPath(opposite, first);
                        var from = ret.Item1;
                        var exit = ret.Item2;

                        from[first] = cells.haven[first];
                        var path = restorePath(opposite, first, "ocean", from);
                        paths.AddRange(path);
                    }
                }

                var farthest = ports[D3.scan(ports, (a, b) => ((b.y - ports[0].y).pow(2) + (b.x - ports[0].x).pow(2)).CompareTo((a.y - ports[0].y).pow(2) + (a.x - ports[0].x).pow(2)))].cell;
                //msg.push($"bodies {f} {first} {farthest}");

                // directly connect first port with the farthest one
                {
                    var ret = findOceanPath(farthest, first);
                    var from = ret.Item1;
                    var exit = ret.Item2;

                    from[first] = cells.haven[first];
                    var path = restorePath(farthest, first, "ocean", from);
                    paths.AddRange(path);
                }
                // indirectly connect first port with all other ports
                if (ports.Length < 3)
                    continue;
                foreach (var p in ports)
                {
                    if (p.cell == first || p.cell == farthest)
                        continue;
                    var ret = findOceanPath(p.cell, first, true);
                    var from = ret.Item1;
                    var exit = ret.Item2;

                    //from[exit] = cells.haven[exit];
                    var path = restorePath(p.cell, exit, "ocean", from);
                    paths.AddRange(path);

                    //msg.push($"findOceanPath {p.cell} { first} {exit}");
                    //msg.push($"{path.map(pp => pp.join()).join(" ")}");
                }
            }
            //Debug.SaveArray("getSearoutes.txt", msg);

            return paths;
        }

        class Item { public int e; public double p; }
        // Find a land path to a specific cell (exit), to a closest road (toRoad), or to all reachable cells (null, null)
        private Tuple<Dictionary<int, int>, int> findLandPath(int start, int exit = -1, bool toRoad = false)
        {
            var pack = map.pack;
            var cells = pack.cells;
            var biomesData = map.biomesData;

            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            queue.push(new Item { e = start, p = 0 });

            var cost = new Dictionary<int, double>();
            var from = new Dictionary<int, int>();

            while (queue.Count > 0)
            {
                var next = queue.pop();
                var n = next.e;
                var p = next.p;
                if (toRoad && cells.road[n] > 0)
                { return Tuple.Create(from, n); }

                foreach (var c in cells.r_neighbor_r[n])
                {
                    if (cells.r_height[c] < 20)
                        continue;// ignore water cells
                    var stateChangeCost = cells.state != null && cells.state[c] != cells.state[n] ? 400 : 0; // trails tend to lay within the same state
                    var habitedCost = Math.Max(100 - biomesData.habitability[cells.biome[c]], 0); // routes tend to lay within populated areas
                    var heightChangeCost = Math.Abs(cells.r_height[c] - cells.r_height[n]) * 10; // routes tend to avoid elevation changes
                    var cellCoast = 10 + stateChangeCost + habitedCost + heightChangeCost;
                    var tmp = (decimal)p + (cells.road[c] > 0 || cells.burg[c] > 0 ? cellCoast / 3.0m : cellCoast);
                    var totalCost = Utils.rn((double)tmp, 6);

                    if ((from.ContainsKey(c) && 0 != from[c]) || (cost.ContainsKey(c) && totalCost >= cost[c]))
                        continue;
                    from[c] = n;
                    if (c == exit)
                    { return Tuple.Create(from, exit); }

                    cost[c] = totalCost;
                    queue.push(new Item { e = c, p = totalCost });
                }
            }

            return Tuple.Create(from, exit);
        }
        // find water paths
        private Tuple<Dictionary<int, int>, int> findOceanPath(int start, int exit = -1, bool toRoute = false, List<string> mzg = null)
        {
            var pack = map.pack;
            var cells = pack.cells;

            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            queue.push(new Item { e = start, p = 0 });

            var cost = new Dictionary<int, double>();
            var from = new Dictionary<int, int>();

            //var msg = new List<string>();
            while (queue.Count > 0)
            {
                var next = queue.pop();
                var n = next.e;
                var p = next.p;
                if (toRoute && n != start && cells.road[n] > 0)
                { return Tuple.Create(from, n); }

                //msg.push($"queue.pop {n} {p} {queue.Count}");
                foreach (var c in cells.r_neighbor_r[n])
                {
                    if (cells.r_height[c] >= 20)
                        continue; // ignore land cells
                    var dist2 = (cells.r_points[c][1] - cells.r_points[n][1]).pow(2) + (cells.r_points[c][0] - cells.r_points[n][0]).pow(2);
                    var totalCost = p + (cells.road[c] > 0 ? 1 + dist2 / 2.0 : dist2 + (0 != cells.t[c] ? 1 : 100));

                    if ((from.ContainsKey(c) && 0 != from[c]) || (cost.ContainsKey(c) && totalCost >= cost[c]))
                        continue;
                    from[c] = n;
                    if (c == exit)
                    { return Tuple.Create(from, exit); }

                    cost[c] = totalCost;
                    queue.push(new Item { e = c, p = totalCost });
                    //msg.push($"queue.push {c} {totalCost} {dist2}");
                }
            }
            //if (mzg != null)
                //mzg.AddRange(msg);

            return Tuple.Create(from, exit);
        }
        private List<int[]> restorePath(int start, int end, string type, Dictionary<int, int> from)
        {
            var pack = map.pack;
            var cells = pack.cells;
            var path = new List<int[]>(); // to store all segments;
            var segment = new List<int>();
            int current = end, prev = end;

            var score = type == "main" ? 5 : 1; // to incrade road score at cell

            if (type == "ocean" || 0 == cells.road[prev])
                segment.Add(end);
            if (0 == cells.road[prev])
                cells.road[prev] = (ushort)score;

            for (int i = 0, limit = 1000; i < limit; i++)
            {
                if (0 == from[current])
                    break;
                current = from[current];

                if (0 != cells.road[current])
                {
                    if (segment.Count > 0)
                    {
                        segment.Add(current);
                        path.Add(segment.ToArray());
                        if (segment[0] != end)
                        {
                            cells.road[segment[0]] += (ushort)score;
                            cells.crossroad[segment[0]] += (ushort)score;
                        }
                        if (current != start)
                        {
                            cells.road[current] += (ushort)score;
                            cells.crossroad[current] += (ushort)score;
                        }
                    }
                    segment.Clear();
                    prev = current;
                }
                else
                {
                    if (0 != prev)
                        segment.Add(prev);
                    prev = 0;
                    segment.Add(current);
                }

                cells.road[current] += (ushort)score;
                if (current == start)
                    break;
            }

            if (segment.Count > 1)
                path.Add(segment.ToArray());
            return path;
        }

    }
}
