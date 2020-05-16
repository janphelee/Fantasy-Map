using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe.Fantasy.Map
{
    using static Utils;
    using static Grid;
    using SkiaSharp;
    using System.Collections;

    internal class Map6BurgsAndStates
    {
        private MapJobs map { get; set; }
        private NamesGenerator Names { get; set; }
        private Map6Routes Routes { get; set; }
        private Grid grid { get; set; }
        private Grid pack { get; set; }

        private int powerInput { get; set; }
        private int regionsInput { get; set; }
        private int manorsInput { get; set; }
        private int neutralInput { get; set; }
        private int statesNeutral { get; set; }

        private int graphWidth { get; set; }
        private int graphHeight { get; set; }

        public Map6BurgsAndStates(MapJobs map)
        {
            this.map = map;
            grid = map.grid;
            pack = map.pack;
            Names = map.Names;
            Routes = map.Routes;

            powerInput = map.Options.PowerInput;
            regionsInput = map.Options.RegionsNumber;
            manorsInput = map.Options.ManorsInput;
            neutralInput = map.Options.NeutralInput;
            statesNeutral = map.Options.StatesNeutral;
            Debug.Log($"BurgsAndStates powerInput:{powerInput} regionsInput:{regionsInput} manorsInput:{manorsInput} neutralInput:{neutralInput} statesNeutral:{statesNeutral}");

            graphWidth = map.Options.Width;
            graphHeight = map.Options.Height;
        }

        private List<int[]> capitalRoutes;
        private List<int[]> townRoutes;
        private List<int[]> oceanRoutes;
        // temporary elevate some lakes to resolve depressions and flux the water to form an open (exorheic) lake
        public void generate()
        {
            var cells = pack.cells;
            var n = cells.i.Length;

            cells.burg = new ushort[n];// cell burg
            cells.road = new ushort[n];// cell road power
            cells.crossroad = new ushort[n];// cell cell crossroad power

            pack.burgs = placeStateCapitals(); // 势力首都
            pack.states = createStates();

            //Debug.SaveArray("pack.burgs.txt", pack.burgs.map(s => s == null ? "" : $"{'{'}'cell':{s.cell},'x':{s.x},'y':{s.y},'state':{s.state},'i':{s.i},'culture':{s.culture},'name':'{s.name}','feature':{s.feature},'capital':{s.capital}{'}'}"));
            //Debug.SaveArray("pack.states.txt", pack.states.map(s => s == null ? "" : $"{'{'}'i':{s.i},'color':'{s.color}','name':'{s.name}','expansionism':{s.expansionism},'capital':{s.capital},'type':'{s.type}','center':{s.center},'culture':{s.culture}{'}'}"));

            var msg = new List<string>();

            var capitalRoutes = Routes.getRoads();
            msg.push($"Routes.getRoads {Random.NextDouble()}");
            capitalRoutes.forEach(rr => msg.push(rr.join(",")));

            placeTowns();
            expandStates();
            normalizeStates();
            //Debug.SaveArray("pack.burgs.txt", pack.burgs.map(s => s == null ? "" : $"{'{'}'cell':{s.cell},'x':{s.x},'y':{s.y},'state':{s.state},'i':{s.i},'culture':{s.culture},'name':'{s.name}','feature':{s.feature},'capital':{s.capital}{'}'}"));
            //Debug.SaveArray("pack.states.txt", pack.states.map(s => s == null ? "" : $"{'{'}'i':{s.i},'color':'{s.color}','name':'{s.name}','expansionism':{s.expansionism},'capital':{s.capital},'type':'{s.type}','center':{s.center},'culture':{s.culture}{'}'}"));

            var townRoutes = Routes.getTrails();
            msg.push($"Routes.getTrails {Random.NextDouble()}");
            townRoutes.forEach(rr => msg.push(rr.join(",")));

            specifyBurgs();

            var oceanRoutes = Routes.getSearoutes();
            msg.push($"Routes.getSearoutes {Random.NextDouble()}");
            oceanRoutes.forEach(rr => msg.push(rr.join(",")));

            collectStatistics();
            msg.push($"collectStatistics {Random.NextDouble()}");
            assignColors();
            msg.push($"assignColors {Random.NextDouble()}");

            generateDiplomacy();
            msg.push($"generateDiplomacy {Random.NextDouble()}");

            //Routes.draw(capitalRoutes, townRoutes, oceanRoutes);
            this.capitalRoutes = capitalRoutes;
            this.townRoutes = townRoutes;
            this.oceanRoutes = oceanRoutes;
            msg.push($"Routes.draw {Random.NextDouble()}");
            //drawBurgs();
            msg.push($"drawBurgs {Random.NextDouble()}");

            Debug.SaveArray("Map6BurgsAndStates.generate.txt", msg);
        }
        private List<Burg> placeStateCapitals()
        {
            var cells = pack.cells;
            var count = regionsInput;

            var burgs = new List<Burg>() { null };

            var score = cells.s.Select(s => (short)(s * Random.NextDouble()))
                .ToArray();// cell score for capitals placement
            var iiii = cells.i.Where(i => score[i] > 0 && cells.culture[i] > 0);
            var sorted = LinqSort(iiii, (a, b) => score[b] - score[a])
                .ToArray();// filtered and sorted array of indexes

            if (sorted.Length < count * 10)
            {
                count = (int)Math.Floor(sorted.Length / 10d);
                if (count == 0)
                {
                    Debug.LogWarning("There is no populated cells. Cannot generate states");
                    return burgs;
                }
                else
                {
                    Debug.Log($"Not enought populated cells ({sorted.Length}). Will generate only {count} states");
                }
            }

            var burgsTree = D3.quadtree();
            var spacing = (graphWidth + graphHeight) / 2d / count; // min distance between capitals

            for (var i = 0; burgs.Count <= count; i++)
            {
                int cell = sorted[i];
                double x = cells.r_points[cell][0], y = cells.r_points[cell][1];

                if (burgsTree.find(x, y, spacing) == null)
                {
                    burgs.Add(new Burg() { cell = cell, x = x, y = y });
                    burgsTree.add(new D3.Quadtree.Value(x, y, cell));
                }

                if (i == sorted.Length - 1)
                {
                    Debug.LogWarning("Cannot place capitals with current spacing. Trying again with reduced spacing");
                    burgsTree = D3.quadtree();
                    i = -1;
                    burgs = new List<Burg>() { null };
                    spacing /= 1.2;
                }
            }

            burgs[0] = new Burg() { tree = burgsTree };
            return burgs;
        }
        // For each capital create a state
        private List<State> createStates()
        {
            var burgs = pack.burgs;
            var cells = pack.cells;
            var cultures = pack.cultures;

            var colors = getColors(burgs.Count - 1);
            var states = new List<State>();
            states.Add(new State { i = 0, name = "Neutrals" });
            //Debug.Log($"createStates.getColors {colors.join(",")}");

            for (ushort i = 0; i < burgs.Count; ++i)
            {
                if (i == 0)
                    continue;// skip first element

                // burgs data
                var b = burgs[i];
                b.i = b.state = i;
                b.culture = cells.culture[b.cell];
                b.name = Names.getCultureShort(b.culture);
                b.feature = cells.f[b.cell];
                b.capital = 1;

                // states data
                var expansionism = rn(Random.NextDouble() * powerInput + 1, 1);
                var basename = b.name.Length < 9 && b.cell % 5 == 0 ? b.name : Names.getCultureShort(b.culture);
                var name = Names.getState(basename, b.culture);
                var nomadic = new int[] { 1, 2, 3, 4 }.includes(cells.biome[b.cell]);
                var type = nomadic ? "Nomadic" : cultures[b.culture].type == "Nomadic" ? "Generic" : cultures[b.culture].type;
                states.Add(new State
                {
                    i = i,
                    color = colors[i - 1],
                    name = name,
                    expansionism = expansionism,
                    capital = i,
                    type = type,
                    center = b.cell,
                    culture = b.culture
                });
                cells.burg[b.cell] = i;
            }
            return states;
        }
        // place secondary settlements based on geo and economical evaluation
        private void placeTowns()
        {
            var cells = pack.cells;
            var burgs = pack.burgs;

            var score = cells.s.Select(s => (short)(s * gauss(1, 3, 0, 20, 3)))
                .ToArray(); // a bit randomized cell score for towns placement
            var iiii = cells.i.Where(i => 0 == cells.burg[i] && score[i] > 0 && cells.culture[i] > 0);
            var sorted = LinqSort(iiii, (a, b) => score[b] - score[a])
                .ToArray();// filtered and sorted array of indexes

            var desiredNumber = manorsInput == 1000 ? rn(sorted.Length / 5.0 / (grid.points.Count / 10000.0).pow(.8)) : manorsInput;
            var burgsNumber = Math.Min(desiredNumber, sorted.Length); // towns to generate
            var burgsAdded = 0;

            var burgsTree = burgs[0].tree;
            var spacing = (graphWidth + graphHeight) / 150d / (burgsNumber.pow(.7) / 66d); // min distance between towns

            while (burgsAdded < burgsNumber && spacing > 1)
            {
                for (var i = 0; burgsAdded < burgsNumber && i < sorted.Length; ++i)
                {
                    if (cells.burg[sorted[i]] > 0)
                        continue;
                    var cell = sorted[i];
                    double x = cells.r_points[cell][0], y = cells.r_points[cell][1];
                    var s = spacing * gauss(1, .3, .2, 2, 2); // randomize to make placement not uniform
                    if (burgsTree.find(x, y, s) != null)
                        continue;// to close to existing burg
                    var burg = (ushort)burgs.Count;
                    var culture = cells.culture[cell];
                    var name = Names.getCulture(culture);
                    burgs.Add(new Burg
                    {
                        cell = cell,
                        x = x,
                        y = y,
                        state = 0,
                        i = burg,
                        culture = culture,
                        name = name,
                        capital = 0,
                        feature = cells.f[cell]
                    });
                    burgsTree.add(new D3.Quadtree.Value(x, y, cell));
                    cells.burg[cell] = burg;
                    burgsAdded++;
                }
                spacing *= .5;
            }

            if (manorsInput != 1000 && burgsAdded < desiredNumber)
            {
                Debug.LogError($"Cannot place all burgs. Requested {desiredNumber}, placed {burgsAdded}");
            }

            burgs[0] = null;
        }

        class Item { public int e, s, b; public double p; public int province, state; }
        // growth algorithm to assign cells to states like we did for cultures
        private void expandStates()
        {
            //var msg = new List<string>();

            var biomesData = map.biomesData;

            var cells = pack.cells;
            var states = pack.states;
            var cultures = pack.cultures;
            var burgs = pack.burgs;

            cells.state = new ushort[cells.i.Length];// cell state
            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            var cost = new Dictionary<int, double>();

            states.filter(s => s.i > 0 && !s.removed).forEach(s =>
            {
                cells.state[burgs[s.capital].cell] = s.i;
                var b = cells.biome[cultures[s.culture].center]; // native biome
                queue.push(new Item { e = s.center, p = 0, s = s.i, b = b });
                cost[s.center] = 1;
                //msg.push($"{s.i} {s.name}");
            });

            var neutral = cells.i.Length / 5000d * 2000 * neutralInput * statesNeutral; // limit cost for state growth

            while (queue.Count > 0)
            {
                var next = queue.pop();
                int n = next.e, s = next.s, b = next.b;
                var p = next.p;
                var type = states[s].type;

                //msg.push($"{n} {p} {s} {b}");
                cells.r_neighbor_r[n].forEach(e =>
                {
                    if (cells.state[e] > 0 && e == states[cells.state[e]].center)
                        return; // do not overwrite capital cells

                    var cultureCost = states[s].culture == cells.culture[e] ? -9 : 700;
                    var biomeCost = getBiomeCost(b, cells.biome[e], type);
                    var heightCost = getHeightCost(pack.features[cells.f[e]], cells.r_height[e], type);
                    var riverCost = getRiverCost(cells.r[e], e, type);
                    var typeCost = getTypeCost(cells.t[e], type);
                    var sum = 10 + cultureCost + biomeCost + heightCost + riverCost + typeCost;
                    var tmp = (decimal)p + (decimal)sum / (decimal)states[s].expansionism;
                    var totalCost = rn((double)tmp, 6);
                    //msg.push($"{e} {cultureCost} {biomeCost} {heightCost} {riverCost} {typeCost} {sum} {totalCost}");
                    //msg.push($"totalCost:{totalCost} neutral:{neutral}");

                    if (totalCost > neutral)
                        return;

                    if (!cost.ContainsKey(e) || totalCost < cost[e])
                    {
                        if (cells.r_height[e] >= 20)
                            cells.state[e] = (ushort)s; // assign state to cell
                        cost[e] = totalCost;
                        queue.push(new Item { e = e, p = totalCost, s = s, b = b });
                        //msg.push($"{e} {totalCost} {s} {b}");
                    }
                });
            }

            //Debug.SaveArray("expandStates.txt", msg);

            burgs.filter(b => null != b && 0 != b.i && !b.removed).forEach(b => b.state = cells.state[b.cell]); // assign state to burgs

            double getBiomeCost(int b, int biome, string type)
            {
                if (b == biome)
                    return 10;// tiny penalty for native biome
                if (type == "Hunting")
                    return biomesData.cost[biome] * 2; // non-native biome penalty for hunters
                if (type == "Nomadic" && biome > 4 && biome < 10)
                    return biomesData.cost[biome] * 3; // forest biome penalty for nomads
                return biomesData.cost[biome]; // general non-native biome penalty
            }
            double getHeightCost(Feature f, int h, string type)
            {
                if (type == "Lake" && f.type == "lake")
                    return 10; // low lake crossing penalty for Lake cultures
                if (type == "Naval" && h < 20)
                    return 300; // low sea crossing penalty for Navals
                if (type == "Nomadic" && h < 20)
                    return 10000; // giant sea crossing penalty for Nomads
                if (h < 20)
                    return 1000; // general sea crossing penalty
                if (type == "Highland" && h < 62)
                    return 1100; // penalty for highlanders on lowlands
                if (type == "Highland")
                    return 0; // no penalty for highlanders on highlands
                if (h >= 67)
                    return 2200; // general mountains crossing penalty
                if (h >= 44)
                    return 300; // general hills crossing penalty
                return 0;
            }
            double getRiverCost(int r, int i, string type)
            {
                if (type == "River")
                    return r > 0 ? 0 : 100; // penalty for river cultures
                if (0 == r)
                    return 0; // no penalty for others if there is no river
                return Math.Min(Math.Max(cells.fl[i] / 10d, 20), 100); // river penalty from 20 to 100 based on flux
            }
            double getTypeCost(int t, string type)
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
        private void normalizeStates()
        {
            var cells = pack.cells;
            var burgs = pack.burgs;

            foreach (var i in cells.i)
            {
                if (cells.r_height[i] < 20 || cells.burg[i] > 0)
                    continue; // do not overwrite burgs
                if (cells.r_neighbor_r[i].some(c => cells.burg[c] > 0 && burgs[cells.burg[c]].capital > 0))
                    continue; // do not overwrite near capital

                var neibs = cells.r_neighbor_r[i].Where(c => cells.r_height[c] >= 20);
                var adversaries = neibs.Where(c => cells.state[c] != cells.state[i]).ToArray();
                if (adversaries.Length < 2)
                    continue;
                var buddies = neibs.Where(c => cells.state[c] == cells.state[i]).ToArray();
                if (buddies.Length > 2)
                    continue;
                if (adversaries.Length <= buddies.Length)
                    continue;
                cells.state[i] = cells.state[adversaries[0]];
            }
        }
        // define burg coordinates and define details
        private void specifyBurgs()
        {
            var cells = pack.cells;
            var vertices = pack.vertices;

            foreach (var b in pack.burgs)
            {
                if (null == b || 0 == b.i)
                    continue;
                var i = b.cell;

                // asign port status: capital with any harbor and towns with good harbors
                var port = (0 != b.capital && 0 != cells.harbor[i]) || cells.harbor[i] == 1;
                b.port = (ushort)(port ? cells.f[cells.haven[i]] : 0); // port is defined by feature id it lays on

                // define burg population (keep urbanization at about 10% rate)
                b.population = rn(Math.Max((cells.s[i] + cells.road[i]) / 8d + b.i / 1000d + i % 100 / 1000d, .1), 3);
                if (0 != b.capital)
                    b.population = rn(b.population * 1.3, 3); // increase capital population

                if (port)
                {
                    b.population = b.population * 1.3; // increase port population
                    var e = cells.v[i]
                        .filter(v => vertices.c[v].some(c => c == cells.haven[i]))
                        .ToArray(); // vertices of common edge
                    b.x = rn((vertices.p[e[0]][0] + vertices.p[e[1]][0]) / 2, 2);
                    b.y = rn((vertices.p[e[0]][1] + vertices.p[e[1]][1]) / 2, 2);
                }

                // add random factor
                b.population = rn(b.population * gauss(2, 3, .6, 20, 3), 3);

                // shift burgs on rivers semi-randomly and just a bit
                if (!port && 0 != cells.r[i])
                {
                    var shift = Math.Min(cells.fl[i] / 150d, 1);
                    if (0 != (i % 2))
                        b.x = rn(b.x + shift, 2);
                    else
                        b.x = rn(b.x - shift, 2);
                    if (0 != (cells.r[i] % 2))
                        b.y = rn(b.y + shift, 2);
                    else
                        b.y = rn(b.y - shift, 2);
                }
            }
        }

        private void collectStatistics()
        {
            var cells = pack.cells;
            var states = pack.states;
            var neighbors = new HashSet<ushort>[states.Count];
            states.forEach((s, i) =>
            {
                s.cells = s.area = s.burgs = 0;
                s.rural = s.urban = 0;
                neighbors[i] = new HashSet<ushort>();
            });

            foreach (var i in cells.i)
            {
                if (cells.h[i] < 20)
                    continue;
                var s = cells.state[i];

                // check for neighboring states
                cells.c[i].filter(c => cells.h[c] >= 20 && cells.state[c] != s).forEach(c => neighbors[s].Add(cells.state[c]));

                // collect stats
                states[s].cells += 1;
                states[s].area += cells.area[i];
                states[s].rural += cells.pop[i];
                if (0 != cells.burg[i])
                {
                    states[s].urban += pack.burgs[cells.burg[i]].population;
                    states[s].burgs++;
                }
            }
            // convert neighbors Set object into array
            states.forEach((s, i) => s.neighbors = neighbors[i].ToArray());
        }
        private void assignColors()
        {
            var colors = new List<string> { "#66c2a5", "#fc8d62", "#8da0cb", "#e78ac3", "#a6d854", "#ffd92f" }; // d3.schemeSet2;

            // assin basic color using greedy coloring algorithm
            pack.states.forEach(s =>
            {
                if (0 == s.i || s.removed)
                    return;
                var neibs = s.neighbors;
                s.color = colors.find(c => neibs.every(n => pack.states[n].color != c));
                if (not(s.color))
                    s.color = getRandomColor();
                colors.push(colors.shift());
            });

            // randomize each already used color a bit
            colors.forEach(c =>
            {
                var sameColored = pack.states.filter(s => s.color == c);
                sameColored.forEach((s, d) =>
                {
                    if (0 == d)
                        return;
                    s.color = getMixedColor(s.color);
                });
            });
        }
        // generate Diplomatic Relationships
        private void generateDiplomacy()
        {
            var cells = pack.cells;
            var states = pack.states;
            var chronicle = pack.chronicle = new List<string[]>();
            var valid = states.filter(s => s.i != 0 && !s.removed);

            var neibs = new JObject {
                { "Ally", 1 },
                { "Friendly", 2 },
                { "Neutral", 1 },
                { "Suspicion", 10 },
                { "Rival", 9 }
            }.dict(); // relations to neighbors
            var neibsOfNeibs = new JObject {
                { "Ally", 10 },
                { "Friendly",8 },
                { "Neutral",5 },
                { "Suspicion",1 }
            }.dict(); // relations to neighbors of neighbors
            var far = new JObject {
                { "Friendly", 1 },
                { "Neutral", 12 },
                { "Suspicion",2 },
                { "Unknown",6 }
            }.dict(); // relations to other
            var navals = new JObject {
                { "Neutral",1 },
                { "Suspicion",2 },
                { "Rival",1 },
                { "Unknown",1 }
            }.dict(); // relations of naval powers

            // clear all relationships
            valid.forEach(s => s.diplomacy = new string[states.Count].fill("x"));
            if (valid.Count() < 2)
                return; // no states to renerate relations with
            var areaMean = D3.mean(valid.map(s => (int)s.area)); // avarage state area

            //var msg = new List<string>();
            // generic relations
            for (var f = 1; f < states.Count; f++)
            {
                if (states[f].removed)
                    continue;

                if (states[f].diplomacy.includes("Vassal"))
                {
                    // Vassals copy relations from their Suzerains
                    var suzerain = states[f].diplomacy.indexOf("Vassal");

                    for (var i = 1; i < states.Count; i++)
                    {
                        if (i == f || i == suzerain)
                            continue;
                        states[f].diplomacy[i] = states[suzerain].diplomacy[i];
                        if (states[suzerain].diplomacy[i] == "Suzerain")
                            states[f].diplomacy[i] = "Ally";
                        for (var e = 1; e < states.Count; e++)
                        {
                            if (e == f || e == suzerain)
                                continue;
                            if (states[e].diplomacy[suzerain] == "Suzerain" || states[e].diplomacy[suzerain] == "Vassal")
                                continue;
                            states[e].diplomacy[f] = states[e].diplomacy[suzerain];
                        }
                    }
                    continue;
                }

                for (var t = f + 1; t < states.Count; t++)
                {
                    if (states[t].removed)
                        continue;

                    if (states[t].diplomacy.includes("Vassal"))
                    {
                        var suzerain = states[t].diplomacy.indexOf("Vassal");
                        states[f].diplomacy[t] = states[f].diplomacy[suzerain];
                        continue;
                    };

                    var naval = states[f].type == "Naval" && states[t].type == "Naval" && cells.f[states[f].center] != cells.f[states[t].center];
                    var neib = naval ? false : states[f].neighbors.includes((ushort)t);
                    var neibOfNeib = naval || neib ? false : states[f].neighbors.some(n => states[n].neighbors.includes((ushort)t));

                    var status = naval ? rw(navals) :
                        neib ? rw(neibs) :
                        neibOfNeib ? rw(neibsOfNeibs) : rw(far);

                    // add Vassal
                    if (neib && P(.8) && states[f].area > areaMean && states[t].area < areaMean && (float)states[f].area / states[t].area > 2)
                        status = "Vassal";
                    states[f].diplomacy[t] = status == "Vassal" ? "Suzerain" : status;
                    states[t].diplomacy[f] = status;
                    //msg.push($"{f} {t} {status} {naval} {neib} {neibOfNeib} {states[f].area} {states[t].area} {(float)states[f].area / states[t].area > 2}");
                }
            }

            // declare wars
            for (var attacker = 1; attacker < states.Count; attacker++)
            {
                var ad = states[attacker].diplomacy; // attacker relations;
                if (states[attacker].removed)
                    continue;
                if (!ad.includes("Rival"))
                    continue; // no rivals to attack
                if (ad.includes("Vassal"))
                    continue; // not independent
                if (ad.includes("Enemy"))
                    continue; // already at war

                // random independent rival
                var defender = ra(
                    ad.map((r, d) => r == "Rival" && !states[d].diplomacy.includes("Vassal") ? d : 0)
                    .filter(d => d != 0).ToArray()
                    );
                double ap = states[attacker].area * states[attacker].expansionism, dp = states[defender].area * states[defender].expansionism;
                if (ap < dp * gauss(1.6, .8, 0, 10, 2))
                    continue; // defender is too strong
                string an = states[attacker].name, dn = states[defender].name; // names
                var attackers = new List<int> { attacker };
                var defenders = new List<int> { defender }; // attackers and defenders array
                var dd = states[defender].diplomacy; // defender relations;

                // start a war
                var war = new List<string> {
                    $"{an}-{ trimVowels(dn)}ian War",
                    $"{an} declared a war on its rival {dn}"
                };

                // attacker vassals join the war
                ad.forEach((r, d) =>
                {
                    if (r == "Suzerain")
                    {
                        attackers.push(d);
                        war.push($"{an}'s vassal {states[d].name} joined the war on attackers side");
                    }
                });

                // defender vassals join the war
                dd.forEach((r, d) =>
                {
                    if (r == "Suzerain")
                    {
                        defenders.push(d);
                        war.push($"{dn}'s vassal {states[d].name} joined the war on defenders side");
                    }
                });

                ap = D3.sum(attackers.map(a => states[a].area * states[a].expansionism)); // attackers joined power
                dp = D3.sum(defenders.map(d => states[d].area * states[d].expansionism)); // defender joined power

                // defender allies join
                dd.forEach((r, d) =>
                {
                    if (r != "Ally" || states[d].diplomacy.includes("Vassal"))
                        return;
                    if (states[d].diplomacy[attacker] != "Rival" && ap / dp > (2 * gauss(1.6, .8, 0, 10, 2)))
                    {
                        var reason = states[d].diplomacy.includes("Enemy") ? $"Being already at war," : $"Frightened by {an},";
                        war.push($"{reason} {states[d].name} severed the defense pact with {dn}");
                        dd[d] = states[d].diplomacy[defender] = "Suspicion";
                        return;
                    }
                    defenders.push(d);
                    dp += states[d].area * states[d].expansionism;
                    war.push($"{dn}'s ally {states[d].name} joined the war on defenders side");

                    // ally vassals join
                    states[d].diplomacy.map((r2, d2) => r2 == "Suzerain" ? d2 : 0).filter(d2 => d2 != 0).forEach(v =>
                    {
                        defenders.push(v);
                        dp += states[v].area * states[v].expansionism;
                        war.push($"{states[d].name}'s vassal {states[v].name} joined the war on defenders side");
                    });
                });

                // attacker allies join if the defender is their rival or joined power > defenders power and defender is not an ally
                ad.forEach((r, d) =>
                {
                    if (r != "Ally" || states[d].diplomacy.includes("Vassal") || defenders.includes(d))
                        return;

                    var name = states[d].name;
                    if (states[d].diplomacy[defender] != "Rival" && (P(.2) || ap <= dp * 1.2))
                    { war.push($"{an}'s ally {name} avoided entering the war"); return; }

                    var allies = states[d].diplomacy.map((r2, d2) => r2 == "Ally" ? d2 : 0).filter(d2 => d2 != 0);
                    if (allies.some(ally => defenders.includes(ally)))
                    { war.push($"{an}'s ally {name} did not join the war as its allies are in war on both sides"); return; };

                    attackers.push(d);
                    ap += states[d].area * states[d].expansionism;
                    war.push($"{an}'s ally {name} joined the war on attackers side");

                    // ally vassals join
                    states[d].diplomacy.map((r2, d2) => r2 == "Suzerain" ? d2 : 0).filter(d2 => d2 != 0).forEach(v =>
                    {
                        attackers.push(v);
                        dp += states[v].area * states[v].expansionism;
                        war.push($"{states[d].name}'s vassal {states[v].name} joined the war on attackers side");
                    });
                });

                // change relations to Enemy for all participants
                attackers.forEach(a => defenders.forEach(d => states[a].diplomacy[d] = states[d].diplomacy[a] = "Enemy"));
                chronicle.push(war.ToArray()); // add a record to diplomatical history
                //msg.push(war.join("\n"));
            }

            //Debug.SaveArray("generateDiplomacy.txt", msg);
        }

        // select a forms for listed or all valid states
        public void defineStateForms(List<int> list = null)
        {
            var states = pack.states.filter(s => 0 != s.i && !s.removed);
            if (states.Count() < 1)
                return;

            var generic = JObject.Parse(@"{ Monarchy:25, Republic: 2, Union: 1}").dict();
            var naval = JObject.Parse(@"{ Monarchy:25, Republic:8, Union:3 }").dict();
            var genericArray = ww(generic);
            var navalArray = ww(naval);

            var median = D3.median(pack.states.map(s => s.area));
            var empireMin = states.map(s => s.area).sort((a, b) => b - a).ToArray()[Math.Max((int)Math.Ceiling(states.Count().pow(.4)) - 2, 0)];
            var expTiers = pack.states.map(s =>
            {
                var tier = Math.Min((int)Math.Floor(s.area / median * 2.6), 4);
                if (tier == 4 && s.area < empireMin)
                    tier = 3;
                return tier;
            }).ToArray();

            var monarchy = JArray.Parse(@"['Duchy', 'Grand Duchy', 'Principality', 'Kingdom', 'Empire']"); // per expansionism tier
            var republic = JObject.Parse(@"{ Republic:70, Federation: 2, Oligarchy: 2, Tetrarchy: 1, Triumvirate: 1, Diarchy: 1, 'Trade Company':3}").dict(); // weighted random
            var union = JObject.Parse(@"{ Union:3, League:4, Confederation:1, 'United Kingdom':1, 'United Republic':1, 'United Provinces':2, Commonwealth:1, Heptarchy:1 }").dict(); // weighted random

            //var msg = new List<string>();
            foreach (var s in states)
            {
                if (null != list && !list.includes(s.i))
                    continue;

                //msg.push($"{s.type} {expTiers[s.i]} {Random.NextDouble()}");
                // some nomadic states
                if (s.type == "Nomadic" && P(.8))
                {
                    s.form = "Horde";
                    s.formName = expTiers[s.i] > 2 ? "United Hordes" : "Horde";
                    s.fullName = getFullName(s);
                    continue;
                }

                var religion = pack.cells.religion[s.center];
                var theocracy = 0 != religion && pack.religions[religion].expansion == "state" || (P(.1) && pack.religions[religion].type == "Organized");
                s.form = theocracy ? "Theocracy" : s.type == "Naval" ? ra(navalArray) : ra(genericArray);
                s.formName = selectForm(s);
                s.fullName = getFullName(s);

                //msg.push($"{s.i} name:{s.name} form:{s.form} {s.formName} full:{s.fullName}");
            }
            //Debug.SaveArray("defineStateForms.txt", msg);

            string selectForm(State s)
            {
                var @base = pack.cultures[s.culture].@base;

                if (s.form == "Monarchy")//君主政体, 君主国, 君主政治
                {
                    var form = monarchy[expTiers[s.i]].Value<string>();
                    // Default name depends on exponent tier, some culture bases have special names for tiers
                    if (s.diplomacy != null)
                    {
                        if (form == "Duchy" && s.neighbors.Length > 1 && rand(6) < s.neighbors.Length && s.diplomacy.includes("Vassal"))
                            return "Marches"; // some vassal dutchies on borderland
                        if (P(.3) && s.diplomacy.includes("Vassal"))
                            return "Protectorate"; // some vassals
                    }

                    if (@base == 16 && (form == "Empire" || form == "Kingdom"))
                        return "Sultanate"; // Turkic
                    if (@base == 5 && (form == "Empire" || form == "Kingdom"))
                        return "Tsardom"; // Ruthenian
                    if (@base == 31 && (form == "Empire" || form == "Kingdom"))
                        return "Khaganate"; // Mongolian
                    if (@base == 12 && (form == "Kingdom" || form == "Grand Duchy"))
                        return "Shogunate"; // Japanese
                    if (new int[] { 18, 17 }.includes(@base) && form == "Empire")
                        return "Caliphate"; // Arabic, Berber
                    if (@base == 18 && (form == "Grand Duchy" || form == "Duchy"))
                        return "Emirate"; // Arabic
                    if (@base == 7 && (form == "Grand Duchy" || form == "Duchy"))
                        return "Despotate"; // Greek
                    if (@base == 31 && (form == "Grand Duchy" || form == "Duchy"))
                        return "Ulus"; // Mongolian
                    if (@base == 16 && (form == "Grand Duchy" || form == "Duchy"))
                        return "Beylik"; // Turkic
                    if (@base == 24 && (form == "Grand Duchy" || form == "Duchy"))
                        return "Satrapy"; // Iranian
                    return form;
                }

                if (s.form == "Republic")//共和国, 共和政体, 团体, 界
                {
                    // Default name is from weighted array, special case for small states with only 1 burg
                    if (expTiers[s.i] < 2 && s.burgs == 1)
                    {
                        if (trimVowels(s.name) == trimVowels(pack.burgs[s.capital].name))
                        {
                            s.name = pack.burgs[s.capital].name;
                            return "Free City";
                        }
                        if (P(.3))
                            return "City-state";
                    }
                    return rw(republic);
                }

                if (s.form == "Union")//联盟, 联合, 结合, 工会
                    return rw(union);

                if (s.form == "Theocracy")//神权政治, 神权国
                {
                    // default name is "Theocracy", some culture bases have special names
                    if (new int[] { 0, 1, 2, 3, 4, 6, 8, 9, 13, 15, 20 }.includes(@base))
                        return "Diocese"; // Euporean
                    if (new int[] { 7, 5 }.includes(@base))
                        return "Eparchy"; // Greek, Ruthenian
                    if (new int[] { 21, 16 }.includes(@base))
                        return "Imamah"; // Nigerian, Turkish
                    if (new int[] { 18, 17, 28 }.includes(@base))
                        return "Caliphate"; // Arabic, Berber, Swahili
                    return "Theocracy";
                }
                return "";
            }
        }

        public void defineBurgFeatures()
        {
            pack.burgs.filter(b => null != b && 0 != b.i && !b.removed).forEach(b =>
            {
                var pop = b.population;
                b.citadel = 0 != b.capital || pop > 50 && P(.75) || P(.5) ? (byte)1 : (byte)0;
                b.plaza = pop > 50 || pop > 30 && P(.75) || pop > 10 && P(.5) || P(.25) ? (byte)1 : (byte)0;
                b.walls = 0 != b.capital || pop > 30 || pop > 20 && P(.75) || pop > 10 && P(.5) || P(.2) ? (byte)1 : (byte)0;
                b.shanty = pop > 30 || pop > 20 && P(.75) || 0 != b.walls && P(.75) ? (byte)1 : (byte)0;
                var religion = pack.cells.religion[b.cell];
                var theocracy = pack.states[b.state].form == "Theocracy";
                b.temple = 0 != religion && theocracy || pop > 50 || pop > 35 && P(.75) || pop > 20 && P(.5) ? (byte)1 : (byte)0;
            });
        }

        private string getFullName(State s)
        {
            if (not(s.formName))
                return s.name;
            if (not(s.name) && @is(s.formName))
                return "The " + s.formName;
            // state forms requiring Adjective + Name, all other forms use scheme Form + Of + Name
            var adj = new string[] { "Empire", "Sultanate", "Khaganate", "Shogunate", "Caliphate", "Despotate", "Theocracy", "Oligarchy", "Union", "Confederation", "Trade Company", "League", "Tetrarchy", "Triumvirate", "Diarchy", "Horde" };
            return adj.includes(s.formName) ? getAdjective(s.name) + " " + s.formName : s.formName + " of " + s.name;
        }

        public void generateProvinces(bool regenerate = false)
        {
            //var msg = new List<string>();

            var localSeed = regenerate ? (int)Math.Floor(Random.NextDouble() * 1e9) : map.Options.MapSeed;
            Random.Seed(localSeed);
            //msg.push($"localSeed:{localSeed} ProvincesInput:{map.Options.ProvincesInput} {Random.NextDouble()}");

            var cells = pack.cells;
            var states = pack.states;
            var burgs = pack.burgs;
            var provinces = pack.provinces = new List<Province>() { null };
            cells.province = new ushort[cells.i.Length]; // cell state
            var percentage = map.Options.ProvincesInput;
            if (states.Count < 2 || 0 == percentage)
            { states.forEach(s => s.provinces = new List<ushort>()); return; } // no provinces
            var max = percentage == 100 ? 1000 : gauss(20, 5, 5, 100) * percentage.pow(.5); // max growth
            //msg.push($"{max} {Random.NextDouble()}");

            var forms = JObject.Parse(@"{
              Monarchy:{County:11, Earldom:3, Shire:1, Landgrave:1, Margrave:1, Barony:1},
              Republic:{Province:6, Department:2, Governorate:2, State:1, Canton:1, Prefecture:1},
              Theocracy:{Parish:5, Deanery:3, Province:2, Council:1, District:1},
              Union:{Province:2, State:1, Canton:1, Republic:1, County:1},
              Wild:{Territory:10, Land:5, Province:2, Region:2, Tribe:1, Clan:1},
              Horde:{Horde:1}
            }");

            // generate provinces for a selected burgs
            states.forEach(s =>
            {
                s.provinces = new List<ushort>();
                if (0 == s.i || s.removed)
                    return;
                var startBurgs = burgs.filter(b => b != null && b.state == s.i && !b.removed);

                Burg[] stateBurgs;
                try
                {
                    stateBurgs = startBurgs.ToArray().SortTim(
                        (a, b) => (b.population * gauss(1, .2, .5, 1.5, 3)).CompareTo(a.population)
                    );
                }
                catch
                {
                    stateBurgs = new Burg[0];
                }

                if (stateBurgs.Length < 2)
                    return; // at least 2 provinces are required
                var provincesNumber = Math.Max(Math.Ceiling(stateBurgs.Length * percentage / 100d), 2);
                var form = forms[s.form].dict<int>();

                //msg.push($"{s.i} {startBurgs.Count()} {provincesNumber} {Random.NextDouble()}");
                for (var i = 0; i < provincesNumber; i++)
                {
                    var province = (ushort)provinces.Count;
                    s.provinces.push(province);
                    var center = stateBurgs[i].cell;
                    var burg = stateBurgs[i].i;
                    var c = stateBurgs[i].culture;
                    var name = P(.5) ? Names.getState(Names.getCultureShort(c), c) : stateBurgs[i].name;
                    var formName = rw(form);
                    form[formName] += 5;
                    var fullName = name + " " + formName;
                    var color = getMixedColor(s.color);

                    provinces.push(new Province
                    {
                        i = province,
                        state = s.i,
                        center = center,
                        burg = burg,
                        name = name,
                        formName = formName,
                        fullName = fullName,
                        color = color
                    });

                    //msg.push($"{s.color} {color}");
                    //msg.push($"{i} {center} {burg} {c} name:{name} formName:{formName} {form[formName]}");
                }
            });

            // expand generated provinces
            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            var cost = new Dictionary<int, double>();
            provinces.forEach(p =>
            {
                if ((p == null || 0 == p.i) || p.removed)
                    return;
                cells.province[p.center] = (ushort)p.i;
                queue.push(new Item { e = p.center, p = 0, province = p.i, state = p.state });
                cost[p.center] = 1;
                //debug.append("circle").attr("cx", cells.p[p.center][0]).attr("cy", cells.p[p.center][1]).attr("r", .3).attr("fill", "red");
            });

            while (queue.Count > 0)
            {
                var next = queue.pop();
                int n = next.e, province = next.province, state = next.state;
                var p = next.p;
                cells.c[n].forEach(e =>
                {
                    var land = cells.h[e] >= 20;
                    if (!land && 0 == cells.t[e])
                        return; // cannot pass deep ocean
                    if (land && cells.state[e] != state)
                        return;
                    var evevation = cells.h[e] >= 70 ? 100 : cells.h[e] >= 50 ? 30 : cells.h[e] >= 20 ? 10 : 100;
                    var totalCost = p + evevation;

                    if (totalCost > max)
                        return;
                    if (!cost.ContainsKey(e) || totalCost < cost[e])
                    {
                        if (land)
                            cells.province[e] = (ushort)province; // assign province to a cell
                        cost[e] = totalCost;
                        queue.push(new Item { e = e, p = totalCost, province = province, state = state });
                    }
                });
            }

            // justify provinces shapes a bit
            foreach (var i in cells.i)
            {
                if (0 != cells.burg[i])
                    continue;// do not overwrite burgs
                var neibs = cells.c[i].filter(c => cells.state[c] == cells.state[i]).map(c => cells.province[c]);
                var adversaries = neibs.filter(c => c != cells.province[i]).ToArray();
                if (adversaries.Length < 2)
                    continue;
                var buddies = neibs.filter(c => c == cells.province[i]).Count();
                if (buddies > 2)
                    continue;
                var competitors = adversaries.map(p => adversaries.reduce((s, v) => v == p ? ++s : s, (ushort)0)).ToArray();
                var maxc = D3.max(competitors);
                if (buddies >= maxc)
                    continue;
                cells.province[i] = adversaries[competitors.indexOf(maxc)];
            }

            // add "wild" provinces if some cells don't have a province assigned
            var noProvince = cells.i.filter(i => 0 != cells.state[i] && 0 == cells.province[i]); // cells without province assigned
            states.forEach(s =>
            {
                if (0 == s.provinces.Count)
                    return;
                var stateNoProvince = noProvince.filter(i => cells.state[i] == s.i && 0 == cells.province[i])
                    // IEnumerable不能多次使用，所以转成数组
                    .ToArray();
                //msg.push($"stateNoProvince:{stateNoProvince.join()}");
                while (stateNoProvince.Count() > 0)
                {
                    // add new province
                    var province = (ushort)provinces.Count;
                    int burgCell;
                    try
                    { burgCell = stateNoProvince.find(i => 0 != cells.burg[i]); }
                    catch
                    { burgCell = 0; }

                    var center = 0 != burgCell ? burgCell : stateNoProvince.First();
                    var burg = 0 != burgCell ? cells.burg[burgCell] : 0;
                    cells.province[center] = province;

                    // expand province
                    cost = new Dictionary<int, double>();
                    cost[center] = 1;
                    queue.push(new Item { e = center, p = 0 });
                    while (queue.Count > 0)
                    {
                        var next = queue.pop();
                        var n = next.e;
                        var p = next.p;

                        //msg.push($"queue.pop {center} {province} n:{n} p:{p} {queue.Count}");
                        cells.c[n].forEach(e =>
                        {
                            if (0 != cells.province[e])
                                return;
                            var land = cells.h[e] >= 20;
                            if (0 != cells.state[e] && cells.state[e] != s.i)
                                return;
                            var ter = land ? cells.state[e] == s.i ? 3 : 20 : 0 != cells.t[e] ? 10 : 30;
                            var totalCost = p + ter;

                            if (totalCost > max)
                                return;
                            if (!cost.ContainsKey(e) || totalCost < cost[e])
                            {
                                //msg.push($"land && cells.state[e] == s.i {land} {cells.state[e]} {s.i}");
                                if (land && cells.state[e] == s.i)
                                    cells.province[e] = province; // assign province to a cell
                                cost[e] = totalCost;
                                queue.push(new Item { e = e, p = totalCost });
                                //msg.push($"queue.push e:{e} p:{totalCost} {queue.Count}");
                            }
                        });
                    }

                    // generate "wild" province name
                    var c = cells.culture[center];
                    var name = 0 != burgCell && P(.5) ? burgs[burg].name : Names.getState(Names.getCultureShort(c), c);
                    var f = pack.features[cells.f[center]];
                    var provCells = stateNoProvince.filter(i => cells.province[i] == province);
                    //if (119 == province)
                    //{
                    //msg.push(stateNoProvince.join());
                    //msg.push(stateNoProvince.map(i => cells.province[i]).join());
                    //}
                    var singleIsle = provCells.Count() == f.cells && !provCells.finded(i => cells.f[i] != f.i);
                    var isleGroup = !singleIsle && !provCells.finded(i => pack.features[cells.f[i]].group != "isle");
                    var colony = !singleIsle && !isleGroup && P(.5) && !isPassable(s.center, center);
                    var formName = singleIsle ? "Island" : isleGroup ? "Islands" : colony ? "Colony" : rw(forms["Wild"]);
                    var fullName = name + " " + formName;
                    var color = getMixedColor(s.color);
                    provinces.push(new Province
                    {
                        i = province,
                        state = s.i,
                        center = center,
                        burg = burg,
                        name = name,
                        formName = formName,
                        fullName = fullName,
                        color = color
                    });
                    s.provinces.push(province);

                    //msg.push($"provCells:{provCells.join()} {singleIsle} {isleGroup} {colony} {fullName}");
                    //msg.push($"{s.color} {color}");
                    //msg.push($"{province} {center} {burg} {c} name:{name} formName:{formName}");

                    // check if there is a land way within the same state between two cells
                    bool isPassable(int from, int to)
                    {
                        if (cells.f[from] != cells.f[to])
                            return false; // on different islands
                        var lstTmp = new List<int>() { from };
                        var used = new byte[cells.i.Length];
                        var state = cells.state[from];
                        while (lstTmp.Count > 0)
                        {
                            var current = lstTmp.pop();
                            if (current == to)
                                return true; // way is found
                            cells.c[current].forEach(cTmp =>
                            {
                                if (0 != used[cTmp] || cells.h[cTmp] < 20 || cells.state[cTmp] != state)
                                    return;
                                lstTmp.push(cTmp);
                                used[cTmp] = 1;
                            });
                        }
                        return false;// way is not found
                    }

                    // re-check
                    stateNoProvince = noProvince.filter(i => cells.state[i] == s.i && 0 == cells.province[i])
                        .ToArray();
                    //msg.push($"re-check stateNoProvince:{stateNoProvince.join()}");
                }
            });

            //provinces.forEach(p =>
            //{
            //    if (p != null)
            //        msg.push($"provinces {p.i} {p.state} {p.center} {p.burg} {p.name} {p.formName} {p.fullName} {p.color}");
            //});

            //Debug.SaveArray("generateProvinces.txt", msg);
        }


        private string[] stateSvgs { get; set; }
        private List<KeyValuePair<int, IEnumerable<double[]>>> labelSvgs { get; set; }

        internal void generateStatesPath()
        {
            var cells = pack.cells;
            var vertices = pack.vertices;
            var states = pack.states;
            var n = cells.i.Length;

            var used = new BitArray(n);
            var vArray = new List<IEnumerable<double[]>>[states.Count]; // store vertices array
            var body = new string[states.Count].fill(""); // store path around each state
            var gap = new string[states.Count].fill(""); // store path along water for each state to fill the gaps

            foreach (var i in cells.i)
            {
                if (0 == cells.state[i] || used[i])
                    continue;

                var s = cells.state[i];
                var onborder = cells.c[i].filter(ci => cells.state[ci] != s);
                if (0 == onborder.Count())
                    continue;
                var borderWith = cells.c[i].map(ci => cells.state[ci]).find(rc => rc != s);
                var vertex = cells.v[i].find(v => vertices.c[v].some(cv => cells.state[cv] == borderWith));
                var chain = map.connectVertices(vertex, s, borderWith, cells.state, used);
                if (chain.Count < 3)
                    continue;

                var points = chain.map(v => vertices.p[v[0]]);

                if (vArray[s] == null)
                    vArray[s] = new List<IEnumerable<double[]>>();
                vArray[s].push(points);

                body[s] += "M" + points.map(p => p.join(",")).join("L");
                gap[s] += "M" + vertices.p[chain[0][0]].join(",") + chain.reduce((str, v, vi, d) =>
                  0 == vi ? str :
                  0 == v[2] ? str + "L" + vertices.p[v[0]].join(",") :
                  null != d[vi + 1] && 0 == d[vi + 1][2] ? str + "M" + vertices.p[v[0]].join(",") : str
                , "");
            }

            //var msg = new List<string>();
            // find state visual center
            vArray.forEach((ar, i) =>
            {
                if (ar == null)
                    return;
                var sorted = ar.sort((a, b) => b.Count() - a.Count()); // sort by points number
                var polygon = sorted.map(p => p.ToArray()).First();//取第一个最大区域
                //polygon.map(p => p.join(",")).forEach((p, k) => msg.push($"{i} {k} {p}"));

                states[i].pole = polygon.polylabel(1.0); // pole of inaccessibility
                //msg.push($"{states[i].pole.join(",")}");
            });

            stateSvgs = body;
        }

        private string[] provinceSvgs { get; set; }
        internal void generateProvincesPath()
        {
            var cells = pack.cells;
            var vertices = pack.vertices;
            var provinces = pack.provinces;
            var n = cells.i.Length;

            var used = new BitArray(n);
            var vArray = new List<IEnumerable<double[]>>[provinces.Count]; // store vertices array
            var body = new string[provinces.Count].fill(""); // store path around each state
            var gap = new string[provinces.Count].fill(""); // store path along water for each state to fill the gaps

            foreach (var i in cells.i)
            {
                if (0 == cells.province[i] || used[i])
                    continue;

                var p = cells.province[i];
                var onborder = cells.c[i].filter(ci => cells.province[ci] != p);
                if (0 == onborder.Count())
                    continue;
                var borderWith = cells.c[i].map(n2 => cells.province[n2]).find(n2 => n2 != p);
                var vertex = cells.v[i].find(v => vertices.c[v].some(n2 => cells.province[n2] == borderWith));
                var chain = map.connectVertices(vertex, p, borderWith, cells.province, used);
                if (chain.Count < 3)
                    continue;

                var points = chain.map(v => vertices.p[v[0]]);

                if (vArray[p] == null)
                    vArray[p] = new List<IEnumerable<double[]>>();
                vArray[p].push(points);

                body[p] += "M" + points.map(pt => pt.join(",")).join("L");
                gap[p] += "M" + vertices.p[chain[0][0]].join(",") + chain.reduce((str, v, vi, d) =>
                  0 == vi ? str :
                  0 == v[2] ? str + "L" + vertices.p[v[0]].join(",") :
                  null != d[vi + 1] && 0 == d[vi + 1][2] ? str + "M" + vertices.p[v[0]].join(",") : str
                , "");
            }

            //var msg = new List<string>();
            // find state visual center
            vArray.forEach((ar, i) =>
            {
                if (ar == null)
                    return;
                var sorted = ar.sort((a, b) => b.Count() - a.Count()); // sort by points number
                var polygon = sorted.map(p => p.ToArray()).First();//取第一个最大区域

                provinces[i].pole = polygon.polylabel(1.0); // pole of inaccessibility
            });

            provinceSvgs = body;
        }

        public void generateStateLabels()
        {
            var cells = pack.cells;
            var states = pack.states;
            var features = pack.features;

            var paths = new List<KeyValuePair<int, IEnumerable<double[]>>>();
#if true
            foreach (var s in states)
            {
                if (0 == s.i || s.removed)
                    continue;

                if (s.pole == null)
                    continue;

                var used = new Dictionary<int, bool>();
                var visualCenter = pack.findCell(s.pole[0], s.pole[1]);
                var start_ = cells.state[visualCenter] == s.i ? visualCenter : s.center;

                var hull = getHull(start_, s.i, s.cells / 10f);
                var points = hull.map(v => pack.vertices.p[v]).ToList();

                var delauny = PointsSelection.fromPoints(points);
                var voronoi = new Voronoi()
                {
                    s_triangles_r = delauny.triangles,
                    s_halfedges_s = delauny.halfedges,
                };

                var voronoiCell = new VoronoiCells(voronoi, points, points.Count);

                var chain_ = connectCenters(voronoiCell.vertices, s.pole[1]);
                var relaxed = chain_.map(i => voronoiCell.vertices.p[i]).filter((p, i) => i % 15 == 0 || i + 1 == chain_.Count);
                var kp = new KeyValuePair<int, IEnumerable<double[]>>(s.i, relaxed);
                paths.push(kp);

                HashSet<int> getHull(int start, ushort state, float maxLake)
                {
                    var queue = new List<int>() { start };
                    var _hull = new HashSet<int>();

                    while (queue.Count > 0)
                    {
                        var q = queue.pop();
                        var nQ = cells.c[q].filter(c => cells.state[c] == state);

                        cells.c[q].forEach((c, d) =>
                        {

                            var passableLake = features[cells.f[c]].type == "lake" && features[cells.f[c]].cells < maxLake;
                            if (cells.b[c] || (cells.state[c] != state && !passableLake))
                            { _hull.Add(cells.v[q][d]); return; }

                            var nC = cells.c[c].filter(n => cells.state[n] == state);
                            var intersected = intersect(nQ, nC).Count();
                            if (_hull.Count > 20 && 0 == intersected && !passableLake)
                            { _hull.Add(cells.v[q][d]); return; }

                            if (used.ContainsKey(c))
                                return;
                            used[c] = true;
                            queue.push(c);
                        });
                    }
                    return _hull;
                }

                List<int> connectCenters(Vertices c, double y)
                {
                    // check if vertex is inside the area
                    var inside = c.p.map(p =>
                    {
                        if (p[0] <= 0 || p[1] <= 0 || p[0] >= graphWidth || p[1] >= graphHeight)
                            return false; // out of the screen
                        var cell = pack.findCell(p[0], p[1]);
                        return used.ContainsKey(cell);
                    }).ToArray();

                    var pointsInside = D3.range(c.p.Length).filter(i => inside[i]).ToArray();
                    if (0 == pointsInside.Length)
                        return new List<int>() { 0 };

                    var h = c.p.Length < 200 ? 0 : c.p.Length < 600 ? .5 : 1; // power of horyzontality shift
                    var end = pointsInside[D3.scan(pointsInside, (a, b) =>
                        (int)((c.p[a][0] - c.p[b][0]) + (Math.Abs(c.p[a][1] - y) - Math.Abs(c.p[b][1] - y)) * h)
                    )]; // left point
                    var start = pointsInside[D3.scan(pointsInside, (a, b) =>
                        (int)((c.p[b][0] - c.p[a][0]) - (Math.Abs(c.p[b][1] - y) - Math.Abs(c.p[a][1] - y)) * h)
                    )]; // right point

                    //debug.append("line").attr("x1", c.p[start][0]).attr("y1", c.p[start][1]).attr("x2", c.p[end][0]).attr("y2", c.p[end][1]).attr("stroke", "#00dd00");

                    // connect leftmost and rightmost points with shortest path
                    var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
                    var cost = new Dictionary<int, double>();
                    var from = new Dictionary<int, int>();
                    queue.push(new Item { e = start, p = 0 });

                    while (queue.Count > 0)
                    {
                        var next = queue.pop();
                        var n = next.e;
                        var p = next.p;
                        if (n == end)
                            break;

                        foreach (var v in c.v[n])
                        {
                            if (v == -1)
                                continue;
                            var totalCost = p + (inside[v] ? 1 : 100);
                            if ((from.ContainsKey(v) && from[v] > 0) || (cost.ContainsKey(v) && totalCost >= cost[v]))
                                continue;
                            cost[v] = totalCost;
                            from[v] = n;
                            queue.push(new Item { e = v, p = totalCost });
                        }
                    }

                    // restore path
                    var chain = new List<int>() { end };
                    var cur = end;
                    while (cur != start)
                    {
                        cur = from[cur];
                        if (inside[cur])
                            chain.push(cur);
                    }
                    return chain;
                }
            }
#endif
            labelSvgs = paths;
        }


        private SKPath stateBorders;
        private SKPath provinceBorders;

        public void generateBorders()
        {
            var cells = pack.cells;
            var vertices = pack.vertices;
            var n = cells.i.Length;

            SKPath sPath = new SKPath(), pPath = new SKPath();
            var sUsed = pack.states.map(s => new ushort[n]).ToArray();
            var pUsed = pack.provinces.map(p => new ushort[n]).ToArray();

            for (var i = 0; i < cells.i.Length; ++i)
            {
                if (0 == cells.state[i])
                    continue;

                var p = cells.province[i];
                var s = cells.state[i];

                // if cell is on province border
                var pidx = cells.c[i].findIndex(n2 => cells.state[n2] == s && p > cells.province[n2] && pUsed[p][n2] != cells.province[n2]);
                if (pidx != -1)
                {
                    var provToCell = cells.c[i][pidx];
                    var provTo = cells.province[provToCell];
                    pUsed[p][provToCell] = provTo;
                    var vertex = cells.v[i].find(v => vertices.c[v].some(n2 => cells.province[n2] == provTo));
                    var chain = connectVertices(vertex, p, cells.province, provTo, pUsed);

                    if (chain.Count > 1)
                    {
                        var path = linePoly(chain.map(c => vertices.p[c].SK()), false);
                        pPath.AddPath(path);
                        i--;
                        continue;
                    }
                }

                // if cell is on state border
                var sidx = cells.c[i].findIndex(n2 => cells.h[n2] >= 20 && s > cells.state[n2] && sUsed[s][n2] != cells.state[n2]);
                if (sidx != -1)
                {
                    var stateToCell = cells.c[i][sidx];
                    var stateTo = cells.state[stateToCell];
                    sUsed[s][stateToCell] = stateTo;
                    var vertex = cells.v[i].find(v => vertices.c[v].some(n2 => cells.h[n2] >= 20 && cells.state[n2] == stateTo));
                    var chain = connectVertices(vertex, s, cells.state, stateTo, sUsed);

                    if (chain.Count > 1)
                    {
                        var path = linePoly(chain.map(c => vertices.p[c].SK()), false);
                        sPath.AddPath(path);
                        i--;
                        continue;
                    }
                }
            }

            stateBorders = sPath;
            provinceBorders = pPath;

            List<int> connectVertices(int current, ushort f, ushort[] array, ushort t, ushort[][] used)
            {
                Func<int, bool> checkCell = c => c >= n || array[c] != f;
                Func<int, bool> checkVertex = v => vertices.c[v].some(c => array[c] == f) && vertices.c[v].some(c => array[c] == t && cells.h[c] >= 20);
                Func<bool, int> b2i = b => b ? 1 : 0;

                var chain = new List<int>();
                // find starting vertex
                for (var i = 0; i < 1000; i++)
                {
                    if (i == 999)
                        Debug.LogError($"Find starting vertex: limit is reached {current} {f} {t}");
                    var p = chain.Count > 1 ? chain[chain.Count - 2] : -1; // previous vertex
                    var v = vertices.v[current];
                    var c = vertices.c[current];

                    var v0 = checkCell(c[0]) != checkCell(c[1]) && checkVertex(v[0]);
                    var v1 = checkCell(c[1]) != checkCell(c[2]) && checkVertex(v[1]);
                    var v2 = checkCell(c[0]) != checkCell(c[2]) && checkVertex(v[2]);
                    if (b2i(v0) + b2i(v1) + b2i(v2) == 1)
                        break;
                    current = v0 && p != v[0] ? v[0] : v1 && p != v[1] ? v[1] : v[2];

                    if (chain.Count > 0 && current == chain[0])
                        break;
                    if (current == p)
                        return new List<int>();
                    chain.push(current);
                }

                chain = new List<int>() { current };
                // find path
                for (var i = 0; i < 1000; i++)
                {
                    if (i == 999)
                        Debug.LogError($"Find path: limit is reached {current} {f} {t}");
                    var p = chain.Count > 1 ? chain[chain.Count - 2] : -1; // previous vertex
                    var v = vertices.v[current];
                    var c = vertices.c[current];
                    c.filter(n2 => array[n2] == t).forEach(n2 => used[f][n2] = t);

                    var v0 = checkCell(c[0]) != checkCell(c[1]) && checkVertex(v[0]);
                    var v1 = checkCell(c[1]) != checkCell(c[2]) && checkVertex(v[1]);
                    var v2 = checkCell(c[0]) != checkCell(c[2]) && checkVertex(v[2]);
                    current = v0 && p != v[0] ? v[0] : v1 && p != v[1] ? v[1] : v[2];

                    if (current == p)
                        break;
                    if (current == chain[chain.Count - 1])
                        break;
                    if (chain.Count > 1 && b2i(v0) + b2i(v1) + b2i(v2) < 2)
                        break;
                    chain.push(current);
                    if (current == chain[0])
                        break;
                }
                return chain;
            }
        }

        public void drawStates(SKCanvas canvas)
        {
            var states = pack.states;
            var body = stateSvgs;

            var paint = new SKPaint() { IsAntialias = true };
            paint.Style = SKPaintStyle.Fill;
            body.forEach((p, i) =>
            {
                if (p.Length <= 10)
                    return;

                paint.Color = states[i].color.ToColor().Opacity(0.4f).SK();
                var path = SKPath.ParseSvgPathData(p);
                if (path != null)
                    canvas.DrawPath(path, paint);
                //Debug.Log($"{i} {states[i].name} {states[i].color} {p}");
            });

        }
        public void drawProvinces(SKCanvas canvas)
        {

            var provinces = pack.provinces;
            var body = provinceSvgs;

            var paint = new SKPaint() { IsAntialias = true };
            paint.Style = SKPaintStyle.Fill;
            body.forEach((p, i) =>
            {
                if (p.Length <= 10)
                    return;

                paint.Color = provinces[i].color.ToColor().Opacity(0.4f).SK();
                var path = SKPath.ParseSvgPathData(p);
                if (path != null)
                    canvas.DrawPath(path, paint);
            });
        }

        public void drawBurgs(SKCanvas canvas, Func<string, SKTypeface> faceFunc)
        {
            var scale = canvas.TotalMatrix.ScaleX;

            var text_capitalSize = (float)Math.Max(rn(8 - regionsInput / 20d), 3);
            var text_townSize = 3f;

            var icon_capitalSize = 1f;
            var icon_townSize = 0.5f;

            var paint = new SKPaint() { IsAntialias = true };

            var desired = text_capitalSize;
            var relative = Math.Max(rn((desired + desired / scale) / 2, 2), 1);
            var hidden = relative * scale < 6 || relative * scale > 50;

            if (!hidden)
            {
                var capitals = pack.burgs.filter(b => b != null && 0 != b.capital);
                // burgLabels.select("#cities").attr("fill", "#3e3e4b")
                //                             .attr("opacity", 1)
                //                             .attr("font-family", "Almendra SC")
                //                             .attr("data-font", "Almendra+SC")
                //                             .attr("font-size", citiesSize)
                //                             .attr("data-size", citiesSize);
                paint.Style = SKPaintStyle.Stroke;
                paint.Color = "3e3e4b".ToColor().SK();
                paint.StrokeWidth = 0.24f;
                paint.StrokeCap = SKStrokeCap.Butt;
                capitals.forEach(b =>
                {
                    canvas.DrawCircle((float)b.x, (float)b.y, icon_capitalSize, paint);
                });

                paint.Style = SKPaintStyle.Fill;
                paint.TextSize = text_capitalSize;
                paint.TextAlign = SKTextAlign.Center;
                paint.Typeface = faceFunc("AlmendraSC-Regular.ttf");
                capitals.forEach(b =>
                {
                    canvas.DrawText(b.name, (float)b.x, (float)b.y + icon_capitalSize * -1.5f, paint);
                });
            }

            desired = text_townSize;
            relative = Math.Max(rn((desired + desired / scale) / 2, 2), 1);
            hidden = relative * scale < 6 || relative * scale > 50;

            if (!hidden)
            {
                var towns = pack.burgs.filter(b => b != null && 0 != b.i && 0 == b.capital);
                // burgLabels.select("#towns").attr("fill", "#3e3e4b")
                //                            .attr("opacity", 1)
                //                            .attr("font-family", "Almendra SC")
                //                            .attr("data-font", "Almendra+SC")
                //                            .attr("font-size", 3)
                //                            .attr("data-size", 4);
                paint.Style = SKPaintStyle.Stroke;
                paint.Color = "3e3e4b".ToColor().SK();
                paint.StrokeWidth = 0.12f;
                paint.StrokeCap = SKStrokeCap.Butt;
                towns.forEach(b =>
                {
                    canvas.DrawCircle((float)b.x, (float)b.y, icon_townSize, paint);
                });

                paint.Style = SKPaintStyle.Fill;
                paint.TextSize = text_townSize;
                paint.TextAlign = SKTextAlign.Center;
                paint.Typeface = faceFunc("AlmendraSC-Regular.ttf");
                towns.forEach(b =>
                {
                    canvas.DrawText(b.name, (float)b.x, (float)b.y + icon_townSize * -1.5f, paint);
                });
            }
        }

        public void drawStateLabels(SKCanvas canvas, Func<string, SKTypeface> faceFunc)
        {
            var scale = canvas.TotalMatrix.ScaleX;
            var text_stateSize = 22f;//默认字体大小

            var desired = text_stateSize;
            var relative = Math.Max(rn((desired + desired / scale) / 2, 2), 1);
            var hidden = relative * scale < 6 || relative * scale > 50;
            if (hidden)
                return;

            var states = pack.states;
            var paths = labelSvgs;

            var paint = new SKPaint() { IsAntialias = true };
            paint.Typeface = faceFunc("AlmendraSC-Regular.ttf");
            paint.TextAlign = SKTextAlign.Center;
            paint.Color = SKColors.Black;

            paint.TextSize = text_stateSize;
            var letterLength = paint.MeasureText("Average") / 7;

            paths.ForEach(pp =>
            {
                var s = states[pp.Key];
                var points = pp.Value.ToArray();
                var path = lineGen1(points);
                var pathMeasure = new SKPathMeasure(path, false, 1);

                var pathLength = points.Length > 1 ? pathMeasure.Length / letterLength : 0;


                string[] lines;
                var ratio = 100f;

                if (pathLength < s.name.Length)
                {
                    // only short name will fit
                    lines = splitInTwo(s.name);
                    ratio = Math.Max(Math.Min((int)rn(pathLength / lines[0].Length * 60), 150), 50);
                }
                else if (pathLength > s.fullName.Length * 2.5)
                {
                    // full name will fit in one line
                    lines = new string[] { s.fullName };
                    ratio = Math.Max(Math.Min((int)rn(pathLength / lines[0].Length * 70), 170), 70);
                }
                else
                {
                    // try miltilined label
                    lines = splitInTwo(s.fullName);
                    ratio = Math.Max(Math.Min((int)rn(pathLength / lines[0].Length * 60), 150), 70);
                }

                // prolongate path if it's too short
                if (pathLength > 0 && pathLength < lines[0].Length)
                {
                    double[] f = points[0], l = points[points.Length - 1];
                    double dx = l[0] - f[0], dy = l[1] - f[1];
                    double mod = Math.Abs(letterLength * lines[0].Length / dx) / 2;
                    points[0] = new double[] { rn(f[0] - dx * mod), rn(f[1] - dy * mod) };
                    points[points.Length - 1] = new double[] { rn(l[0] + dx * mod), rn(l[1] + dy * mod) };

                    // textPath.attr("d", round(lineGen(points)));
                    path = lineGen1(points);
                }

                // example.attr("font-size", ratio+"%");// states font size 22px
                paint.TextSize = ratio * 22 / 100;
                paint.Style = SKPaintStyle.Fill;

                var rect = new SKRect();
                var width = paint.MeasureText(s.name, ref rect);
                var height = rect.Height;
                var topY = (height * 0.5f) * (1 - lines.Length * 0.5f);
                lines.forEach((l, i) =>
                {
                    canvas.DrawTextOnPath(l, path, 0, topY + height * i, paint);
                });

                //paint.Style = SKPaintStyle.Stroke;
                //canvas.DrawPath(path, paint);
                //points.forEach(p => canvas.DrawCircle(p.SK(), 2, paint));
            });

        }

        public void drawBorders(SKCanvas canvas)
        {
            var paintState = new SKPaint() { IsAntialias = true };
            paintState.Color = "#56566d".ToColor().Opacity(0.8f).SK();
            paintState.Style = SKPaintStyle.Stroke;
            paintState.StrokeWidth = 1f;
            paintState.StrokeCap = SKStrokeCap.Butt;
            paintState.PathEffect = SKPathEffect.CreateDash(new float[] { 2, 2 }, 0);
            canvas.DrawPath(stateBorders, paintState);

            var paintProvince = new SKPaint() { IsAntialias = true };
            paintProvince.Color = "#56566d".ToColor().Opacity(0.8f).SK();
            paintProvince.Style = SKPaintStyle.Stroke;
            paintProvince.StrokeWidth = 0.2f;
            paintProvince.StrokeCap = SKStrokeCap.Butt;
            paintProvince.PathEffect = SKPathEffect.CreateDash(new float[] { 1, 1 }, 0);
            canvas.DrawPath(provinceBorders, paintProvince);
        }

        public void drawRoutes(SKCanvas canvas)
        {
            var cells = pack.cells;
            var burgs = pack.burgs;

            Action<int[], SKPaint> draw = (road, brush) =>
             {
                 var pp = road.map(c =>
                 {
                     var b = cells.burg[c];
                     var x = 0 != b ? burgs[b].x : cells.p[c][0];
                     var y = 0 != b ? burgs[b].y : cells.p[c][1];
                     return new double[] { x, y };
                 }).ToArray();
                 canvas.DrawPath(lineGen1(pp), brush);
             };

            var paint = new SKPaint() { IsAntialias = true };
            paint.Style = SKPaintStyle.Stroke;

            //roads.attr("opacity", .9).attr("stroke", "#d06324").attr("stroke-width", .7)
            //.attr("stroke-dasharray", "2").attr("stroke-linecap", "butt").attr("filter", null).attr("mask", null);
            paint.Color = "#d06324".ToColor().Opacity(0.9f).SK();
            paint.StrokeWidth = 0.7f;
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 2, 2 }, 0);
            paint.StrokeCap = SKStrokeCap.Butt;
            capitalRoutes.forEach(road => draw(road, paint));

            //trails.attr("opacity", .9).attr("stroke", "#d06324").attr("stroke-width", .25)
            //.attr("stroke-dasharray", ".8 1.6").attr("stroke-linecap", "butt").attr("filter", null).attr("mask", null);
            paint.Color = "#d06324".ToColor().Opacity(0.9f).SK();
            paint.StrokeWidth = 0.25f;
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { .8f, 1.6f }, 0);
            paint.StrokeCap = SKStrokeCap.Butt;
            townRoutes.forEach(road => draw(road, paint));

            //searoutes.attr("opacity", .8).attr("stroke", "#ffffff").attr("stroke-width", .45)
            //.attr("stroke-dasharray", "1 2").attr("stroke-linecap", "round").attr("filter", null).attr("mask", null);
            paint.Color = "#ffffff".ToColor().Opacity(0.8f).SK();
            paint.StrokeWidth = 0.45f;
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 1, 2 }, 0);
            paint.StrokeCap = SKStrokeCap.Round;
            oceanRoutes.forEach(road => draw(road, paint));
        }
    }
}
