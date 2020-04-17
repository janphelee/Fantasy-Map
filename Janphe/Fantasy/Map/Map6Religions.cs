using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe.Fantasy.Map
{
    using System.Collections;
    using SkiaSharp;
    using static Grid;

    internal partial class Map6Religions
    {
        private MapJobs mapJobs { get; set; }
        private NamesGenerator Names { get; set; }
        private Grid pack { get; set; }

        private int religionsInput { get; set; }
        private int neutralInput { get; set; }

        private int graphWidth { get; set; }
        private int graphHeight { get; set; }

        public Map6Religions(MapJobs map)
        {
            mapJobs = map;
            pack = map.pack;
            Names = map.Names;

            religionsInput = map.Options.ReligionsNumber;
            neutralInput = map.Options.NeutralInput;
            Debug.Log($"Religions {religionsInput} {neutralInput}");

            graphWidth = map.Options.Width;
            graphHeight = map.Options.Height;
        }

        public void generate()
        {
            var cells = pack.cells;
            var states = pack.states;
            var cultures = pack.cultures;

            var religions = pack.religions = new List<Religion>();
            var religion = cells.religion = new ushort[cells.culture.Length];// cell religion; initially based on culture
            Array.Copy(cells.culture, religion, religion.Length);

            cultures.forEach(c =>
            {
                if (0 == c.i)
                { religions.push(new Religion { i = 0, name = "No religion" }); return; }
                if (c.removed)
                { religions.push(new Religion { i = c.i, name = "Extinct religion for " + c.name, color = Utils.getMixedColor(c.color, .1, 0), removed = true }); return; }
                var form = Utils.rw(forms["Folk"]);
                var name = c.name + " " + Utils.rw(types[form]);
                var deity = form == "Animism" ? null : getDeityName(c.i);
                var color = Utils.getMixedColor(c.color, .1, 0); // `url(#hatch${rand(8,13)})`;
                religions.push(new Religion { i = c.i, name = name, color = color, culture = c.i, type = "Folk", form = form, deity = deity, center = c.center, origin = 0 });
            });
            if (religionsInput == 0 || cultures.Length < 2)
            {
                religions.filter(r => r.i != 0).forEach(r => r.code = getCode(r.name));
                return;
            }

            var burgs = pack.burgs.filter(b => null != b && b.i != 0 && !b.removed);
            var sorting = burgs.Count() > religionsInput
                ? burgs.sort((a, b) => b.population.CompareTo(a.population)).map(b => b.cell)
                : cells.i.filter(i => cells.s[i] > 2).sort((a, b) => cells.s[b].CompareTo(cells.s[a]));
            var sorted = sorting.ToArray();
            var religionsTree = D3.quadtree();
            var spacing = (graphWidth + graphHeight) / 6d / religionsInput; // base min distance between towns
            var cultsCount = Math.Floor(Utils.rand(10, 40) / 100d * religionsInput);
            var count = religionsInput - cultsCount + religions.Count;

            // generate organized religions
            for (var i = 0; religions.Count < count && i < 1000; i++)
            {
                var center = sorted[Utils.biased(0, sorted.Length - 1, 5)]; // religion center
                var form = Utils.rw(forms["Organized"]);
                var state = cells.state[center];
                var culture = cells.culture[center];

                var deity = form == "Non-theism" ? null : getDeityName(culture);
                var ret = getReligionName(form, deity, center);
                var name = ret[0];
                var expansion = ret[1];
                if (expansion == "state" && 0 == state)
                    expansion = "global";
                if (expansion == "culture" && 0 == culture)
                    expansion = "global";

                if (expansion == "state" && Random.NextDouble() > .5)
                    center = states[state].center;
                if (expansion == "culture" && Random.NextDouble() > .5)
                    center = cultures[culture].center;

                if (0 == cells.burg[center] && cells.c[center].some(c => cells.burg[c] != 0))
                    center = cells.c[center].find(c => cells.burg[c] != 0);
                var x = cells.p[center][0];
                var y = cells.p[center][1];

                var s = spacing * Utils.gauss(1, .3, .2, 2, 2); // randomize to make the placement not uniform
                if (religionsTree.find(x, y, s) != null)
                    continue; // to close to existing religion

                // add "Old" to name of the folk religion on this culture
                var folk = religions.find(r => r.culture == culture && r.type == "Folk");
                if (folk != null && expansion == "culture" && folk.name.slice(0, 3) != "Old")
                    folk.name = "Old " + folk.name;
                var origin = folk != null ? folk.i : 0;

                var expansionism = Utils.rand(3, 8);
                var color = Utils.getMixedColor(religions[origin].color, .3, 0); // `url(#hatch${rand(0,5)})`;
                religions.push(new Religion
                {
                    i = religions.Count,
                    name = name,
                    color = color,
                    culture = culture,
                    type = "Organized",
                    form = form,
                    deity = deity,
                    expansion = expansion,
                    expansionism = expansionism,
                    center = center,
                    origin = origin
                });
                religionsTree.add(new D3.Quadtree.Value(x, y, center));
            }

            // generate cults 膜拜, 礼拜式, 祭仪, 一群信徒
            for (var i = 0; religions.Count < count + cultsCount && i < 1000; i++)
            {
                var form = Utils.rw(forms["Cult"]);
                var center = sorted[Utils.biased(0, sorted.Length - 1, 1)]; // religion center
                if (0 == cells.burg[center] && cells.c[center].some(c => 0 != cells.burg[c]))
                    center = cells.c[center].find(c => 0 != cells.burg[c]);
                var x = cells.p[center][0];
                var y = cells.p[center][1];

                var s = spacing * Utils.gauss(2, .3, 1, 3, 2); // randomize to make the placement not uniform
                if (religionsTree.find(x, y, s) != null)
                    continue; // to close to existing religion

                var culture = cells.culture[center];
                var folk = religions.find(r => r.culture == culture && r.type == "Folk");
                var origin = null != folk ? folk.i : 0;
                var deity = getDeityName(culture);
                var name = getCultName(form, center);
                var expansionism = Utils.gauss(1.1, .5, 0, 5);
                var color = Utils.getMixedColor(cultures[culture].color, .5, 0); // "url(#hatch7)";
                religions.push(new Religion
                {
                    i = religions.Count,
                    name = name,
                    color = color,
                    culture = culture,
                    type = "Cult",
                    form = form,
                    deity = deity,
                    expansion = "global",
                    expansionism = expansionism,
                    center = center,
                    origin = origin
                });
                religionsTree.add(new D3.Quadtree.Value(x, y, center));
                //debug.append("circle").attr("cx", x).attr("cy", y).attr("r", 2).attr("fill", "red");
            }

            expandReligions(religions);

            // generate heresies 异端, 异教
            religions.filter(r => r.type == "Organized").ToList().forEach(r =>
            {
                if (r.expansionism < 3)
                    return;
                var IMAX = Utils.gauss(0, 1, 0, 3);
                for (var i = 0; i < IMAX; i++)
                {
                    var center = Utils.ra(cells.i.filter(ii => cells.religion[ii] == r.i && cells.c[ii].some(c => cells.religion[c] != r.i)).ToArray());
                    if (0 == center)
                        continue;
                    if (0 == cells.burg[center] && cells.c[center].some(c => 0 != cells.burg[c]))
                        center = cells.c[center].find(c => 0 != cells.burg[c]);
                    double x = cells.p[center][0], y = cells.p[center][1];
                    if (religionsTree.find(x, y, spacing / 10) != null)
                        continue; // to close to other

                    var culture = cells.culture[center];
                    var name = getCultName("Heresy", center);
                    var expansionism = Utils.gauss(1.2, .5, 0, 5);
                    var color = Utils.getMixedColor(r.color, .4, .2); // "url(#hatch6)";
                    religions.push(new Religion
                    {
                        i = religions.Count,
                        name = name,
                        color = color,
                        culture = culture,
                        type = "Heresy",
                        form = r.form,
                        deity = r.deity,
                        expansion = "global",
                        expansionism = expansionism,
                        center = center,
                        origin = r.i
                    });
                    religionsTree.add(new D3.Quadtree.Value(x, y, center));
                    //debug.append("circle").attr("cx", x).attr("cy", y).attr("r", 2).attr("fill", "green");
                }
            });

            expandHeresies(religions);
            checkCenters(religions);
        }

        public void add(int center)
        {
            var cells = pack.cells;
            var religions = pack.religions;
            var r = cells.religion[center];
            var i = religions.Count;
            var culture = cells.culture[center];
            var color = Utils.getMixedColor(religions[r].color, .3, 0);

            var type = religions[r].type == "Organized" ? Utils.rw("{ Organized: 4, Cult: 1, Heresy: 2}") : Utils.rw("{ Organized: 5, Cult: 2}");
            var form = Utils.rw(forms[type]);
            var deity = type == "Heresy" ? religions[r].deity : form == "Non-theism" ? null : getDeityName(culture);

            string name, expansion;
            if (type == "Organized")
            {
                var ret = getReligionName(form, deity, center);
                name = ret[0];
                expansion = ret[1];
            }
            else
            { name = getCultName(form, center); expansion = "global"; }
            var formName = type == "Heresy" ? religions[r].form : form;
            var code = getCode(name);
            religions.push(new Religion
            {
                i = i,
                name = name,
                color = color,
                culture = culture,
                type = type,
                form = formName,
                deity = deity,
                expansion = expansion,
                expansionism = 0,
                center = center,
                cells = 0,
                area = 0,
                rural = 0,
                urban = 0,
                origin = r,
                code = code
            });
            cells.religion[center] = (ushort)i;
        }

        class Item { public int e, r, s, c, b; public double p; }
        // growth algorithm to assign cells to religions
        private void expandReligions(IList<Religion> religions)
        {
            var biomesData = mapJobs.biomesData;

            var cells = pack.cells;
            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            var cost = new Dictionary<int, double>();

            religions.filter(r => r.type == "Organized" || r.type == "Cult").forEach(r =>
            {
                cells.religion[r.center] = (ushort)r.i;
                queue.push(new Item { e = r.center, p = 0, r = r.i, s = cells.state[r.center], c = r.culture });
                cost[r.center] = 1;
            });

            var neutral = cells.i.Length / 5000d * 200 * Utils.gauss(1, .3, .2, 2, 2) * neutralInput; // limit cost for organized religions growth
            var popCost = cells.pop.Max() / 3d; // enougth population to spered religion without penalty

            while (queue.Count > 0)
            {
                var next = queue.pop();
                int n = next.e, r = next.r, c = next.c, s = next.s;
                var p = next.p;
                var expansion = religions[r].expansion;

                cells.c[n].forEach(e =>
                {
                    if (expansion == "culture" && c != cells.culture[e])
                        return;
                    if (expansion == "state" && s != cells.state[e])
                        return;

                    var cultureCost = c != cells.culture[e] ? 10 : 0;
                    var stateCost = s != cells.state[e] ? 10 : 0;
                    var biomeCost = 0 != cells.road[e] ? 1 : biomesData.cost[cells.biome[e]];
                    var populationCost = Math.Max(Utils.rn(popCost - cells.pop[e]), 0);
                    var heightCost = Math.Max(cells.h[e] & 0xff, 20) - 20;
                    var waterCost = cells.h[e] < 20 ? 0 != cells.road[e] ? 50 : 1000 : 0;
                    var totalCost = p + (cultureCost + stateCost + biomeCost + populationCost + heightCost + waterCost) / religions[r].expansionism;
                    if (totalCost > neutral)
                        return;

                    if (!cost.ContainsKey(e) || totalCost < cost[e])
                    {
                        if (cells.h[e] >= 20 && 0 != cells.culture[e])
                            cells.religion[e] = (ushort)r; // assign religion to cell
                        cost[e] = totalCost;
                        queue.push(new Item { e = e, p = totalCost, r = r, c = c, s = s });
                    }
                });
            }
        }

        // growth algorithm to assign cells to heresies
        private void expandHeresies(IList<Religion> religions)
        {
            var biomesData = mapJobs.biomesData;

            var cells = pack.cells;
            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            var cost = new Dictionary<int, double>();

            religions.filter(r => r.type == "Heresy").forEach(r =>
            {
                var b = cells.religion[r.center]; // "base" religion id
                cells.religion[r.center] = (ushort)r.i; // heresy id
                queue.push(new Item { e = r.center, p = 0, r = r.i, b = b });
                cost[r.center] = 1;
            });

            var neutral = cells.i.Length / 5000d * 500 * neutralInput; // limit cost for heresies growth

            while (queue.Count > 0)
            {
                var next = queue.pop();
                int n = next.e, r = next.r, b = next.b;
                var p = next.p;

                cells.c[n].forEach(e =>
                {
                    var religionCost = cells.religion[e] == b ? 0 : 2000;
                    var biomeCost = 0 != cells.road[e] ? 0 : biomesData.cost[cells.biome[e]];
                    var heightCost = Math.Max((int)cells.h[e], 20) - 20;
                    var waterCost = cells.h[e] < 20 ? 0 != cells.road[e] ? 50 : 1000 : 0;
                    var totalCost = p + (religionCost + biomeCost + heightCost + waterCost) / Math.Max(religions[r].expansionism, .1);

                    if (totalCost > neutral)
                        return;

                    if (!cost.ContainsKey(e) || totalCost < cost[e])
                    {
                        if (cells.h[e] >= 20 && 0 != cells.culture[e])
                            cells.religion[e] = (ushort)r; // assign religion to cell
                        cost[e] = totalCost;
                        queue.push(new Item { e = e, p = totalCost, r = r });
                    }
                });
            }
        }

        private void checkCenters(IList<Religion> religions)
        {
            var cells = pack.cells;
            religions.filter(r => 0 != r.i).forEach(r =>
            {
                // generate religion code (abbreviation)
                r.code = getCode(r.name);

                // move religion center if it's not within religion area after expansion
                if (cells.religion[r.center] == r.i)
                    return; // in area
                var religCells = cells.i.filter(i => cells.religion[i] == r.i);
                if (0 == religCells.Count())
                    return; // extinct religion
                r.center = religCells.sort((a, b) => cells.pop[b].CompareTo(cells.pop[a])).First();
            });
        }

        // assign a unique two-letters code (abbreviation)
        private string getCode(string rawName)
        {
            var name = rawName.replace("Old ", ""); // remove Old prefix
            var words = name.split(" ");
            var letters = words.join("");
            //Debug.Log($"getCode letters:{letters} name:{name} rawName:{rawName} words:{words.Length}");
            var code = words.Length == 2 ? "" + words[0][0] + words[1][0] : letters.slice(0, 2);
            for (var i = 1; i < letters.Length - 1 && pack.religions.some(r => r.code == code); i++)
            {
                code = letters[0] + letters[i].toUpperCase();
            }
            return code;
        }

        // get supreme deity name
        private string getDeityName(int culture)
        {
            var meaning = generateMeaning();
            var cultureName = Names.getCulture(culture, 0, 0, "", .8);
            return cultureName + ", The " + meaning;
        }

        private string generateMeaning()
        {
            var a = Utils.ra(approaches); // select generation approach
            if (a == "Number")
                return Utils.ra(@base["number"]);
            if (a == "Being")
                return Utils.ra(@base["being"]);
            if (a == "Adjective")
                return Utils.ra(@base["adjective"]);
            if (a == "Color + Animal")
                return Utils.ra(@base["color"]) + " " + Utils.ra(@base["animal"]);
            if (a == "Adjective + Animal")
                return Utils.ra(@base["adjective"]) + " " + Utils.ra(@base["animal"]);
            if (a == "Adjective + Being")
                return Utils.ra(@base["adjective"]) + " " + Utils.ra(@base["being"]);
            if (a == "Adjective + Genitive")
                return Utils.ra(@base["adjective"]) + " " + Utils.ra(@base["genitive"]);
            if (a == "Color + Being")
                return Utils.ra(@base["color"]) + " " + Utils.ra(@base["being"]);
            if (a == "Color + Genitive")
                return Utils.ra(@base["color"]) + " " + Utils.ra(@base["genitive"]);
            if (a == "Being + of + Genitive")
                return Utils.ra(@base["being"]) + " of " + Utils.ra(@base["genitive"]);
            if (a == "Being + of the + Genitive")
                return Utils.ra(@base["being"]) + " of the " + Utils.ra(@base["theGenitive"]);
            if (a == "Animal + of + Genitive")
                return Utils.ra(@base["animal"]) + " of " + Utils.ra(@base["genitive"]);
            if (a == "Adjective + Being + of + Genitive")
                return Utils.ra(@base["adjective"]) + " " + Utils.ra(@base["being"]) + " of " + Utils.ra(@base["genitive"]);
            if (a == "Adjective + Animal + of + Genitive")
                return Utils.ra(@base["adjective"]) + " " + Utils.ra(@base["animal"]) + " of " + Utils.ra(@base["genitive"]);
            return string.Empty;
        }

        private string[] getReligionName(string form, string deity, int center)
        {
            var cells = pack.cells;
            string random()
            { return Names.getCulture(cells.culture[center], 0, 0, "", 0); }
            string type()
            { return Utils.rw(types[form]); }
            string supreme()
            { return deity.split(" ,")[0]; }//(级别或地位) 最高的，至高无上的; (程度) 很大的，最大的;
            string place(string adj = null)
            {
                var @base = cells.burg[center] != 0 ? pack.burgs[cells.burg[center]].name : pack.states[cells.state[center]].name;
                if (string.IsNullOrEmpty(@base))
                    return "Null";

                var name = Utils.trimVowels(@base.split(" ,")[0]);
                return !string.IsNullOrEmpty(adj) ? Utils.getAdjective(name) : name;
            }
            string culture()
            { return pack.cultures[cells.culture[center]].name; }

            var m = Utils.rw(methods);
            if (m == "Random + type")
                return new string[] { random() + " " + type(), "global" };
            if (m == "Random + ism")
                return new string[] { Utils.trimVowels(random()) + "ism", "global" };
            if (m == "Supreme + ism" && !string.IsNullOrEmpty(deity))
                return new string[] { Utils.trimVowels(supreme()) + "ism", "global" };
            if (m == "Faith of + Supreme" && !string.IsNullOrEmpty(deity))
                return new string[] { Utils.ra(new string[] { "Faith", "Way", "Path", "Word", "Witnesses" }) + " of " + supreme(), "global" };
            if (m == "Place + ism")
                return new string[] { place() + "ism", "state" };
            if (m == "Culture + ism")
                return new string[] { Utils.trimVowels(culture()) + "ism", "culture" };
            if (m == "Place + ian + type")
                return new string[] { place("adj") + " " + type(), "state" };
            if (m == "Culture + type")
                return new string[] { culture() + " " + type(), "culture" };
            return new string[] { Utils.trimVowels(random()) + "ism", "global" }; // else
        }

        private string getCultName(string form, int center)
        {
            var cells = pack.cells;
            string type()
            { return Utils.rw(types[form]); }
            string random()
            {
                var ret = Names.getCulture(cells.culture[center], 0, 0, "", 0).split(" ,");
                if (ret.Length == 0)
                    return string.Empty;
                return Utils.trimVowels(ret[0]);
            }
            string burg()
            {
                var ret = pack.burgs[cells.burg[center]].name.split(" ,");
                if (ret.Length == 0)
                    return string.Empty;
                return Utils.trimVowels(ret[0]);
            }

            if (0 != cells.burg[center])
                return burg() + "ian " + type();
            if (Random.NextDouble() > .5)
                return random() + "ian " + type();
            return type() + " of the " + generateMeaning();
        }


        public void draw(SKCanvas canvas, Func<IList<SKPoint>, SKPath> curve)
        {
            var cells = pack.cells;
            var vertices = pack.vertices;
            var religions = pack.religions;
            var features = pack.features;
            var n = cells.i.Length;

            var used = new BitArray(n);
            //var vArray = new List<IEnumerable<double[]>>[religions.Count]; // store vertices array
            var body = new string[religions.Count].fill(""); // store path around each religion
            var gap = new string[religions.Count].fill(""); // store path along water for each religion to fill the gaps

            foreach (var i in cells.i)
            {
                if (0 == cells.religion[i])
                    continue;
                if (used[i])
                    continue;
                used[i] = true;
                var r = cells.religion[i];
                var onborder = cells.c[i].filter(ci => cells.religion[ci] != r);
                if (0 == onborder.Count())
                    continue;
                var borderWith = cells.c[i].map(ci => cells.religion[ci]).find(rc => rc != r);
                var vertex = cells.v[i].find(v => vertices.c[v].some(cv => cells.religion[cv] == borderWith));
                var chain = connectVertices(vertex, r, borderWith);
                if (chain.Count < 3)
                    continue;

                var points = chain.map(v => vertices.p[v[0]]);

                //if (vArray[r] == null)
                //    vArray[r] = new List<IEnumerable<double[]>>();
                //vArray[r].push(points);

                body[r] += "M" + points.map(p => p.join(",")).join("L");
                gap[r] += "M" + vertices.p[chain[0][0]].join(",") + chain.reduce((s, v, vi, d) =>
                  0 == vi ? s :
                  0 == v[2] ? s + "L" + vertices.p[v[0]].join(",") :
                  null != d[vi + 1] && 0 == d[vi + 1][2] ? s + "M" + vertices.p[v[0]].join(",") : s
                , "");
            }

            var paint = new SKPaint() { IsAntialias = true };
            paint.Style = SKPaintStyle.Fill;
            body.forEach((p, i) =>
            {
                if (p.Length <= 10)
                    return;

                paint.Color = religions[i].color.ToColor().SK();
                var path = SKPath.ParseSvgPathData(p);
                if (path != null)
                    canvas.DrawPath(path, paint);
            });

            List<int[]> connectVertices(int start, ushort t, int religion)
            {
                var chain = new List<int[]>();

                var land = vertices.c[start].some(c => cells.h[c] >= 20 && cells.religion[c] != t);
                void check(int i)
                { religion = cells.religion[i]; land = cells.h[i] >= 20; }

                for (int i = 0, current = start; i == 0 || current != start && i < 20000; i++)
                {
                    var prev = chain.Count > 0 ? chain.Last()[0] : -1;
                    chain.push(new int[] { current, religion, land ? 1 : 0 }); // add current vertex to sequence
                    var c = vertices.c[current]; // cells adjacent to vertex
                    c.filter(ci => cells.religion[ci] == t).forEach(ci => used[ci] = true);
                    var c0 = c[0] >= n || cells.religion[c[0]] != t;
                    var c1 = c[1] >= n || cells.religion[c[1]] != t;
                    var c2 = c[2] >= n || cells.religion[c[2]] != t;
                    var v = vertices.v[current]; // neighboring vertices
                    if (v[0] != prev && c0 != c1)
                    {
                        current = v[0];
                        check(c0 ? c[0] : c[1]);
                    }
                    else if (v[1] != prev && c1 != c2)
                    {
                        current = v[1];
                        check(c1 ? c[1] : c[2]);
                    }
                    else if (v[2] != prev && c0 != c2)
                    {
                        current = v[2];
                        check(c2 ? c[2] : c[0]);
                    }
                    if (current == chain[chain.Count - 1][0])
                    { Debug.Log("Next vertex is not found"); break; }
                }
                return chain;
            }
        }
    }
}
