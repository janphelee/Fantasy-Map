using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Janphe.Fantasy.Map
{
    using CultureSet = Grid.Culture;

    internal class Map5Cultures
    {
        private Grid grid { get; set; }
        private Grid pack { get; set; }

        private int culturesInput { get; set; }
        private string culturesSet { get; set; }
        private int culturesSet_dataMax { get; set; }

        private Biomes biomesData { get; set; }
        private NamesGenerator.Names[] nameBases { get; set; }
        private NamesGenerator Names { get; set; }
        private int graphWidth { get; set; }
        private int graphHeight { get; set; }
        private int powerInput { get; set; }
        private int neutralInput { get; set; }


        /**
          <select id="culturesSet" data-stored="culturesSet">
           <option value="world" data-max="32" selected>All-world</option>
           <option value="european" data-max="15">European</option>
           <option value="oriental" data-max="13">Oriental</option>
           <option value="english" data-max="10">English</option>
           <option value="antique" data-max="10">Antique</option>
           <option value="highFantasy" data-max="23">High Fantasy</option>
           <option value="darkFantasy" data-max="18">Dark Fantasy</option>
           <option value="random" data-max="100">Random</option>
          </select>
        */
        public Map5Cultures(MapJobs map)
        {
            grid = map.grid;
            pack = map.pack;
            biomesData = map.biomesData;
            nameBases = map.nameBases;
            Names = map.Names;

            graphWidth = map.Options.Width;
            graphHeight = map.Options.Height;

            culturesInput = map.Options.CulturesNumber;
            culturesSet = map.Options.CulturesSet;
            culturesSet_dataMax = map.Options.CulturesSet_DataMax;
            powerInput = map.Options.PowerInput;
            neutralInput = map.Options.NeutralInput;
        }

        public void generate()
        {
            var cells = pack.cells;
            cells.culture = new ushort[cells.i.Length];
            //var count = Math.Min(culturesInput, +culturesSet.selectedOptions[0].dataset.max);
            var count = Math.Min(culturesInput, culturesSet_dataMax);

            Debug.Log($"Map5Cultures.generate {culturesInput} {culturesSet} {culturesSet_dataMax} {powerInput} {neutralInput}");

            var populated = cells.i.Where(i => cells.s[i] > 0).ToArray(); // populated cells
            if (populated.Length < count * 25)
            {
                count = (int)Math.Floor(populated.Length / 50.0);
                if (count == 0)
                {
                    Debug.LogError("There are no populated cells.Cannot generate cultures");
                    pack.cultures = new CultureSet[] {
                      new CultureSet() { name= "Wildlands", i= 0, @base= 1 }
                    };
                    return;
                }
                else
                {
                    Debug.LogError($"Not enought populated cells({ populated.Length}). Will generate only {count} cultures");
                }
            }

            //var msg = new List<string>();

            var cultures = getRandomCultures(count);
            var centers = D3.quadtree();
            var colors = Utils.getColors(count);

            pack.cultures = cultures.ToArray();
            for (var i = 0; i < cultures.Count; ++i)
            {
                var c = cultures[i];
                var cell = c.center = placeCenter(c.sort != null ? c.sort : (_) => cells.s[_]);
                var p = cells.r_points[cell];
                centers.add(new D3.Quadtree.Value(p[0], p[1], cell));
                c.i = i + 1;

                c.odd = 0;
                c.sort = null;

                c.color = colors[i];
                c.type = defineCultureType(cell);
                c.expansionism = (float)defineCultureExpansionism(c.type);
                c.origin = 0;
                c.code = getCode(c.name, pack.cultures);
                cells.culture[cell] = (ushort)(i + 1);

                //msg.Add($"{c.name} {c.code} {c.i} {c.center} {c.color}");
            }
            //Debug.SaveArray("Map5Cultures.txt", msg);

            // the first culture with id 0 is for wildlands
            cultures.Insert(0, new CultureSet() { name = "Wildlands", i = 0, @base = 1, origin = 0 });

            // make sure all bases exist in nameBases
            if (nameBases.Length == 0)
            {
                Debug.LogError("Name base is empty, default nameBases will be applied");
                nameBases = NamesGenerator.getNameBases();
            }
            foreach (var c in cultures)
            { c.@base = c.@base % nameBases.Length; }

            pack.cultures = cultures.ToArray();
            //Debug.SaveArray("pack.cultures.txt", cultures.map(s => s == null ? "" : $"{'{'}'name':'{s.name}','base':{s.@base},'center':{s.center},'i':{s.i},'color':'{s.color}','type':'{s.type}','expansionism':{s.expansionism},'origin':{s.origin},'code':'{s.code}'{'}'}"));

            ////////////////////////////////////////////////////////////////////
            int placeCenter(Func<int, double> v)
            {
                int c;
                var spacing = (graphWidth + graphHeight) / 2.0 / count;
                var max = Math.Floor(populated.Length / 2.0);
                var sorted = Utils
                    .LinqSort(populated, (a, b) => v(b).CompareTo(v(a)))
                    .Take((int)max)
                    .ToArray();

                //foreach (var p in sorted) msg.Add($"{p} {v(p)}");
                do
                {
                    c = sorted[Utils.biased(0, max, 5)];
                    spacing *= .9;
                    //msg.Add($"{c} {spacing} {max}");
                }
                while (centers.find(cells.r_points[c][0], cells.r_points[c][1], spacing) != null);

                return c;

            }
            // set culture type based on culture center position
            string defineCultureType(int i)
            {
                byte[] b1 = { 1, 2, 4 };
                byte[] b2 = { 3, 7, 8, 9, 10, 12 };

                if (cells.r_height[i] < 70 && b1.includes(cells.biome[i]))
                    return "Nomadic"; // high penalty in forest biomes and near coastline
                if (cells.r_height[i] > 50)
                    return "Highland"; // no penalty for hills and moutains, high for other elevations
                var f = pack.features[cells.f[cells.haven[i]]]; // opposite feature
                if (f.type == "lake" && f.cells > 5)
                    return "Lake"; // low water cross penalty and high for growth not along coastline
                if (cells.harbor[i] != 0 && f.type != "lake" && Utils.P(.1)
                    || (cells.harbor[i] == 1 && Utils.P(.6))
                    || (pack.features[cells.f[i]].group == "isle" && Utils.P(.4))
                    )
                    return "Naval"; // low water cross penalty and high for non-along-coastline growth

                if (cells.r[i] != 0 && cells.fl[i] > 100)
                    return "River"; // no River cross penalty, penalty for non-River growth
                if (cells.t[i] > 2 && b2.includes(cells.biome[i]))
                    return "Hunting"; // high penalty in non-native biomes
                return "Generic";
            }

            double defineCultureExpansionism(string type)
            {
                double @base = 1; // Generic
                if (type == "Lake")
                    @base = .8;
                else
                if (type == "Naval")
                    @base = 1.5;
                else
                if (type == "River")
                    @base = .9;
                else
                if (type == "Nomadic")
                    @base = 1.5;
                else
                if (type == "Hunting")
                    @base = .7;
                else
                if (type == "Highland")
                    @base = 1.2;
                return Utils.rn((Random.NextDouble() * powerInput / 2.0 + 1) * @base, 1);
            }
        }

        public void draw(SKCanvas canvas, Func<IList<SKPoint>, SKPath> curve)
        {
            var cells = pack.cells;
            var vertices = pack.vertices;
            var cultures = pack.cultures;
            var n = cells.i.Length;

            var used = new BitArray(n);
            var paths = new Dictionary<int, List<List<int>>>();

            var pp = new List<string>();
            foreach (var i in cells.i)
            {
                if (0 == cells.culture[i])
                    continue;
                if (used[i])
                    continue;
                used[i] = true;
                var c = cells.culture[i];
                var onborder = cells.c[i].some(d => cells.culture[d] != c);
                if (!onborder)
                    continue;
                var vertex = cells.v[i].find(v => vertices.c[v].some(d => cells.culture[d] != c));
                var chain = connectVertices(vertex, c);
                pp.push($"{i} vertex:{vertex} c:{c} {chain.Count}");
                if (chain.Count < 3)
                    continue;

                if (!paths.ContainsKey(c))
                    paths[c] = new List<List<int>>();
                paths[c].Add(chain);
            }
            //Debug.SaveArray("Map5Cultures.culture.txt", cells.culture);
            //Debug.SaveArray("Map5Cultures.draw.txt", pp);

            var paint = new SKPaint() { IsAntialias = true };
            paint.StrokeWidth = 0.5f;

            foreach (var kv in paths)
            {
                var cul = cultures[kv.Key];
                kv.Value.forEach(d =>
                {
                    var points = d.map(v => vertices.p[v]).map(p => new SKPoint((float)p[0], (float)p[1])).ToArray();
                    var path = curve(points);

                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = cul.color.ToColor().Opacity(0.6f).SK();
                    canvas.DrawPath(path, paint);

                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = "#777777".ToColor().Opacity(0.6f).SK();
                    canvas.DrawPath(path, paint);
                });
            }

            List<int> connectVertices(int start, ushort t)
            {
                var chain = new List<int>();
                for (int i = 0, current = start; i == 0 || current != start && i < 20000; i++)
                {
                    var prev = chain.Count > 0 ? chain.Last() : -1;
                    chain.push(current); // add current vertex to sequence
                    var c = vertices.c[current]; // cells adjacent to vertex
                    c.filter(ci => cells.culture[ci] == t).forEach(ci => used[ci] = true);
                    var c0 = c[0] >= n || cells.culture[c[0]] != t;
                    var c1 = c[1] >= n || cells.culture[c[1]] != t;
                    var c2 = c[2] >= n || cells.culture[c[2]] != t;
                    var v = vertices.v[current]; // neighboring vertices
                    if (v[0] != prev && c0 != c1)
                        current = v[0];
                    else if (v[1] != prev && c1 != c2)
                        current = v[1];
                    else if (v[2] != prev && c0 != c2)
                        current = v[2];
                    if (current == chain[chain.Count - 1])
                    { Debug.Log("Next vertex is not found"); break; }
                }
                return chain;
            }
        }

        // assign a unique two-letters code (abbreviation)
        private static string getCode(string name, CultureSet[] cultures)
        {
            var words = name.Split(' ');
            var letters = words.reduce((s, v, i, d) => s + v);
            var code = words.Length == 2 ? "" + words[0][0] + words[1][0] : letters.slice(0, 2);
            for (var i = 1; i < letters.Length - 1 && cultures.some(c => c.code == code); i++)
            {
                code = letters[0] + letters[i].toUpperCase();
            }
            return code;
        }

        private List<CultureSet> getRandomCultures(int c)
        {
            var d = getDefault(c);
            var n = d.Length - 1;
            var count = Math.Min(c, d.Length);

            var cultures = new List<CultureSet>();
            while (cultures.Count < count)
            {
                var culture = d[(int)Utils.rand(n)];
                do
                {
                    culture = d[(int)Utils.rand(n)];
                } while (!Utils.P(culture.odd) || cultures.FindIndex(_ => _.name == culture.name) != -1);
                cultures.Add(culture);
            }
            return cultures;
        }

        class Item { public int e, c; public double p; }
        public void expand()
        {
            //var msg = new List<string>();

            var cells = pack.cells;

            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            foreach (var c in pack.cultures)
            {
                if (c.i == 0 || c.removed)
                    continue;
                queue.push(new Item() { e = c.center, p = 0, c = c.i });
                //msg.Add($"{c.center} {0} {c.i}");
            }

            var neutral = cells.i.Length / 5000d * 3000 * neutralInput;// limit cost for culture growth
            var cost = new Dictionary<int, double>();
            while (queue.Count > 0)
            {
                var next = queue.pop();
                int n = next.e, c = next.c;
                var p = next.p;
                var type = pack.cultures[c].type;
                //msg.Add($"------ {n} {c} {type} {p}");
                //msg.push($"{cells.c[n].join(",")}");

                foreach (var e in cells.r_neighbor_r[n])
                {
                    var biome = cells.biome[e];
                    var biomeCost = getBiomeCost(c, biome, type);
                    var biomeChangeCost = biome == cells.biome[n] ? 0 : 20; // penalty on biome change
                    var heightCost = getHeightCost(e, cells.r_height[e], type);
                    var riverCost = getRiverCost(cells.r[e], e, type);
                    var typeCost = getTypeCost(cells.t[e], type);
                    var total = biomeCost + biomeChangeCost + heightCost + riverCost + typeCost;
                    var tmp = (decimal)p + (decimal)total / (decimal)pack.cultures[c].expansionism;
                    var totalCost = Utils.rn((double)tmp, 6);

                    //msg.push($"{e} {total} {p} {c} {pack.cultures[c].expansionism} {totalCost}");

                    if (totalCost > neutral)
                        continue;

                    if (!cost.ContainsKey(e) || totalCost < cost[e])
                    {
                        if (cells.s[e] > 0)
                            cells.culture[e] = (ushort)c;// assign culture to populated cell
                        cost[e] = totalCost;
                        queue.push(new Item() { e = e, p = totalCost, c = c });

                        //msg.Add($"++++++ {n} {c} {e} {total} {pack.cultures[c].expansionism} {totalCost}");
                    }
                }
            }
            //Debug.SaveArray("Map5Cultures.expand.txt", msg);

            int getBiomeCost(int c, int biome, string type)
            {
                if (cells.biome[pack.cultures[c].center] == biome)
                    return 10; // tiny penalty for native biome
                if (type == "Hunting")
                    return biomesData.cost[biome] * 5; // non-native biome penalty for hunters
                if (type == "Nomadic" && biome > 4 && biome < 10)
                    return biomesData.cost[biome] * 10; // forest biome penalty for nomads
                return biomesData.cost[biome] * 2; // general non-native biome penalty
            }
            int getHeightCost(int i, int h, string type)
            {
                var f = pack.features[cells.f[i]];
                var a = cells.area[i];
                if (type == "Lake" && f.type == "lake")
                    return 10; // no lake crossing penalty for Lake cultures
                if (type == "Naval" && h < 20)
                    return a * 2; // low sea/lake crossing penalty for Naval cultures
                if (type == "Nomadic" && h < 20)
                    return a * 50; // giant sea/lake crossing penalty for Nomads
                if (h < 20)
                    return a * 6; // general sea/lake crossing penalty
                if (type == "Highland" && h < 44)
                    return 3000; // giant penalty for highlanders on lowlands
                if (type == "Highland" && h < 62)
                    return 200; // giant penalty for highlanders on lowhills
                if (type == "Highland")
                    return 0; // no penalty for highlanders on highlands
                if (h >= 67)
                    return 200; // general mountains crossing penalty
                if (h >= 44)
                    return 30; // general hills crossing penalty
                return 0;
            }
            double getRiverCost(int r, int i, string type)
            {
                if (type == "River")
                    return r != 0 ? 0 : 100; // penalty for river cultures
                if (r == 0)
                    return 0; // no penalty for others if there is no river
                return Math.Min(Math.Max(cells.fl[i] / 10.0, 20), 100); // river penalty from 20 to 100 based on flux
            }
            int getTypeCost(int t, string type)
            {
                if (t == 1)
                    return type == "Naval" || type == "Lake" ? 0 : type == "Nomadic" ? 60 : 20; // penalty for coastline
                if (t == 2)
                    return type == "Naval" || type == "Nomadic" ? 30 : 0; // low penalty for land level 2 for Navals and nomads
                if (t != -1)
                    return type == "Naval" || type == "Lake" ? 100 : 0;  // penalty for mainland for navals
                return 0;
            }
        }


        private CultureSet[] getDefault(int count)
        {
            // generic sorting functions
            var cells = pack.cells;
            var s = cells.s;
            var sMax = s.Max();
            var t = cells.t;
            var h = cells.r_height;
            var temp = grid.cells.temp;
            double n(int cell) => Math.Ceiling((double)s[cell] / sMax * 3); // normalized cell score
            double td(int cell, int goal)
            { var d = Math.Abs(temp[cells.g[cell]] - goal); return d > 0 ? d + 1 : 1; } // temperature difference fee
            double bd(int cell, int[] biomes, int fee = 4) => biomes.includes(cells.biome[cell]) ? 1 : fee; // biome difference fee
            double sf(int cell, int fee = 4) => cells.haven[cell] != 0 && pack.features[cells.f[cells.haven[cell]]].type != "lake" ? 1 : fee; // not on sea coast fee

            // https://en.wikipedia.org/wiki/List_of_cities_by_average_temperature

            if (culturesSet == "european")
            {
                return new CultureSet[] {
        new CultureSet () { name = "Shwazen", @base = 0, odd = 1, sort = i => n (i) / td (i, 10) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Angshire", @base = 1, odd = 1, sort = i => n (i) / td (i, 10) / sf (i) },
        new CultureSet () { name = "Luari", @base = 2, odd = 1, sort = i => n (i) / td (i, 12) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Tallian", @base = 3, odd = 1, sort = i => n (i) / td (i, 15) },
        new CultureSet () { name = "Astellian", @base = 4, odd = 1, sort = i => n (i) / td (i, 16) },
        new CultureSet () { name = "Slovan", @base = 5, odd = 1, sort = i => n (i) / td (i, 6) * t[i] },
        new CultureSet () { name = "Norse", @base = 6, odd = 1, sort = i => n (i) / td (i, 5) },
        new CultureSet () { name = "Elladan", @base = 7, odd = 1, sort = i => n (i) / td (i, 18) * h[i] },
        new CultureSet () { name = "Romian", @base = 8, odd = .2, sort = i => n (i) / td (i, 15) / t[i] },
        new CultureSet () { name = "Soumi", @base = 9, odd = 1, sort = i => n (i) / td (i, 5) / bd (i, new int[] { 9 }) * t[i] },
        new CultureSet () { name = "Portuzian", @base = 13, odd = 1, sort = i => n (i) / td (i, 17) / sf (i) },
        new CultureSet () { name = "Vengrian", @base = 15, odd = 1, sort = i => n (i) / td (i, 11) / bd (i, new int[] { 4 }) * t[i] },
        new CultureSet () { name = "Turchian", @base = 16, odd = .05, sort = i => n (i) / td (i, 14) },
        new CultureSet () { name = "Euskati", @base = 20, odd = .05, sort = i => n (i) / td (i, 15) * h[i] },
        new CultureSet () { name = "Keltan", @base = 22, odd = .05, sort = i => n (i) / td (i, 11) / bd (i, new int[] { 6, 8 }) * t[i] }
        };
            }

            if (culturesSet == "oriental")
            {
                CultureSet[] ret = {
                    new CultureSet () { name = "Koryo", @base = 10, odd = 1, sort = i => n (i) / td (i, 12) / t[i] },
                    new CultureSet () { name = "Hantzu", @base = 11, odd = 1, sort = i => n (i) / td (i, 13) },
                    new CultureSet () { name = "Yamoto", @base = 12, odd = 1, sort = i => n (i) / td (i, 15) / t[i] },
                    new CultureSet () { name = "Turchian", @base = 16, odd = 1, sort = i => n (i) / td (i, 12) },
                    new CultureSet () { name = "Berberan", @base = 17, odd = .2, sort = i => n (i) / td (i, 19) / bd (i, new int[] { 1, 2, 3 }, 7) * t[i] },
                    new CultureSet () { name = "Eurabic", @base = 18, odd = 1, sort = i => n (i) / td (i, 26) / bd (i, new int[] { 1, 2 }, 7) * t[i] },
                    new CultureSet () { name = "Efratic", @base = 23, odd = .1, sort = i => n (i) / td (i, 22) * t[i] },
                    new CultureSet () { name = "Tehrani", @base = 24, odd = 1, sort = i => n (i) / td (i, 18) * h[i] },
                    new CultureSet () { name = "Maui", @base = 25, odd = .2, sort = i => n (i) / td (i, 24) / sf (i) / t[i] },
                    new CultureSet () { name = "Carnatic", @base = 26, odd = .5, sort = i => n (i) / td (i, 26) },
                    new CultureSet () { name = "Vietic", @base = 29, odd = .8, sort = i => n (i) / td (i, 25) / bd (i, new int[] { 7 }, 7) / t[i] },
                    new CultureSet () { name = "Guantzu", @base = 30, odd = .5, sort = i => n (i) / td (i, 17) },
                    new CultureSet () { name = "Ulus", @base = 31, odd = 1, sort = i => n (i) / td (i, 5) / bd (i, new int[] { 2, 4, 10 }, 7) * t[i] }
                };
                return ret;
            }

            if (culturesSet == "english")
            {
                string getName() => Names.getBase(1, 5, 9, "", 0);
                return new CultureSet[] {
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 },
            new CultureSet () { name = getName (), @base = 1, odd = 1 }
        };
            }

            if (culturesSet == "antique")
            {
                return new CultureSet[] {
        new CultureSet () { name = "Roman", @base = 8, odd = 1, sort = i => n (i) / td (i, 14) / t[i] }, // Roman
        new CultureSet () { name = "Roman", @base = 8, odd = 1, sort = i => n (i) / td (i, 15) / sf (i) }, // Roman
        new CultureSet () { name = "Roman", @base = 8, odd = 1, sort = i => n (i) / td (i, 16) / sf (i) }, // Roman
        new CultureSet () { name = "Roman", @base = 8, odd = 1, sort = i => n (i) / td (i, 17) / t[i] }, // Roman
        new CultureSet () { name = "Hellenic", @base = 7, odd = 1, sort = i => n (i) / td (i, 18) / sf (i) * h[i] }, // Greek
        new CultureSet () { name = "Hellenic", @base = 7, odd = 1, sort = i => n (i) / td (i, 19) / sf (i) * h[i] }, // Greek
        new CultureSet () { name = "Macedonian", @base = 7, odd = .5, sort = i => n (i) / td (i, 12) * h[i] }, // Greek
        new CultureSet () { name = "Celtic", @base = 22, odd = 1, sort = i => n (i) / td (i, 11).pow (.5) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Germanic", @base = 0, odd = 1, sort = i => n (i) / td (i, 10).pow (.5) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Persian", @base = 24, odd = .8, sort = i => n (i) / td (i, 18) * h[i] }, // Iranian
        new CultureSet () { name = "Scythian", @base = 24, odd = .5, sort = i => n (i) / td (i, 11).pow (.5) / bd (i, new int[] { 4 }) }, // Iranian
        new CultureSet () { name = "Cantabrian", @base = 20, odd = .5, sort = i => n (i) / td (i, 16) * h[i] }, // Basque
        new CultureSet () { name = "Estian", @base = 9, odd = .2, sort = i => n (i) / td (i, 5) * t[i] }, // Finnic
        new CultureSet () { name = "Carthaginian", @base = 17, odd = .3, sort = i => n (i) / td (i, 19) / sf (i) }, // Berber
        new CultureSet () { name = "Mesopotamian", @base = 23, odd = .2, sort = i => n (i) / td (i, 22) / bd (i, new int[] { 1, 2, 3 }) } // Mesopotamian
        };
            }

            if (culturesSet == "highFantasy")
            {
                return new CultureSet[] {
        // fantasy races
        new CultureSet () { name = "Quenian", @base = 33, odd = 1, sort = i => n (i) / bd (i, new int[] { 6, 7, 8, 9 }, 10) * t[i] }, // Elves
        new CultureSet () { name = "Eldar", @base = 33, odd = 1, sort = i => n (i) / bd (i, new int[] { 6, 7, 8, 9 }, 10) * t[i] }, // Elves
        new CultureSet () { name = "Lorian", @base = 33, odd = .5, sort = i => n (i) / bd (i, new int[] { 6, 7, 8, 9 }, 10) }, // Elves
        new CultureSet () { name = "Trow", @base = 34, odd = .9, sort = i => n (i) / bd (i, new int[] { 7, 8, 9, 12 }, 10) * t[i] }, // Dark Elves
        new CultureSet () { name = "Dokalfar", @base = 34, odd = .3, sort = i => n (i) / bd (i, new int[] { 7, 8, 9, 12 }, 10) * t[i] }, // Dark Elves
        new CultureSet () { name = "Durinn", @base = 35, odd = 1, sort = i => n (i) + h[i] }, // Dwarven
        new CultureSet () { name = "Khazadur", @base = 35, odd = 1, sort = i => n (i) + h[i] }, // Dwarven
        new CultureSet () { name = "Kobblin", @base = 36, odd = 1, sort = i => t[i] - s[i] }, // Goblin
        new CultureSet () { name = "Uruk", @base = 37, odd = 1, sort = i => h[i] * t[i] }, // Orc
        new CultureSet () { name = "Ugluk", @base = 37, odd = .7, sort = i => h[i] * t[i] / bd (i, new int[] { 1, 2, 10, 11 }) }, // Orc
        new CultureSet () { name = "Yotunn", @base = 38, odd = .9, sort = i => td (i, -10) }, // Giant
        new CultureSet () { name = "Drake", @base = 39, odd = .7, sort = i => -s[i] }, // Draconic
        new CultureSet () { name = "Rakhnid", @base = 40, odd = .9, sort = i => t[i] - s[i] }, // Arachnid
        new CultureSet () { name = "Aj'Snaga", @base = 41, odd = .9, sort = i => n (i) / bd (i, new int[] { 12 }, 10) }, // Serpents
        // common fantasy human
        new CultureSet () { name = "Gozdor", @base = 32, odd = 1, sort = i => n (i) / td (i, 18) },
        new CultureSet () { name = "Anor", @base = 32, odd = 1, sort = i => n (i) / td (i, 10) },
        new CultureSet () { name = "Dail", @base = 32, odd = 1, sort = i => n (i) / td (i, 13) },
        new CultureSet () { name = "Duland", @base = 32, odd = 1, sort = i => n (i) / td (i, 14) },
        new CultureSet () { name = "Rohand", @base = 32, odd = 1, sort = i => n (i) / td (i, 16) },
        // rare real-world western
        new CultureSet () { name = "Norse", @base = 6, odd = .5, sort = i => n (i) / td (i, 5) / sf (i) },
        new CultureSet () { name = "Izenlute", @base = 0, odd = .1, sort = i => n (i) / td (i, 5) },
        new CultureSet () { name = "Lurian", @base = 2, odd = .1, sort = i => n (i) / td (i, 12) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Getalian", @base = 3, odd = .1, sort = i => n (i) / td (i, 15) },
        new CultureSet () { name = "Astelan", @base = 4, odd = .05, sort = i => n (i) / td (i, 16) },
        // rare real-world exotic
        new CultureSet () { name = "Yoruba", @base = 21, odd = .05, sort = i => n (i) / td (i, 15) / bd (i, new int[] { 5, 7 }) },
        new CultureSet () { name = "Ryoko", @base = 10, odd = .05, sort = i => n (i) / td (i, 12) / t[i] },
        new CultureSet () { name = "Toyamo", @base = 12, odd = .05, sort = i => n (i) / td (i, 15) / t[i] },
        new CultureSet () { name = "Guan-Tsu", @base = 30, odd = .05, sort = i => n (i) / td (i, 17) },
        new CultureSet () { name = "Ulus-Khan", @base = 31, odd = .05, sort = i => n (i) / td (i, 5) / bd (i, new int[] { 2, 4, 10 }, 7) * t[i] },
        new CultureSet () { name = "Turan", @base = 16, odd = .05, sort = i => n (i) / td (i, 13) },
        new CultureSet () { name = "Al'Uma", @base = 18, odd = .05, sort = i => n (i) / td (i, 26) / bd (i, new int[] { 1, 2 }, 7) * t[i] },
        new CultureSet () { name = "Druidas", @base = 22, odd = .05, sort = i => n (i) / td (i, 11) / bd (i, new int[] { 6, 8 }) * t[i] },
        new CultureSet () { name = "Gorodian", @base = 5, odd = .05, sort = i => n (i) / td (i, 6) * t[i] }
        };
            }

            if (culturesSet == "darkFantasy")
            {
                return new CultureSet[] {
        // common real-world English
        new CultureSet () { name = "Angshire", @base = 1, odd = 1, sort = i => n (i) / td (i, 10) / sf (i) },
        new CultureSet () { name = "Enlandic", @base = 1, odd = 1, sort = i => n (i) / td (i, 12) },
        new CultureSet () { name = "Westen", @base = 1, odd = 1, sort = i => n (i) / td (i, 10) },
        new CultureSet () { name = "Nortumbic", @base = 1, odd = 1, sort = i => n (i) / td (i, 7) },
        new CultureSet () { name = "Mercian", @base = 1, odd = 1, sort = i => n (i) / td (i, 9) },
        new CultureSet () { name = "Kentian", @base = 1, odd = 1, sort = i => n (i) / td (i, 12) },
        // rare real-world western
        new CultureSet () { name = "Norse", @base = 6, odd = .7, sort = i => n (i) / td (i, 5) / sf (i) },
        new CultureSet () { name = "Schwarzen", @base = 0, odd = .3, sort = i => n (i) / td (i, 10) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Luarian", @base = 2, odd = .3, sort = i => n (i) / td (i, 12) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Hetallian", @base = 3, odd = .3, sort = i => n (i) / td (i, 15) },
        new CultureSet () { name = "Astellian", @base = 4, odd = .3, sort = i => n (i) / td (i, 16) },
        // rare real-world exotic
        new CultureSet () { name = "Kiswaili", @base = 28, odd = .05, sort = i => n (i) / td (i, 29) / bd (i, new int[] { 1, 3, 5, 7 }) },
        new CultureSet () { name = "Yoruba", @base = 21, odd = .05, sort = i => n (i) / td (i, 15) / bd (i, new int[] { 5, 7 }) },
        new CultureSet () { name = "Koryo", @base = 10, odd = .05, sort = i => n (i) / td (i, 12) / t[i] },
        new CultureSet () { name = "Hantzu", @base = 11, odd = .05, sort = i => n (i) / td (i, 13) },
        new CultureSet () { name = "Yamoto", @base = 12, odd = .05, sort = i => n (i) / td (i, 15) / t[i] },
        new CultureSet () { name = "Guantzu", @base = 30, odd = .05, sort = i => n (i) / td (i, 17) },
        new CultureSet () { name = "Ulus", @base = 31, odd = .05, sort = i => n (i) / td (i, 5) / bd (i, new int[] { 2, 4, 10 }, 7) * t[i] },
        new CultureSet () { name = "Turan", @base = 16, odd = .05, sort = i => n (i) / td (i, 12) },
        new CultureSet () { name = "Berberan", @base = 17, odd = .05, sort = i => n (i) / td (i, 19) / bd (i, new int[] { 1, 2, 3 }, 7) * t[i] },
        new CultureSet () { name = "Eurabic", @base = 18, odd = .05, sort = i => n (i) / td (i, 26) / bd (i, new int[] { 1, 2 }, 7) * t[i] },
        new CultureSet () { name = "Slovan", @base = 5, odd = .05, sort = i => n (i) / td (i, 6) * t[i] },
        new CultureSet () { name = "Keltan", @base = 22, odd = .1, sort = i => n (i) / td (i, 11).pow (.5) / bd (i, new int[] { 6, 8 }) },
        new CultureSet () { name = "Elladan", @base = 7, odd = .2, sort = i => n (i) / td (i, 18) / sf (i) * h[i] },
        new CultureSet () { name = "Romian", @base = 8, odd = .2, sort = i => n (i) / td (i, 14) / t[i] },
        // fantasy races
        new CultureSet () { name = "Eldar", @base = 33, odd = .5, sort = i => n (i) / bd (i, new int[] { 6, 7, 8, 9 }, 10) * t[i] }, // Elves
        new CultureSet () { name = "Trow", @base = 34, odd = .8, sort = i => n (i) / bd (i, new int[] { 7, 8, 9, 12 }, 10) * t[i] }, // Dark Elves
        new CultureSet () { name = "Durinn", @base = 35, odd = .8, sort = i => n (i) + h[i] }, // Dwarven
        new CultureSet () { name = "Kobblin", @base = 36, odd = .8, sort = i => t[i] - s[i] }, // Goblin
        new CultureSet () { name = "Uruk", @base = 37, odd = .8, sort = i => h[i] * t[i] / bd (i, new int[] { 1, 2, 10, 11 }) }, // Orc
        new CultureSet () { name = "Yotunn", @base = 38, odd = .8, sort = i => td (i, -10) }, // Giant
        new CultureSet () { name = "Drake", @base = 39, odd = .9, sort = i => -s[i] }, // Draconic
        new CultureSet () { name = "Rakhnid", @base = 40, odd = .9, sort = i => t[i] - s[i] }, // Arachnid
        new CultureSet () { name = "Aj'Snaga", @base = 41, odd = .9, sort = i => n (i) / bd (i, new int[] { 12 }, 10) }, // Serpents
        };
            }

            if (culturesSet == "random")
            {
                var ret = new CultureSet[count];
                for (var i = 0; i < count; ++i)
                {
                    var rnd = (int)Utils.rand(nameBases.Length - 1);
                    ret[i] = new CultureSet { name = Names.getBaseShort(rnd), @base = rnd, odd = 1 };
                }
                return ret;
            }

            // all-world
            return new CultureSet[] {
        new CultureSet () { name = "Shwazen", @base = 0, odd = .7, sort = i => n (i) / td (i, 10) / bd (i, new int[] { 6, 8 }) },
          new CultureSet () { name = "Angshire", @base = 1, odd = 1, sort = i => n (i) / td (i, 10) / sf (i) },
          new CultureSet () { name = "Luari", @base = 2, odd = .6, sort = i => n (i) / td (i, 12) / bd (i, new int[] { 6, 8 }) },
          new CultureSet () { name = "Tallian", @base = 3, odd = .6, sort = i => n (i) / td (i, 15) },
          new CultureSet () { name = "Astellian", @base = 4, odd = .6, sort = i => n (i) / td (i, 16) },
          new CultureSet () { name = "Slovan", @base = 5, odd = .7, sort = i => n (i) / td (i, 6) * t[i] },
          new CultureSet () { name = "Norse", @base = 6, odd = .7, sort = i => n (i) / td (i, 5) },
          new CultureSet () { name = "Elladan", @base = 7, odd = .7, sort = i => n (i) / td (i, 18) * h[i] },
          new CultureSet () { name = "Romian", @base = 8, odd = .7, sort = i => n (i) / td (i, 15) },
          new CultureSet () { name = "Soumi", @base = 9, odd = .3, sort = i => n (i) / td (i, 5) / bd (i, new int[] { 9 }) * t[i] },
          new CultureSet () { name = "Koryo", @base = 10, odd = .1, sort = i => n (i) / td (i, 12) / t[i] },
          new CultureSet () { name = "Hantzu", @base = 11, odd = .1, sort = i => n (i) / td (i, 13) },
          new CultureSet () { name = "Yamoto", @base = 12, odd = .1, sort = i => n (i) / td (i, 15) / t[i] },
          new CultureSet () { name = "Portuzian", @base = 13, odd = .4, sort = i => n (i) / td (i, 17) / sf (i) },
          new CultureSet () { name = "Nawatli", @base = 14, odd = .1, sort = i => h[i] / td (i, 18) / bd (i, new int[] { 7 }) },
          new CultureSet () { name = "Vengrian", @base = 15, odd = .2, sort = i => n (i) / td (i, 11) / bd (i, new int[] { 4 }) * t[i] },
          new CultureSet () { name = "Turchian", @base = 16, odd = .2, sort = i => n (i) / td (i, 13) },
          new CultureSet () { name = "Berberan", @base = 17, odd = .1, sort = i => n (i) / td (i, 19) / bd (i, new int[] { 1, 2, 3 }, 7) * t[i] },
          new CultureSet () { name = "Eurabic", @base = 18, odd = .2, sort = i => n (i) / td (i, 26) / bd (i, new int[] { 1, 2 }, 7) * t[i] },
          new CultureSet () { name = "Inuk", @base = 19, odd = .05, sort = i => td (i, -1) / bd (i, new int[] { 10, 11 }) / sf (i) },
          new CultureSet () { name = "Euskati", @base = 20, odd = .05, sort = i => n (i) / td (i, 15) * h[i] },
          new CultureSet () { name = "Yoruba", @base = 21, odd = .05, sort = i => n (i) / td (i, 15) / bd (i, new int[] { 5, 7 }) },
          new CultureSet () { name = "Keltan", @base = 22, odd = .05, sort = i => n (i) / td (i, 11) / bd (i, new int[] { 6, 8 }) * t[i] },
          new CultureSet () { name = "Efratic", @base = 23, odd = .05, sort = i => n (i) / td (i, 22) * t[i] },
          new CultureSet () { name = "Tehrani", @base = 24, odd = .1, sort = i => n (i) / td (i, 18) * h[i] },
          new CultureSet () { name = "Maui", @base = 25, odd = .05, sort = i => n (i) / td (i, 24) / sf (i) / t[i] },
          new CultureSet () { name = "Carnatic", @base = 26, odd = .05, sort = i => n (i) / td (i, 26) },
          new CultureSet () { name = "Inqan", @base = 27, odd = .05, sort = i => h[i] / td (i, 13) },
          new CultureSet () { name = "Kiswaili", @base = 28, odd = .1, sort = i => n (i) / td (i, 29) / bd (i, new int[] { 1, 3, 5, 7 }) },
          new CultureSet () { name = "Vietic", @base = 29, odd = .1, sort = i => n (i) / td (i, 25) / bd (i, new int[] { 7 }, 7) / t[i] },
          new CultureSet () { name = "Guantzu", @base = 30, odd = .1, sort = i => n (i) / td (i, 17) },
          new CultureSet () { name = "Ulus", @base = 31, odd = .1, sort = i => n (i) / td (i, 5) / bd (i, new int[] { 2, 4, 10 }, 7) * t[i] }
      };
        }
    }
}
