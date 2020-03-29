using System;
using System.Collections.Generic;
using System.Linq;

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

            Dictionary<string, int>[] icons = {
             new Dictionary<string, int>{},
             new Dictionary<string, int>{{ "dune",     3},{ "cactus",   6},{ "deadTree", 1}},
             new Dictionary<string, int>{{ "dune",     9},{ "deadTree", 1}},
             new Dictionary<string, int>{{ "acacia",   1},{ "grass",    9}},
             new Dictionary<string, int>{{ "grass",    1}},
             new Dictionary<string, int>{{ "acacia",   8},{ "palm",     1}},
             new Dictionary<string, int>{{ "deciduous",1}},
             new Dictionary<string, int>{{ "acacia",   5},{ "palm",     3},{ "deciduous", 1},{ "swamp", 1}},
             new Dictionary<string, int>{{ "deciduous",6},{ "swamp",    1}},
             new Dictionary<string, int>{{ "conifer",  1}},
             new Dictionary<string, int>{{ "grass",    1}},
             new Dictionary<string, int>{},
             new Dictionary<string, int>{{ "swamp",    1}},
            };

            var cost = new byte[] { 10, 200, 150, 60, 50, 70, 70, 80, 90, 80, 100, 255, 150 }; // biome movement cost

            byte[][] biomesMartix = {
                // hot ↔ cold; dry ↕ wet
                new byte[]{1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2   },
                new byte[]{3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 9, 9, 9, 9, 9, 10, 10 },
                new byte[]{5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 9, 9, 9, 9, 9, 10, 10, 10},
                new byte[]{5, 6, 6, 6, 6, 6, 6, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10},
                new byte[]{7, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 9, 9, 9, 9, 9, 9, 10, 10, 10 }
            };

            var biome = new Biomes()
            {
                i = D3.range(name.Length),
                name = name,
                color = color,
                biomesMartix = biomesMartix,
                habitability = habitability,
                iconsDensity = iconsDensity,
                icons = icons,
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
                if (f[cells.f[i]].group == "freshwater") cells.r_height[i] = 19; // de-elevate lakes
                if (cells.r_height[i] < 20) continue; // water cells have biome 0
                double moist = grid.cells.prec[cells.g[i]];
                if (cells.r[i] != 0) moist += Math.Max(cells.fl[i] / 20.0, 2);
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

            foreach (var i in cells.i)
            {
                double s = biomesData.habitability[cells.biome[i]]; // base suitability derived from biome habitability
                if (s == 0) continue;
                if (flMean != 0) s += Utils.normalize(cells.fl[i] + cells.conf[i], flMean, flMax) * 250; // big rivers and confluences are valued
                s -= (cells.r_height[i] - 50) / 5.0; // low elevation is valued, high is not;

                if (cells.t[i] == 1)
                {
                    if (cells.r[i] != 0) s += 15; // estuary is valued
                    var type = f[cells.f[cells.haven[i]]].type;
                    var group = f[cells.f[cells.haven[i]]].group;
                    if (type == "lake")
                    {
                        // lake coast is valued
                        if (group == "freshwater") s += 30;
                        else if (group != "lava") s += 10;
                    }
                    else
                    {
                        s += 5; // ocean coast is valued
                        if (cells.harbor[i] == 1) s += 20; // safe sea harbor is valued
                    }
                }

                cells.s[i] = (short)(s / 5); // general population rate
                                             // cell rural population is suitability adjusted by cell area
                cells.pop[i] = cells.s[i] > 0 ? (float)(cells.s[i] * cells.area[i] / areaMean) : 0;
            }
        }
    }
}
