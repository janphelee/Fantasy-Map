using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe.Fantasy.Map
{
    internal class Map4Rivers
    {
        private Grid grid { get; set; }
        private Grid pack { get; set; }

        private Grid.Cells cells { get; set; }
        private Grid.Feature[] features { get; set; }
        private Grid.Vertices vertices { get; set; }
        private int n { get; set; }
        private int graphWidth { get; set; }
        private int graphHeight { get; set; }

        public Map4Rivers(MapJobs map)
        {
            grid = map.grid;
            pack = map.pack;

            cells = pack.cells;
            features = pack.features;
            vertices = pack.vertices;
            n = cells.i.Length;
            graphWidth = map.Options.Width;
            graphHeight = map.Options.Height;
        }

        public void generate(bool changeHeights = true)
        {
            markupLand();
            //DebugHelper.SaveArray("pack.cells.t.txt", cells.t);

            var heights = cells.r_height
                .Select((h, i) => h < 20 || cells.t[i] < 1 ? h : h + cells.t[i] / 100.0)
                .Select((h, i) => h < 20 || cells.t[i] < 1 ? h : h + cells.r_neighbor_r[i].Select(c => (int)cells.t[c]).Average() / 10000.0).ToArray();

            //DebugHelper.SaveArray("pack.heights.txt", heights);
            resolveDepressions(heights);
            //DebugHelper.SaveArray("pack.heights2.txt", heights);

            foreach (var f in features)
            {
                if (f == null) continue;
                f.river = 0;
                f.flux = 0;
            }

            cells.fl = new ushort[n];
            cells.r = new ushort[n];
            cells.conf = new byte[n];

            var riverNext = 1; // first river id is 1, not 0
            var riversData = drainWater(heights, out riverNext);
            defineRivers(riverNext, riversData);

            if (changeHeights) cells.r_height = heights.Select(h => (byte)h).ToArray();
            //DebugHelper.SaveArray("pack.cells.fl.txt", cells.fl);
            //DebugHelper.SaveArray("pack.cells.r.txt", cells.r);
            //DebugHelper.SaveArray("pack.cells.conf.txt", cells.conf);
            //DebugHelper.SaveArray("pack.cells.h.txt", cells.r_height);
        }

        // build distance field in cells from water (cells.t)
        private void markupLand()
        {
            Func<int, int[]> q = _t => cells.i.Where(i => cells.t[i] == _t).ToArray();

            int t = 2;
            int[] queue = q(t);
            while (queue.Length > 0)
            {
                foreach (var i in queue)
                    foreach (var c in cells.r_neighbor_r[i])
                    {
                        if (cells.t[c] == 0) cells.t[c] = (sbyte)(t + 1);
                    }
                t++;
                queue = q(t);
            }
        }

        private bool resolveDepressions(double[] h)
        {
            // highest cells go first
            var land = cells.i.Where(i => h[i] >= 20 && h[i] < 100 && !cells.r_near_border[i]);
            var sorted = Utils.LinqSort(land, (a, b) => h[b].CompareTo(h[a])).ToArray();

            //DebugHelper.SaveArray("pack.cells.land1.txt", sorted);

            //var msg = new List<string>();

            var depressed = false;
            for (int l = 0, depression = int.MaxValue; depression > 0 && l < 100; l++)
            {
                depression = 0;

                foreach (var i in sorted)
                {
                    //msg.Add($"{l} {i}");
                    var minHeight = cells.r_neighbor_r[i].Select(c => h[c]).Min();

                    //msg.Add($"{l} {i} {h[i]} min:{minHeight} {depression}");

                    if (minHeight >= 100) continue; // already max height

                    if (h[i] <= minHeight)
                    {
                        h[i] = Math.Min(minHeight + 1, 100);
                        depression++;
                        depressed = true;
                    }
                    //msg.Add($"{l} {i} {h[i]} min:{minHeight} {depression}");
                }
            }
            //DebugHelper.SaveArray("resolveDepressions.txt", msg);

            return depressed;
        }

        class RiverData { public int river, cell, parent; public double x, y; }
        private RiverData[] drainWater(double[] h, out int riverNext)
        {
            //var msg = new List<string>();
            riverNext = 1;
            var riversData = new List<RiverData>();

            var land = cells.i.Where(i => h[i] >= 20);

            //var land1 = new List<int>(land).ToArray();
            land = Utils.LinqSort(land, (a, b) => h[b].CompareTo(h[a]));
            //Utils.OrderBy(land, (a, b) => h[b].CompareTo(h[a]));
            //Utils.ArraySort(land, (a, b) => h[b].CompareTo(h[a]));

            //DebugHelper.SaveArray("pack.cells.land2.txt", land);
            //DebugHelper.SaveArray("grid.cells.prec.txt", grid.cells.prec);
            //DebugHelper.SaveArray("pack.cells.g.txt", cells.g);

            var p = cells.r_points;
            foreach (var i in land)
            {
                cells.fl[i] += grid.cells.prec[cells.g[i]]; // flux from precipitation
                double x = p[i][0], y = p[i][1];

                // near-border cell: pour out of the screen
                if (cells.r_near_border[i])
                {
                    if (cells.r[i] > 0)
                    {
                        var to = new double[2];
                        var _min = new double[] { y, graphHeight - y, x, graphWidth - x }.Min();

                        if (_min == y)
                        { to[0] = x; to[1] = 0; }
                        else if (_min == graphHeight - y)
                        { to[0] = x; to[1] = graphHeight; }
                        else if (_min == x)
                        { to[0] = 0; to[1] = y; }
                        else if (_min == graphWidth - x)
                        { to[0] = graphWidth; to[1] = y; }
                        riversData.Add(new RiverData() { river = cells.r[i], cell = i, x = to[0], y = to[1] });
                    }
                    continue;
                }

                var min = cells.r_neighbor_r[i][D3.scan(cells.r_neighbor_r[i], (a, b) => h[a].CompareTo(h[b]))]; // downhill cell
                //msg.Add($"{i} {cells.fl[i]} {min} {x} {y} {cells.g[i]} {grid.cells.prec[cells.g[i]]}");
                var cf = features[cells.f[i]]; // current cell feature
                if (cf.river != 0 && cf.river != cells.r[i])
                {
                    cells.fl[i] = 0;
                }

                if (cells.fl[i] < 30)
                {
                    if (h[min] >= 20) cells.fl[min] += cells.fl[i];
                    continue; // flux is too small to operate as river
                }

                // Proclaim a new river
                if (cells.r[i] == 0)
                {
                    cells.r[i] = (ushort)riverNext;
                    riversData.Add(new RiverData() { river = riverNext, cell = i, x = x, y = y });
                    riverNext++;
                }

                if (cells.r[min] != 0)
                { // downhill cell already has river assigned
                    if (cells.fl[min] < cells.fl[i])
                    {
                        cells.conf[min] = (byte)cells.fl[min]; // mark confluence
                        if (h[min] >= 20) riversData.Find(r => r.river == cells.r[min]).parent = cells.r[i]; // min river is a tributary of current river
                        cells.r[min] = cells.r[i]; // re-assign river if downhill part has less flux
                    }
                    else
                    {
                        cells.conf[min] += (byte)cells.fl[i]; // mark confluence
                        if (h[min] >= 20) riversData.Find(r => r.river == cells.r[i]).parent = cells.r[min]; // current river is a tributary of min river
                    }
                }
                else cells.r[min] = cells.r[i]; // assign the river to the downhill cell

                double nx = p[min][0], ny = p[min][1];
                if (h[min] < 20)
                {
                    // pour water to the sea haven
                    riversData.Add(new RiverData() { river = cells.r[i], cell = cells.haven[i], x = nx, y = ny });
                }
                else
                {
                    var mf = features[cells.f[min]]; // feature of min cell
                    if (mf.type == "lake")
                    {
                        if (mf.river == 0 || cells.fl[i] > mf.flux)
                        {
                            mf.river = cells.r[i]; // pour water to temporaly elevated lake
                            mf.flux = cells.fl[i]; // entering flux
                        }
                    }
                    cells.fl[min] += cells.fl[i]; // propagate flux
                    riversData.Add(new RiverData() { river = cells.r[i], cell = min, x = nx, y = ny }); // add next River segment
                }
            }
            //DebugHelper.SaveArray("drainWater.txt", msg);
            return riversData.ToArray();
        }

        private void defineRivers(int riverNext, RiverData[] riversData)
        {
            var rivers = new List<Grid.River>();// rivers data
            var riverPaths = new List<Grid.RiverPath>(); // temporary data for all rivers

            //var msg = new List<string>();
            //msg.Add("" + Random.NextDouble());
            //foreach (var r in riversData) msg.Add($"{r.river} {r.x} {r.y}");

            for (var r = 1; r <= riverNext; r++)
            {
                var riverSegments = riversData.Where(d => d.river == r).ToArray();

                if (riverSegments.Length > 2)
                {
                    var riverEnhanced = addMeandring(riverSegments);
                    var width = Utils.rn(.8 + Random.NextDouble() * .4, 1); // river width modifier
                    var increment = Utils.rn(.8 + Random.NextDouble() * .6, 1); // river bed widening modifier
                    //msg.Add($"{r} {riverSegments.Length} {width} {increment} {riverEnhanced[riverEnhanced.Length - 1].x},{riverEnhanced[riverEnhanced.Length - 1].y}");

                    double length;
                    var path = getPath(riverEnhanced, width, increment, out length);

                    riverPaths.Add(new Grid.RiverPath()
                    {
                        r = r,
                        path = path,
                        width = width,
                        increment = increment
                    });

                    var parent = Math.Max(riverSegments[0].parent, 0);
                    rivers.Add(new Grid.River()
                    {
                        i = r,
                        parent = parent,
                        length = length,
                        source = riverSegments[0].cell,
                        mouth = Utils.last(riverSegments).cell
                    });
                }
                else
                {
                    // remove too short rivers
                    var ss = riverSegments.Where(s => cells.r[s.cell] == r);
                    foreach (var s in ss) { cells.r[s.cell] = 0; }
                }
            }
            pack.rivers = rivers.ToArray();
            pack.riverPaths = riverPaths.ToArray();
            //DebugHelper.SaveArray("defineRivers.txt", msg);
        }

        class RiverEnhanced { public double x, y; public int c; }
        // add more river points on 1/3 and 2/3 of length
        private RiverEnhanced[] addMeandring(RiverData[] segments, double rndFactor = 0.3)
        {
            var riverEnhanced = new List<RiverEnhanced>();// to store enhanced segments
            var side = 1;// to control meandring direction

            for (var s = 0; s < segments.Length; ++s)
            {
                double sX = segments[s].x, sY = segments[s].y; // segment start coordinates
                var c = pack.cells.conf[segments[s].cell]; // if segment is river confluence
                riverEnhanced.Add(new RiverEnhanced() { x = sX, y = sY, c = c });

                if (s + 1 == segments.Length) break; // do not enhance last segment

                double eX = segments[s + 1].x, eY = segments[s + 1].y; // segment end coordinates
                double angle = Math.Atan2(eY - sY, eX - sX);
                double sin = Math.Sin(angle), cos = Math.Cos(angle);
                double serpentine = 1.0 / (s + 1) + 0.3;
                double meandr = serpentine + Random.NextDouble() * rndFactor;
                if (Utils.P(.5)) side *= -1; // change meandring direction in 50%

                var dist2 = Math.Pow(eX - sX, 2) + Math.Pow(eY - sY, 2);
                // if dist2 is big or river is small add extra points at 1/3 and 2/3 of segment
                if (dist2 > 64 || (dist2 > 16 && segments.Length < 6))
                {
                    var p1x = (sX * 2 + eX) / 3 + side * -sin * meandr;
                    var p1y = (sY * 2 + eY) / 3 + side * cos * meandr;
                    if (Utils.P(.2)) side *= -1; // change 2nd extra point meandring direction in 20%
                    var p2x = (sX + eX * 2) / 3 + side * sin * meandr;
                    var p2y = (sY + eY * 2) / 3 + side * cos * meandr;
                    riverEnhanced.Add(new RiverEnhanced() { x = p1x, y = p1y });
                    riverEnhanced.Add(new RiverEnhanced() { x = p2x, y = p2y });
                }
                // if dist is medium or river is small add 1 extra middlepoint
                else if (dist2 > 16 || segments.Length < 6)
                {
                    var p1x = (sX + eX) / 2 + side * -sin * meandr;
                    var p1y = (sY + eY) / 2 + side * cos * meandr;
                    riverEnhanced.Add(new RiverEnhanced() { x = p1x, y = p1y });
                }
            }

            return riverEnhanced.ToArray();
        }

        private double[][] getPath(RiverEnhanced[] points, double width /*= 1*/, double increment /*= 1*/, out double riverLength)
        {
            double offset = 0, extraOffset = .1; // starting river width (to make river source visible)
            riverLength = points.Select( // summ of segments length
                (v, i) => i > 0 ? Utils.hypot(v.x - points[i - 1].x, v.y - points[i - 1].y) : 0).Sum();

            var widening = Utils.rn((1000 + (riverLength * 30)) * increment);
            var last = points.Length - 1;
            var factor = riverLength / points.Length;

            // store points on both sides to build a valid polygon
            var riverPointsLeft = new List<double[]>();
            var riverPointsRight = new List<double[]>();

            // first point
            double x = points[0].x, y = points[0].y; int c;
            double angle = Math.Atan2(y - points[1].y, x - points[1].x);
            double sin = Math.Sin(angle), cos = Math.Cos(angle);
            double xLeft = x + -sin * extraOffset, yLeft = y + cos * extraOffset;
            riverPointsLeft.Add(new double[] { xLeft, yLeft });
            double xRight = x + sin * extraOffset, yRight = y + -cos * extraOffset;
            riverPointsRight.Insert(0, new double[] { xRight, yRight });

            // middle points
            for (var p = 1; p < last; ++p)
            {
                x = points[p].x; y = points[p].y; c = points[p].c;
                double xPrev = points[p - 1].x, yPrev = points[p - 1].y;
                double xNext = points[p + 1].x, yNext = points[p + 1].y;
                angle = Math.Atan2(yPrev - yNext, xPrev - xNext);
                sin = Math.Sin(angle); cos = Math.Cos(angle);
                offset = (Math.Atan(Math.Pow(p * factor, 2) / widening) / 2 * width) + extraOffset;


                var confOffset = Math.Atan(c * 5 / widening);
                extraOffset += confOffset;
                xLeft = x + -sin * offset; yLeft = y + cos * (offset + confOffset);
                riverPointsLeft.Add(new double[] { xLeft, yLeft });
                xRight = x + sin * offset; yRight = y + -cos * offset;
                riverPointsRight.Insert(0, new double[] { xRight, yRight });
            }

            // end point
            x = points[last].x; y = points[last].y; c = points[last].c;
            if (c != 0) extraOffset += Math.Atan(c * 10 / widening); // add extra width on river confluence
            angle = Math.Atan2(points[last - 1].y - y, points[last - 1].x - x);
            sin = Math.Sin(angle); cos = Math.Cos(angle);
            xLeft = x + -sin * offset; yLeft = y + cos * offset;
            riverPointsLeft.Add(new double[] { xLeft, yLeft });
            xRight = x + sin * offset; yRight = y + -cos * offset;
            riverPointsRight.Insert(0, new double[] { xRight, yRight });

            riverPointsRight.AddRange(riverPointsLeft);
            return riverPointsRight.Select(p => Utils.rn(p, 2)).ToArray();
        }

    }
}
