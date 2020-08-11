using System;
using System.Collections.Generic;

namespace Janphe.Fantasy.Map
{
    internal class Map1Features
    {
        private int seed { get; set; }
        private Grid grid { get; set; }
        private Grid.Cells cells { get; set; }

        private string templateInput { get; set; }

        public Map1Features(MapJobs map)
        {
            seed = map.Options.MapSeed;
            grid = map.grid;
            cells = grid.cells;

            templateInput = map.Options.MapTemplate;
        }

        // Mark features (ocean, lakes, islands)
        public void markFeatures()
        {
            Random.Seed(seed);

            var heights = cells.r_height;
            var f = new ushort[cells.i.Length];
            var t = new sbyte[cells.i.Length];

            var features = new List<Grid.Feature>();
            features.Add(null);

            var queue = new List<int>();
            queue.Add(0);

            //var msg = new List<string>();
            for (int i = 1; queue[0] != -1; ++i)
            {
                f[queue[0]] = (ushort)i;// feature number
                var land = heights[queue[0]] >= 20;
                var border = false;// true if feature touches map border

                while (queue.Count > 0)
                {
                    var q = queue[queue.Count - 1];
                    queue.RemoveAt(queue.Count - 1);

                    if (cells.r_near_border[q]) border = true;

                    foreach (var e in cells.r_neighbor_r[q])
                    {
                        var eLand = heights[e] >= 20;
                        //if (eLand) cells.t[e] = 2;
                        if (land == eLand && f[e] == 0)
                        {
                            f[e] = (ushort)i;
                            queue.Add(e);
                        }
                        if (land && !eLand)
                        {
                            t[q] = 1;
                            t[e] = -1;
                        }
                    }
                }
                var type = land ? "island" : border ? "ocean" : "lake";
                features.Add(new Grid.Feature()
                {
                    i = i,
                    land = land,
                    border = border,
                    type = type
                });

                queue.Add(Array.FindIndex(f, a => a == 0));// find unmarked cell
                //msg.Add($"i:{i} land:{land} border:{border} type:{type} queue:{queue[0]}");
            }
            //DebugHelper.SaveArray("markFeatures.txt", msg);

            cells.f = f;
            cells.t = t;
            grid.features = features.ToArray();
        }

        // How to handle lakes generated near seas? They can be both open or closed.
        // As these lakes are usually get a lot of water inflow, most of them should have brake the treshold and flow to sea via river or strait (see Ancylus Lake).
        // So I will help this process and open these kind of lakes setting a treshold cell heigh below the sea level (=19).
        public void openNearSeaLakes()
        {
            if (templateInput == "Atoll") return; // no need for Atolls

            var features = grid.features;
            if (Array.Find(features, f => f != null && f.type == "lake") == null) return; // no lakes

            var limit = 50; // max height that can be breached by water
            var removed = true;
            for (var t = 0; t < 5 && removed; t++)
            {
                removed = false;
                foreach (var i in cells.i)
                {
                    var lake = cells.f[i];
                    if (features[lake].type != "lake") continue; // not a lake cell

                    foreach (var c in cells.r_neighbor_r[i])
                    {
                        if (cells.t[c] != 1 || cells.r_height[c] > limit) continue; // water cannot brake this

                        var check_neighbours = false;
                        foreach (var n in cells.r_neighbor_r[c])
                        {
                            var ocean = cells.f[n];
                            if (features[ocean].type != "ocean") continue; // not an ocean
                            removed = removeLake(c, lake, ocean);
                            check_neighbours = true;
                            break;
                        }
                        if (check_neighbours) break;
                    }
                }
            }

            bool removeLake(int treshold, int lake, int ocean)
            {
                cells.r_height[treshold] = 19;
                cells.t[treshold] = -1;
                cells.f[treshold] = (ushort)ocean;
                foreach (var c in cells.r_neighbor_r[treshold])
                {
                    if (cells.r_height[c] >= 20) cells.t[c] = 1; // mark as coastline
                }
                features[lake].type = "ocean"; // mark former lake as ocean
                return true;
            }
        }

    }
}
