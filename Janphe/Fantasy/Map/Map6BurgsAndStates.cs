using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe.Fantasy.Map
{
    using static Utils;
    using static Grid;

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
            regionsInput = map.Options.ReligionsNumber;
            manorsInput = map.Options.ManorsInput;
            neutralInput = map.Options.NeutralInput;
            statesNeutral = map.Options.StatesNeutral;
            Debug.Log($"BurgsAndStates {powerInput} {regionsInput} {manorsInput} {neutralInput} {statesNeutral}");

            graphWidth = map.Options.Width;
            graphHeight = map.Options.Height;
        }

        // temporary elevate some lakes to resolve depressions and flux the water to form an open (exorheic) lake
        public void generate()
        {
            var cells = pack.cells;
            var n = cells.i.Length;

            cells.burg = new ushort[n];// cell burg
            cells.road = new ushort[n];// cell road power
            cells.crossroad = new ushort[n];// cell cell crossroad power

            pack.burgs = placeCapitals();
            pack.states = new List<State>();
            createStates();
            var capitalRoutes = Routes.getRoads();

            placeTowns();
            expandStates();
            normalizeStates();
            var townRoutes = Routes.getTrails();
            specifyBurgs();

            var oceanRoutes = Routes.getSearoutes();

            collectStatistics();
            assignColors();

            generateDiplomacy();

            Routes.draw(capitalRoutes, townRoutes, oceanRoutes);
            drawBurgs();
        }
        private List<Burg> placeCapitals()
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
                    i = -1; burgs = new List<Burg>() { null }; spacing /= 1.2;
                }
            }

            burgs[0] = new Burg() { tree = burgsTree };
            return burgs;
        }
        // For each capital create a state
        private void createStates()
        {
            var cells = pack.cells;
            var cultures = pack.cultures;

            var burgs = pack.burgs;
            var states = pack.states;

            states.Clear(); states.Add(new State { i = 0, name = "Neutrals" });

            var colors = getColors(burgs.Count - 1);

            for (ushort i = 0; i < burgs.Count; ++i)
            {
                if (i == 0) continue;// skip first element

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
                    expansionism = (float)expansionism,
                    capital = i,
                    type = type,
                    center = b.cell,
                    culture = b.culture
                });
                cells.burg[b.cell] = i;
            }
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
                    if (cells.burg[sorted[i]] > 0) continue;
                    var cell = sorted[i]; double x = cells.r_points[cell][0], y = cells.r_points[cell][1];
                    var s = spacing * gauss(1, .3, .2, 2, 2); // randomize to make placement not uniform
                    if (burgsTree.find(x, y, s) != null) continue;// to close to existing burg
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
            var biomesData = map.biomesData;

            var cells = pack.cells;
            var states = pack.states;
            var cultures = pack.cultures;
            var burgs = pack.burgs;

            cells.state = new ushort[cells.i.Length];// cell state
            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            var cost = new Dictionary<int, double>();
            foreach (var s in states.Where(s => s.i > 0 && !s.removed))
            {
                cells.state[burgs[s.capital].cell] = s.i;
                var b = cells.biome[cultures[s.culture].center]; // native biome
                queue.push(new Item { e = s.center, p = 0, s = s.i, b = b });
                cost[s.center] = 1;
            }

            var neutral = cells.i.Length / 5000d * 2000 * neutralInput * statesNeutral; // limit cost for state growth

            while (queue.Count > 0)
            {
                var next = queue.pop();
                int n = next.e, s = next.s, b = next.b;
                var p = next.p;
                var type = states[s].type;

                foreach (var e in cells.r_neighbor_r[n])
                {
                    if (cells.state[e] > 0 && e == states[cells.state[e]].center) continue; // do not overwrite capital cells

                    var cultureCost = states[s].culture == cells.culture[e] ? -9 : 700;
                    var biomeCost = getBiomeCost(b, cells.biome[e], type);
                    var heightCost = getHeightCost(pack.features[cells.f[e]], cells.r_height[e], type);
                    var riverCost = getRiverCost(cells.r[e], e, type);
                    var typeCost = getTypeCost(cells.t[e], type);
                    var tmp = 10 + cultureCost + biomeCost + heightCost + riverCost + typeCost;
                    var totalCost = rn((decimal)p + (decimal)tmp / (decimal)states[s].expansionism, 6);

                    if (totalCost > neutral) return;

                    if (!cost.ContainsKey(e) || totalCost < cost[e])
                    {
                        if (cells.r_height[e] >= 20) cells.state[e] = (ushort)s; // assign state to cell
                        cost[e] = totalCost;
                        queue.push(new Item { e = e, p = totalCost, s = s, b = b });
                    }
                }
            }

            foreach (var b in burgs.Where(b => b.i > 0 && !b.removed))
            { b.state = cells.state[b.cell]; }// assign state to burgs

            double getBiomeCost(int b, int biome, string type)
            {
                if (b == biome) return 10;// tiny penalty for native biome
                if (type == "Hunting") return biomesData.cost[biome] * 2; // non-native biome penalty for hunters
                if (type == "Nomadic" && biome > 4 && biome < 10) return biomesData.cost[biome] * 3; // forest biome penalty for nomads
                return biomesData.cost[biome]; // general non-native biome penalty
            }
            double getHeightCost(Feature f, int h, string type)
            {
                if (type == "Lake" && f.type == "lake") return 10; // low lake crossing penalty for Lake cultures
                if (type == "Naval" && h < 20) return 300; // low sea crossing penalty for Navals
                if (type == "Nomadic" && h < 20) return 10000; // giant sea crossing penalty for Nomads
                if (h < 20) return 1000; // general sea crossing penalty
                if (type == "Highland" && h < 62) return 1100; // penalty for highlanders on lowlands
                if (type == "Highland") return 0; // no penalty for highlanders on highlands
                if (h >= 67) return 2200; // general mountains crossing penalty
                if (h >= 44) return 300; // general hills crossing penalty
                return 0;
            }
            double getRiverCost(int r, int i, string type)
            {
                if (type == "River") return r > 0 ? 0 : 100; // penalty for river cultures
                if (0 == r) return 0; // no penalty for others if there is no river
                return Math.Min(Math.Max(cells.fl[i] / 10d, 20), 100); // river penalty from 20 to 100 based on flux
            }
            double getTypeCost(int t, string type)
            {
                if (t == 1) return type == "Naval" || type == "Lake" ? 0 : type == "Nomadic" ? 60 : 20; // penalty for coastline
                if (t == 2) return type == "Naval" || type == "Nomadic" ? 30 : 0; // low penalty for land level 2 for Navals and nomads
                if (t != -1) return type == "Naval" || type == "Lake" ? 100 : 0;  // penalty for mainland for navals
                return 0;
            }
        }
        private void normalizeStates()
        {
            var cells = pack.cells; var burgs = pack.burgs;

            foreach (var i in cells.i)
            {
                if (cells.r_height[i] < 20 || cells.burg[i] > 0) continue; // do not overwrite burgs
                if (cells.r_neighbor_r[i].some(c => cells.burg[c] > 0 && burgs[cells.burg[c]].capital > 0)) continue; // do not overwrite near capital

                var neibs = cells.r_neighbor_r[i].Where(c => cells.r_height[c] >= 20);
                var adversaries = neibs.Where(c => cells.state[c] != cells.state[i]).ToArray();
                if (adversaries.Length < 2) continue;
                var buddies = neibs.Where(c => cells.state[c] == cells.state[i]).ToArray();
                if (buddies.Length > 2) continue;
                if (adversaries.Length <= buddies.Length) continue;
                cells.state[i] = cells.state[adversaries[0]];
            }
        }
        // define burg coordinates and define details
        private void specifyBurgs()
        {
            var cells = pack.cells; var vertices = pack.vertices;

            foreach (var b in pack.burgs)
            {
                if (null == b || 0 == b.i) continue;
                var i = b.cell;

                // asign port status: capital with any harbor and towns with good harbors
                var port = (0 != b.capital && 0 != cells.harbor[i]) || cells.harbor[i] == 1;
                b.port = (ushort)(port ? cells.f[cells.haven[i]] : 0); // port is defined by feature id it lays on

                // define burg population (keep urbanization at about 10% rate)
                b.population = rn(Math.Max((cells.s[i] + cells.road[i]) / 8d + b.i / 1000d + i % 100 / 1000d, .1), 3);
                if (0 != b.capital) b.population = rn(b.population * 1.3, 3); // increase capital population

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
                    if (0 != (i % 2)) b.x = rn(b.x + shift, 2); else b.x = rn(b.x - shift, 2);
                    if (0 != (cells.r[i] % 2)) b.y = rn(b.y + shift, 2); else b.y = rn(b.y - shift, 2);
                }
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

        private void collectStatistics()
        {
            var cells = pack.cells; var states = pack.states;
            var neighbors = new HashSet<ushort>[states.Count];
            states.forEach((s, i) =>
            {
                s.cells = s.area = s.burgs = 0;
                s.rural = s.urban = 0;
                neighbors[i] = new HashSet<ushort>();
            });

            foreach (var i in cells.i)
            {
                if (cells.h[i] < 20) continue;
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
                if (0 == s.i || s.removed) return;
                var neibs = s.neighbors;
                s.color = colors.find(c => neibs.every(n => pack.states[n].color != c));
                if (not(s.color)) s.color = getRandomColor();
                colors.push(colors.shift());
            });

            // randomize each already used color a bit
            colors.forEach(c =>
            {
                var sameColored = pack.states.filter(s => s.color == c);
                sameColored.forEach((s, d) =>
                {
                    if (0 == d) return;
                    s.color = getMixedColor(s.color);
                });
            });
        }
        // generate Diplomatic Relationships
        private void generateDiplomacy()
        {
            var cells = pack.cells; var states = pack.states;
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
            if (valid.Count() < 2) return; // no states to renerate relations with
            var areaMean = D3.mean(valid.map(s => (int)s.area)); // avarage state area

            // generic relations
            for (var f = 1; f < states.Count; f++)
            {
                if (states[f].removed) continue;

                if (states[f].diplomacy.includes("Vassal"))
                {
                    // Vassals copy relations from their Suzerains
                    var suzerain = states[f].diplomacy.indexOf("Vassal");

                    for (var i = 1; i < states.Count; i++)
                    {
                        if (i == f || i == suzerain) continue;
                        states[f].diplomacy[i] = states[suzerain].diplomacy[i];
                        if (states[suzerain].diplomacy[i] == "Suzerain") states[f].diplomacy[i] = "Ally";
                        for (var e = 1; e < states.Count; e++)
                        {
                            if (e == f || e == suzerain) continue;
                            if (states[e].diplomacy[suzerain] == "Suzerain" || states[e].diplomacy[suzerain] == "Vassal") continue;
                            states[e].diplomacy[f] = states[e].diplomacy[suzerain];
                        }
                    }
                    continue;
                }

                for (var t = f + 1; t < states.Count; t++)
                {
                    if (states[t].removed) continue;

                    if (states[t].diplomacy.includes("Vassal"))
                    {
                        var suzerain = states[t].diplomacy.indexOf("Vassal");
                        states[f].diplomacy[t] = states[f].diplomacy[suzerain];
                        continue;
                    };

                    var naval = states[f].type == "Naval" && states[t].type == "Naval" && cells.f[states[f].center] != cells.f[states[t].center];
                    var neib = naval ? false : states[f].neighbors.includes((ushort)t);
                    //var neibOfNeib = naval || neib ? false : states[f].neighbors.map(n => states[n].neighbors).join().includes(t);
                    var neibOfNeib = naval || neib ? false : states[f].neighbors.map(n => states[n].neighbors).ToArray().some(s => s.includes((ushort)t));

                    var status = naval ? rw(navals) :
                        neib ? rw(neibs) :
                        neibOfNeib ? rw(neibsOfNeibs) : rw(far);

                    // add Vassal
                    if (neib && P(.8) && states[f].area > areaMean && states[t].area < areaMean && states[f].area / states[t].area > 2) status = "Vassal";
                    states[f].diplomacy[t] = status == "Vassal" ? "Suzerain" : status;
                    states[t].diplomacy[f] = status;
                }
            }

            // declare wars
            for (var attacker = 1; attacker < states.Count; attacker++)
            {
                var ad = states[attacker].diplomacy; // attacker relations;
                if (states[attacker].removed) continue;
                if (!ad.includes("Rival")) continue; // no rivals to attack
                if (ad.includes("Vassal")) continue; // not independent
                if (ad.includes("Enemy")) continue; // already at war

                // random independent rival
                var defender = ra(
                    ad.map((r, d) => r == "Rival" && !states[d].diplomacy.includes("Vassal") ? d : 0)
                    .filter(d => d != 0).ToArray()
                    );
                double ap = states[attacker].area * states[attacker].expansionism, dp = states[defender].area * states[defender].expansionism;
                if (ap < dp * gauss(1.6, .8, 0, 10, 2)) continue; // defender is too strong
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
                    if (r != "Ally" || states[d].diplomacy.includes("Vassal")) return;
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
                    if (r != "Ally" || states[d].diplomacy.includes("Vassal") || defenders.includes(d)) return;

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
            }
        }

        // select a forms for listed or all valid states
        public void defineStateForms(List<int> list = null)
        {
            var states = pack.states.filter(s => 0 != s.i && !s.removed);
            if (states.Count() < 1) return;

            var generic = JObject.Parse(@"{ Monarchy:25, Republic: 2, Union: 1}").dict();
            var naval = JObject.Parse(@"{ Monarchy:25, Republic:8, Union:3 }").dict();
            var genericArray = ww(generic);
            var navalArray = ww(naval);

            var median = D3.median(pack.states.map(s => s.area));
            var empireMin = states.map(s => s.area).sort((a, b) => b - a).ToArray()[Math.Max((int)Math.Ceiling(states.Count().pow(.4)) - 2, 0)];
            var expTiers = pack.states.map(s =>
            {
                var tier = Math.Min((int)Math.Floor(s.area / median * 2.6), 4);
                if (tier == 4 && s.area < empireMin) tier = 3;
                return tier;
            }).ToArray();

            var monarchy = JArray.Parse(@"['Duchy', 'Grand Duchy', 'Principality', 'Kingdom', 'Empire']"); // per expansionism tier
            var republic = JObject.Parse(@"{ Republic:70, Federation: 2, Oligarchy: 2, Tetrarchy: 1, Triumvirate: 1, Diarchy: 1, 'Trade Company':3}").dict(); // weighted random
            var union = JObject.Parse(@"{ Union:3, League:4, Confederation:1, 'United Kingdom':1, 'United Republic':1, 'United Provinces':2, Commonwealth:1, Heptarchy:1 }").dict(); // weighted random

            foreach (var s in states)
            {
                if (null != list && !list.includes(s.i)) continue;

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
            }

            string selectForm(State s)
            {
                var @base = pack.cultures[s.culture].@base;

                if (s.form == "Monarchy")//君主政体, 君主国, 君主政治
                {
                    var form = monarchy[expTiers[s.i]].Value<string>();
                    // Default name depends on exponent tier, some culture bases have special names for tiers
                    if (s.diplomacy != null)
                    {
                        if (form == "Duchy" && s.neighbors.Length > 1 && rand(6) < s.neighbors.Length && s.diplomacy.includes("Vassal")) return "Marches"; // some vassal dutchies on borderland
                        if (P(.3) && s.diplomacy.includes("Vassal")) return "Protectorate"; // some vassals
                    }

                    if (@base == 16 && (form == "Empire" || form == "Kingdom")) return "Sultanate"; // Turkic
                    if (@base == 5 && (form == "Empire" || form == "Kingdom")) return "Tsardom"; // Ruthenian
                    if (@base == 31 && (form == "Empire" || form == "Kingdom")) return "Khaganate"; // Mongolian
                    if (@base == 12 && (form == "Kingdom" || form == "Grand Duchy")) return "Shogunate"; // Japanese
                    if (new int[] { 18, 17 }.includes(@base) && form == "Empire") return "Caliphate"; // Arabic, Berber
                    if (@base == 18 && (form == "Grand Duchy" || form == "Duchy")) return "Emirate"; // Arabic
                    if (@base == 7 && (form == "Grand Duchy" || form == "Duchy")) return "Despotate"; // Greek
                    if (@base == 31 && (form == "Grand Duchy" || form == "Duchy")) return "Ulus"; // Mongolian
                    if (@base == 16 && (form == "Grand Duchy" || form == "Duchy")) return "Beylik"; // Turkic
                    if (@base == 24 && (form == "Grand Duchy" || form == "Duchy")) return "Satrapy"; // Iranian
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
                        if (P(.3)) return "City-state";
                    }
                    return rw(republic);
                }

                if (s.form == "Union")//联盟, 联合, 结合, 工会
                    return rw(union);

                if (s.form == "Theocracy")//神权政治, 神权国
                {
                    // default name is "Theocracy", some culture bases have special names
                    if (new int[] { 0, 1, 2, 3, 4, 6, 8, 9, 13, 15, 20 }.includes(@base)) return "Diocese"; // Euporean
                    if (new int[] { 7, 5 }.includes(@base)) return "Eparchy"; // Greek, Ruthenian
                    if (new int[] { 21, 16 }.includes(@base)) return "Imamah"; // Nigerian, Turkish
                    if (new int[] { 18, 17, 28 }.includes(@base)) return "Caliphate"; // Arabic, Berber, Swahili
                    return "Theocracy";
                }
                return "";
            }
        }

        private string getFullName(State s)
        {
            if (not(s.formName)) return s.name;
            if (not(s.name) && @is(s.formName)) return "The " + s.formName;
            // state forms requiring Adjective + Name, all other forms use scheme Form + Of + Name
            var adj = new string[] { "Empire", "Sultanate", "Khaganate", "Shogunate", "Caliphate", "Despotate", "Theocracy", "Oligarchy", "Union", "Confederation", "Trade Company", "League", "Tetrarchy", "Triumvirate", "Diarchy", "Horde" };
            return adj.includes(s.formName) ? getAdjective(s.name) + " " + s.formName : s.formName + " of " + s.name;
        }

        public void generateProvinces(bool regenerate = false)
        {
            var localSeed = regenerate ? (int)Math.Floor(Random.NextDouble() * 1e9) : map.Options.MapSeed;
            Random.Seed(localSeed);

            var cells = pack.cells; var states = pack.states; var burgs = pack.burgs;
            var provinces = pack.provinces = new List<Province>() { null };
            cells.province = new ushort[cells.i.Length]; // cell state
            var percentage = map.Options.ProvincesInput;
            if (states.Count < 2 || 0 == percentage) { states.forEach(s => s.provinces = new List<ushort>()); return; } // no provinces
            var max = percentage == 100 ? 1000 : gauss(20, 5, 5, 100) * percentage.pow(.5); // max growth

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
                if (0 == s.i || s.removed) return;
                var stateBurgs = burgs.filter(b => b.state == s.i && !b.removed).sort((a, b) => (b.population * gauss(1, .2, .5, 1.5, 3)).CompareTo(a.population)).ToArray();
                if (stateBurgs.Length < 2) return; // at least 2 provinces are required
                var provincesNumber = Math.Max(Math.Ceiling(stateBurgs.Length * percentage / 100d), 2);
                var form = forms[s.form].dict<int>();

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
                }
            });

            // expand generated provinces
            var queue = new PriorityQueue<Item>((a, b) => a.p.CompareTo(b.p));
            var cost = new Dictionary<int, double>();
            provinces.forEach(p =>
            {
                if (0 == p.i || p.removed) return;
                cells.province[p.center] = (ushort)p.i;
                queue.push(new Item { e = p.center, p = 0, province = p.i, state = p.state });
                cost[p.center] = 1;
                //debug.append("circle").attr("cx", cells.p[p.center][0]).attr("cy", cells.p[p.center][1]).attr("r", .3).attr("fill", "red");
            });

            while (queue.Count > 0)
            {
                var next = queue.pop(); int n = next.e, province = next.province, state = next.state; var p = next.p;
                cells.c[n].forEach(e =>
                {
                    var land = cells.h[e] >= 20;
                    if (!land && 0 == cells.t[e]) return; // cannot pass deep ocean
                    if (land && cells.state[e] != state) return;
                    var evevation = cells.h[e] >= 70 ? 100 : cells.h[e] >= 50 ? 30 : cells.h[e] >= 20 ? 10 : 100;
                    var totalCost = p + evevation;

                    if (totalCost > max) return;
                    if (!cost.ContainsKey(e) || totalCost < cost[e])
                    {
                        if (land) cells.province[e] = (ushort)province; // assign province to a cell
                        cost[e] = totalCost;
                        queue.push(new Item { e = e, p = totalCost, province = province, state = state });
                    }
                });
            }

            // justify provinces shapes a bit
            foreach (var i in cells.i)
            {
                if (0 != cells.burg[i]) continue;// do not overwrite burgs
                var neibs = cells.c[i].filter(c => cells.state[c] == cells.state[i]).map(c => cells.province[c]);
                var adversaries = neibs.filter(c => c != cells.province[i]).ToArray();
                if (adversaries.Length < 2) continue;
                var buddies = neibs.filter(c => c == cells.province[i]).Count();
                if (buddies > 2) continue;
                var competitors = adversaries.map(p => adversaries.reduce((s, v) => v == p ? ++s : s, (ushort)0)).ToArray();
                var maxc = D3.max(competitors);
                if (buddies >= maxc) continue;
                cells.province[i] = adversaries[competitors.indexOf(maxc)];
            }

            // add "wild" provinces if some cells don't have a province assigned
            var noProvince = cells.i.filter(i => 0 != cells.state[i] && 0 == cells.province[i]); // cells without province assigned
            states.forEach(s =>
            {
                if (0 == s.provinces.Count) return;
                var stateNoProvince = noProvince.filter(i => cells.state[i] == s.i && 0 == cells.province[i]);
                while (stateNoProvince.Count() > 0)
                {
                    // add new province
                    var province = (ushort)provinces.Count;
                    var burgCell = stateNoProvince.find(i => 0 != cells.burg[i]);
                    var center = 0 != burgCell ? burgCell : stateNoProvince.First();
                    var burg = 0 != burgCell ? cells.burg[burgCell] : 0;
                    cells.province[center] = province;

                    // expand province
                    cost = new Dictionary<int, double>(); cost[center] = 1;
                    queue.push(new Item { e = center, p = 0 });
                    while (queue.Count > 0)
                    {
                        var next = queue.pop(); var n = next.e; var p = next.p;

                        cells.c[n].forEach(e =>
                        {
                            if (0 != cells.province[e]) return;
                            var land = cells.h[e] >= 20;
                            if (0 != cells.state[e] && cells.state[e] != s.i) return;
                            var ter = land ? cells.state[e] == s.i ? 3 : 20 : 0 != cells.t[e] ? 10 : 30;
                            var totalCost = p + ter;

                            if (totalCost > max) return;
                            if (!cost.ContainsKey(e) || totalCost < cost[e])
                            {
                                if (land && cells.state[e] == s.i) cells.province[e] = province; // assign province to a cell
                                cost[e] = totalCost;
                                queue.push(new Item { e = e, p = totalCost });
                            }
                        });
                    }

                    // generate "wild" province name
                    var c = cells.culture[center];
                    var name = 0 != burgCell && P(.5) ? burgs[burg].name : Names.getState(Names.getCultureShort(c), c);
                    var f = pack.features[cells.f[center]];
                    var provCells = stateNoProvince.filter(i => cells.province[i] == province);
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

                    // check if there is a land way within the same state between two cells
                    bool isPassable(int from, int to)
                    {
                        if (cells.f[from] != cells.f[to]) return false; // on different islands
                        var lstTmp = new List<int>() { from }; var used = new byte[cells.i.Length]; var state = cells.state[from];
                        while (lstTmp.Count > 0)
                        {
                            var current = lstTmp.pop();
                            if (current == to) return true; // way is found
                            cells.c[current].forEach(cTmp =>
                            {
                                if (0 != used[cTmp] || cells.h[cTmp] < 20 || cells.state[cTmp] != state) return;
                                lstTmp.push(cTmp);
                                used[cTmp] = 1;
                            });
                        }
                        return false;// way is not found
                    }

                    // re-check
                    stateNoProvince = noProvince.filter(i => cells.state[i] == s.i && 0 == cells.province[i]);
                }
            });
        }

        private void drawBurgs()
        {
        }

        public void drawStates()
        {
            var cells = pack.cells; var vertices = pack.vertices; var states = pack.states; var n = cells.i.Length;
            var used = new byte[cells.i.Length];
            //var vArray = new Array(states.length); // store vertices array
            //var body = new Array(states.length).fill(""); // store path around each state
            //var gap = new Array(states.length).fill(""); // store path along water for each state to fill the gaps
        }
        public void drawBorders() { }
        public void drawStateLabels()
        {

        }
    }
}
