using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace Janphe.Fantasy.Map
{
    internal class Map5BiomesSystem
    {
        private Grid grid { get; set; }
        private Grid pack { get; set; }

        private Biomes biomesData { get; set; }

        public Map5BiomesSystem(MapJobs map)
        {
            grid = map.grid;
            pack = map.pack;
            biomesData = map.biomesData;
        }

        public static Biomes applyDefaultBiomesSystem()
        {
            var name = new string[] { "Marine", "Hot desert", "Cold desert", "Savanna", "Grassland", "Tropical seasonal forest", "Temperate deciduous forest", "Tropical rainforest", "Temperate rainforest", "Taiga", "Tundra", "Glacier", "Wetland" };
            var color = new string[] { "#53679f", "#fbe79f", "#b5b887", "#d2d082", "#c8d68f", "#b6d95d", "#29bc56", "#7dcb35", "#409c43", "#4b6b32", "#96784b", "#d5e7eb", "#0b9131" };
            var habitability = new byte[] { 0, 4, 10, 22, 30, 50, 100, 80, 90, 12, 4, 0, 12 };
            var iconsDensity = new byte[] { 0, 3, 2, 120, 120, 120, 120, 150, 150, 100, 5, 0, 150 };
            var icons = JArray.Parse(@"[{},{dune:3, cactus:6, deadTree:1},{dune:9, deadTree:1},{acacia:1, grass:9},{grass:1},{acacia:8, palm:1},{deciduous:1},{acacia:5, palm:3, deciduous:1, swamp:1},{deciduous:6, swamp:1},{conifer:1},{grass:1},{},{swamp:1}]");
            var cost = new byte[] { 10, 200, 150, 60, 50, 70, 70, 80, 90, 80, 100, 255, 150 }; // biome movement cost

            byte[][] biomesMartix = {
                // hot ↔ cold; dry ↕ wet
                new byte[]{1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2   },
                new byte[]{3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 9, 9, 9, 9, 9, 10, 10 },
                new byte[]{5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 9, 9, 9, 9, 9, 10, 10, 10},
                new byte[]{5, 6, 6, 6, 6, 6, 6, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10},
                new byte[]{7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10 }
            };

            var biomeIcons = new string[icons.Count][];
            icons.forEach((d, i) =>
            {
                var parsed = new List<string>();
                foreach (var kv in d.Value<JObject>())
                {
                    for (var j = 0; j < (int)kv.Value; ++j)
                    { parsed.Add(kv.Key); }
                }
                biomeIcons[i] = parsed.ToArray();
            });

            var biome = new Biomes()
            {
                i = D3.range(name.Length),
                name = name,
                color = color,
                biomesMartix = biomesMartix,
                habitability = habitability,
                iconsDensity = iconsDensity,
                icons = biomeIcons,
                cost = cost,
            };
            return biome;
        }

        public void defineBiomes()
        {
            var cells = pack.cells;
            var f = pack.features;

            cells.biome = new byte[cells.i.Length]; // biomes array
            foreach (var i in cells.i)
            {
                if (f[cells.f[i]].group == "freshwater")
                    cells.r_height[i] = 19; // de-elevate lakes
                if (cells.r_height[i] < 20)
                    continue; // water cells have biome 0
                double moist = grid.cells.prec[cells.g[i]];
                if (cells.r[i] != 0)
                    moist += Math.Max(cells.fl[i] / 20.0, 2);
                var n = cells.r_neighbor_r[i].Where(pack.isLand).Select(c => (double)grid.cells.prec[cells.g[c]]).ToList();
                n.Add(moist);
                moist = Utils.rn(4 + n.Average());
                var temp = grid.cells.temp[cells.g[i]]; // flux from precipitation
                cells.biome[i] = (byte)biomesData.getBiomeId(moist, temp, cells.r_height[i]);
            }
        }

        // 评估小格子的宜居性
        // assess cells suitability to calculate population and rand cells for culture center and burgs placement
        public void rankCells()
        {
            var cells = pack.cells;
            var f = pack.features;
            var n = cells.i.Length;
            cells.s = new short[n]; // cell suitability array
            cells.pop = new float[n]; // cell population array

            // to normalize flux
            double flMean = D3.median(cells.fl.Where(_f => _f != 0)), flMax = cells.fl.Max() + cells.conf.Max();
            // to adjust population by cell area
            double areaMean = D3.mean(cells.area);

            //var ss = new List<string>();
            foreach (var i in cells.i)
            {
                double s = biomesData.habitability[cells.biome[i]]; // base suitability derived from biome habitability
                if (s == 0)
                    continue;
                if (flMean != 0)
                    s += Utils.normalize(cells.fl[i] + cells.conf[i], flMean, flMax) * 250; // big rivers and confluences are valued
                s -= (cells.r_height[i] - 50) / 5.0; // low elevation is valued, high is not;

                if (cells.t[i] == 1)
                {
                    if (cells.r[i] != 0)
                        s += 15; // estuary is valued
                    var type = f[cells.f[cells.haven[i]]].type;
                    var group = f[cells.f[cells.haven[i]]].group;
                    if (type == "lake")
                    {
                        // lake coast is valued
                        if (group == "freshwater")
                            s += 30;
                        else if (group != "lava")
                            s += 10;
                    }
                    else
                    {
                        s += 5; // ocean coast is valued
                        if (cells.harbor[i] == 1)
                            s += 20; // safe sea harbor is valued
                    }
                }

                cells.s[i] = (short)(s / 5); // general population rate
                                             // cell rural population is suitability adjusted by cell area
                cells.pop[i] = cells.s[i] > 0 ? (float)(cells.s[i] * cells.area[i] / areaMean) : 0;

                //ss.Add($"{i} {cells.s[i]}");
            }
            //Debug.SaveArray("Map5BiomesSystem.rankCells.ss.txt", ss);
        }

        public List<SKPoint[]>[] Paths { get; private set; }
        public void generate()
        {
            var cells = pack.cells;
            var n = cells.i.Length;
            var vertices = pack.vertices;
            var used = new BitArray(n);

            var paths = new List<SKPoint[]>[biomesData.i.Length];

            cells.i.forEach(i =>
            {
                if (0 == cells.biome[i])
                    return; // no need to mark water
                if (used[i])
                    return; // already marked
                var b = cells.biome[i];
                var onborder = cells.c[i].some(ci => cells.biome[ci] != b);
                if (!onborder)
                    return;
                var edgeVerticle = cells.v[i].find(v => vertices.c[v].some(ci => cells.biome[ci] != b));
                var chain = connectVertices(edgeVerticle, b);
                if (chain.Count < 3)
                    return;
                var points = chain.map(v => vertices.p[v]).map(p => new SKPoint((float)p[0], (float)p[1]));

                if (paths[b] == null)
                    paths[b] = new List<SKPoint[]>();
                paths[b].Add(points.ToArray());
            });

            Paths = paths;

            // connect vertices to chain
            List<int> connectVertices(int start, byte b)
            {
                var chain = new List<int>();// vertices chain to form a path
                for (int i = 0, current = start; i == 0 || current != start && i < 20000; i++)
                {
                    var prev = chain.Count > 0 ? chain.Last() : -1;
                    chain.push(current); // add current vertex to sequence
                    var cc = vertices.c[current]; // cells adjacent to vertex
                    cc.filter(c => cells.biome[c] == b).forEach(c => used[c] = true);
                    var c0 = cc[0] >= n || cells.biome[cc[0]] != b;
                    var c1 = cc[1] >= n || cells.biome[cc[1]] != b;
                    var c2 = cc[2] >= n || cells.biome[cc[2]] != b;
                    var v = vertices.v[current]; // neighboring vertices
                    if (v[0] != prev && c0 != c1)
                        current = v[0];
                    else if (v[1] != prev && c1 != c2)
                        current = v[1];
                    else if (v[2] != prev && c0 != c2)
                        current = v[2];
                    if (current == chain.Last())
                    { Debug.LogError("Next vertex is not found"); break; }
                }
                return chain;
            }
        }
    }
}
