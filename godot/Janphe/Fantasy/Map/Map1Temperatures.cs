using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Janphe.Fantasy.Map
{
    using static Utils;

    internal class Map1Temperatures
    {
        private MapJobs _map { get; set; }
        private Grid grid { get; set; }
        private Grid.Cells cells { get; set; }
        private MapJobs.Coordinates mapCoordinates { get; set; }

        private double temperaturePoleInput { get; set; }
        private double heightExponentInput { get; set; }

        private int svgWidth, svgHeight;

        public Map1Temperatures(MapJobs map)
        {
            _map = map;
            grid = map.grid;
            cells = grid.cells;

            mapCoordinates = map.mapCoordinates;

            temperaturePoleInput = map.Options.TemperaturePoleInput;
            heightExponentInput = map.Options.HeightExponentInput;


            svgWidth = _map.Options.Width;
            svgHeight = _map.Options.Height;
        }

        public void calculateTemperatures()
        {
            var temperatureEquator = _map.Options.TemperatureEquator;
            //Debug.Log($"calculateTemperatures {temperatureEquator.min} {temperatureEquator.max} {temperatureEquator.value}");

            var temp = new sbyte[cells.i.Length];

            var tEq = _map.Options.TemperatureEquator.value;
            var tPole = +temperaturePoleInput;
            var tDelta = tEq - tPole;

            //var msg = new List<string>();
            //msg.Add($"{tEq} {tPole} {heightExponentInput}");

            var range = D3.range(0, cells.i.Length, (int)grid.cellsX);
            foreach (var r in range)
            {
                var y = grid.points[r][1];
                var lat = Math.Abs(mapCoordinates.latN - y / svgHeight * mapCoordinates.latT);
                var initTemp = tEq - lat / 90 * tDelta;
                for (var i = r; i < r + grid.cellsX; i++)
                {
                    temp[i] = (sbyte)(initTemp - convertToFriendly(cells.r_height[i]));
                    //msg.Add($"{r} {i} {temp[i]}");
                }
            }
            //DebugHelper.SaveArray("calculateTemperatures.txt", msg);
            cells.temp = temp;
        }

        // temperature decreases by 6.5 degree C per 1km
        private double convertToFriendly(double h)
        {
            if (h < 20)
                return 0;
            var exponent = +heightExponentInput;
            var height = Math.Pow(h - 18, exponent);
            return Utils.rn(height / 1000 * 6.5);
        }

        class Chain { public int t; public double[][] points; }
        class Lable { public float x, y; public int t; }
        private List<Chain> Chains;
        private List<Lable> Lables;
        private int[] Isolines;

        public void generate()
        {
            var temperatureEquator = _map.Options.TemperatureEquator;
            float tMax = temperatureEquator.max, tMin = temperatureEquator.min, delta = tMax - tMin;

            var cells = grid.cells;
            var vertices = grid.vertices;
            var n = cells.i.Length;

            var used = new BitArray(n);
            sbyte min = D3.min(cells.temp), max = D3.max(cells.temp);
            var step = Math.Max(Utils.round(Math.Abs(min - max) / 5f), 1);
            var isolines = D3.range(min + step, max, step);
            //Debug.Log($"temperature.generate {min} {max} {step} iso:{isolines.Length}");

            var chains = new List<Chain>();
            var labels = new List<Lable>();

            cells.i.forEach(i =>
            {
                var t = cells.temp[i];
                if (used[i] || !isolines.includes(t))
                    return;
                var start = findStart(i, t);
                if (start < 0)
                    return;

                used[i] = true;
                //debug.append("circle").attr("r", 3).attr("cx", vertices.p[start][0]).attr("cy", vertices.p[start][1]).attr("fill", "red").attr("stroke", "black").attr("stroke-width", .3);

                var chain = connectVertices(start, t);
                var relaxed = chain.filter((v, vi) => vi % 4 == 0 || vertices.c[v].some(c => c >= n));
                if (relaxed.Count() < 6)
                    return;
                var points = relaxed.map(v => vertices.p[v]).ToArray();
                chains.push(new Chain() { t = t, points = points });
                addLabel(points, t);
            });
            Chains = chains;
            Lables = labels;
            Isolines = isolines;

            int findStart(int i, int t)
            {
                if (cells.b[i])
                    return cells.v[i].find(v => vertices.c[v].some(c => c >= n));
                var vi = cells.c[i].
                    findIndex(c => cells.temp[c] < t || 0 == cells.temp[c]);
                //Debug.Log($"findStart i:{i} t:{t} vi:{vi} vi.len{cells.v[i].Length}");
                if (vi < 0)
                    return vi;
                return cells.v[i][vi];
            }

            List<int> connectVertices(int start, int t)
            {
                var chain = new List<int>();
                for (int i = 0, current = start; i == 0 || current != start && i < 20000; i++)
                {
                    var prev = chain.Count > 0 ? chain[chain.Count - 1] : -1;
                    chain.push(current);
                    var c = vertices.c[current];

                    c.filter(ci =>
                    {
                        //Debug.Log($"cells.temp:{cells.temp.Length} ci:{ci} t:{t}");
                        return ci < n && cells.temp[ci] == t;
                    }).forEach(ci =>
                    {
                        //Debug.Log($"used:{used.Length} ci:{ci}");
                        used[ci] = true;
                    });

                    var c0 = c[0] >= n || cells.temp[c[0]] < t;
                    var c1 = c[1] >= n || cells.temp[c[1]] < t;
                    var c2 = c[2] >= n || cells.temp[c[2]] < t;
                    var v = vertices.v[current]; // neighboring vertices
                    if (v[0] != prev && c0 != c1)
                        current = v[0];
                    else if (v[1] != prev && c1 != c2)
                        current = v[1];
                    else if (v[2] != prev && c0 != c2)
                        current = v[2];
                    if (current == chain[chain.Count - 1])
                    { /*Debug.Log("Next vertex is not found");*/ break; }
                }
                chain.push(start);
                return chain;
            }

            void addLabel(double[][] points, int t)
            {
                var c = svgWidth / 2f;// map center x coordinate
                                      // add label on isoline top center
                var ii = D3.scan(points, (a, b) => (int)((a[1] - b[1]) + (Math.Abs(a[0] - c) - Math.Abs(b[0] - c)) / 2f));
                var tc = points[ii];
                pushLabel(tc[0], tc[1], t);

                // add label on isoline bottom center
                if (points.Length > 20)
                {
                    ii = D3.scan(points, (a, b) => (int)((b[1] - a[1]) + (Math.Abs(a[0] - c) - Math.Abs(b[0] - c)) / 2));
                    var bc = points[ii];
                    var dist2 = (tc[1] - bc[1]).pow(2) + (tc[0] - bc[0]).pow(2);// square distance between this and top point
                    if (dist2 > 100)
                        pushLabel(bc[0], bc[1], t);
                }
            }

            void pushLabel(double x, double y, int t)
            {
                if (x < 20 || x > svgWidth - 20)
                    return;
                if (y < 20 || y > svgHeight - 20)
                    return;
                labels.push(new Lable() { x = (float)x, y = (float)y, t = t });
            }
        }

        public void draw(SKCanvas canvas, Func<IList<SKPoint>, SKPath> curve)
        {
            var scheme = D3.Spectral;
            var temperatureEquator = _map.Options.TemperatureEquator;
            var tMax = temperatureEquator.max;
            var tMin = temperatureEquator.min;
            var delta = tMax - tMin;
            var min = D3.min(cells.temp);

            var paint = new SKPaint() { IsAntialias = true };
            var opacity = 0.3f;
            var strokeWidth = 1.8f;

            paint.Color = scheme(1 - (min - tMin) / delta).Opacity(opacity).SK();
            canvas.DrawRect(0, 0, svgWidth, svgHeight, paint);

            Isolines.forEach(t =>
            {
                var chain = Chains.filter(c => c.t == t).map(c => c.points);
                if (0 == chain.Count())
                    return;

                var fill = scheme(1 - (t - tMin) / delta).Opacity(opacity);
                var stroke = fill.darker(0.2f);

                var points = new List<SKPoint>();
                chain.forEach(pp => points.AddRange(pp.map(p => p.SK())));
                var path = curve(points);

                paint.Color = fill.SK();
                paint.Style = SKPaintStyle.Fill;
                canvas.DrawPath(path, paint);

                paint.Color = stroke.SK();
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = strokeWidth;
                canvas.DrawPath(path, paint);
            });

            paint.Style = SKPaintStyle.Fill;
            paint.Color = SKColors.Black;
            Lables.forEach(d =>
            {
                canvas.DrawText(convertTemperature(d.t), d.x, d.y, paint);
            });
        }

        public string convertTemperature(double c)
        {
            var temperatureScale = _map.Options.TemperatureScale;
            switch (temperatureScale)
            {
                case "°C":
                    return c + "°C";
                case "°F":
                    return rn(c * 9 / 5 + 32) + "°F";
                case "K":
                    return rn(c + 273.15) + "K";
                case "°R":
                    return rn((c + 273.15) * 9 / 5) + "°R";
                case "°De":
                    return rn((100 - c) * 3 / 2) + "°De";
                case "°N":
                    return rn(c * 33 / 100) + "°N";
                case "°Ré":
                    return rn(c * 4 / 5) + "°Ré";
                case "°Rø":
                    return rn(c * 21 / 40 + 7.5) + "°Rø";
                default:
                    return c + "°C";
            }
        }
    }
}
