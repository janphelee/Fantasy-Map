using System;
using System.Collections.Generic;
using SkiaSharp;
using static Janphe.Utils;
using SKSvg = SkiaSharp.Extended.Svg.SKSvg;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        public static List<double[]> poissonDiscSampler(double x0, double y0, double x1, double y1, double r, int k = 3) // mbostock's poissonDiscSampler
        {
            if (!(x1 >= x0) || !(y1 >= y0) || !(r > 0))
                throw new Exception();

            var width = x1 - x0;
            var height = y1 - y0;
            var r2 = r * r;
            var r2_3 = 3 * r2;
            var cellSize = r * Math.Sqrt(0.5);
            var gridWidth = (int)Math.Ceiling(width / cellSize);
            var gridHeight = (int)Math.Ceiling(height / cellSize);
            var grid = new double[gridWidth * gridHeight][];
            var queue = new List<double[]>();

            var poisson = new List<double[]>();

            sample(width / 2, height / 2);
            pick();

            return poisson;

            void sample(double x, double y)
            {
                var i = gridWidth * (int)(y / cellSize) + (int)(x / cellSize);
                grid[i] = new double[] { x, y };
                queue.push(grid[i]);
                //return new double[] { x + x0, y + y0 };
                poisson.push(new double[] { x + x0, y + y0 });
            }
            void pick()
            {
                while (queue.Count > 0)
                {
                    var i = (int)(Random.NextDouble() * queue.Count);
                    var parent = queue[i];

                    for (var j = 0; j < k; ++j)
                    {
                        var a = 2 * Math.PI * Random.NextDouble();
                        var rj = Math.Sqrt(Random.NextDouble() * r2_3 + r2);
                        var x = parent[0] + rj * Math.Cos(a);
                        var y = parent[1] + rj * Math.Sin(a);
                        if (0 <= x && x < width && 0 <= y && y < height && far(x, y))
                        {
                            sample(x, y);
                            pick();
                        }
                    }
                    if (queue.Count == 0)
                        break;

                    var pp = queue.pop();
                    if (i < queue.Count)
                        queue[i] = pp;
                }
            }

            bool far(double x, double y)
            {
                var i = (int)(x / cellSize);
                var j = (int)(y / cellSize);
                var i0 = Math.Max(i - 2, 0);
                var j0 = Math.Max(j - 2, 0);
                var i1 = Math.Min(i + 3, gridWidth);
                var j1 = Math.Min(j + 3, gridHeight);

                for (var _jj = j0; _jj < j1; ++_jj)
                {
                    var o = _jj * gridWidth;
                    for (var _ii = i0; _ii < i1; ++_ii)
                    {
                        var s = grid[o + _ii];
                        if (s != null)
                        {
                            var dx = s[0] - x;
                            var dy = s[1] - y;
                            if (dx * dx + dy * dy < r2)
                                return false;
                        }
                    }
                }
                return true;
            }
        }

        private Dictionary<string, SKSvg> loaded = new Dictionary<string, SKSvg>();
        private SKSvg getPicture(string id)
        {
            if (loaded.ContainsKey(id))
                return loaded[id];

            var svg = new SKSvg();

            var filePath = $"images/reliefs/{id.slice(1)}.svg";
            App.LoadRes(filePath, d =>
            {
                svg.Load(d);
                loaded[id] = svg;
                Debug.Log($"{id} viewBox:{svg.ViewBox}");
            });

            return svg;
        }
        private void drawReliefIcons(SKCanvas canvas)
        {
            if (reliefs.Count == 0)
                generateReliefIcons();

            foreach (var r in reliefs)
            {
                var svg = getPicture(r.i);

                var sx = r.s / svg.ViewBox.Width;
                var sy = r.s / svg.ViewBox.Height;

                var mat = SKMatrix.MakeIdentity();
                mat.SetScaleTranslate(sx, sy, r.x, r.y);

                canvas.DrawPicture(svg.Picture, ref mat);
            }
        }

        private class Relief { public string i; public float x, y, s; }
        private List<Relief> reliefs = new List<Relief>();//t: type, c: cell, x: centerX, y: centerY, s: size;
        private void generateReliefIcons()
        {
            //var msg = new List<string>();

            var density = Options.terrainDensity;
            var size = Options.terrainSize * 1.6;
            var mod = .2 * size;

            var cells = pack.cells;

            foreach (var i in cells.i)
            {
                var height = cells.h[i];
                if (height < 20)
                    continue; // no icons on water
                if (0 != cells.r[i])
                    continue; // no icons on rivers
                var b = cells.biome[i];
                if (height < 50 && biomesData.iconsDensity[b] == 0)
                    continue; // no icons for this biome

                var polygon = pack.getGridPolygon(i);
                var x = D3.extent(polygon.map(p => p[0]));
                var y = D3.extent(polygon.map(p => p[1]));
                var e = new double[] { Math.Ceiling(x[0]), Math.Ceiling(y[0]), Math.Floor(x[1]), Math.Floor(y[1]) }; // polygon box

                if (height < 50)
                    placeBiomeIcons();
                else
                    placeReliefIcons();

                void placeBiomeIcons()
                {
                    var iconsDensity = biomesData.iconsDensity[b] / 100d;
                    var radius = 2d / iconsDensity / density;
                    if (Random.NextDouble() > iconsDensity * 10)
                        return;

                    var poisson = poissonDiscSampler(e[0], e[1], e[2], e[3], radius);
                    foreach (var pp in poisson)
                    {
                        double cx = pp[0], cy = pp[1];
                        if (!D3.polygonContains(polygon, pp))
                            continue;
                        var h = rn((4 + Random.NextDouble()) * size, 2);
                        var icon = getBiomeIcon(i, biomesData.icons[b]);
                        if (icon == "#relief-grass-1")
                            h *= 1.3f;

                        var d = new Relief() { i = icon, x = (float)rn(cx - h, 2), y = (float)rn(cy - h, 2), s = (float)h * 2 };
                        reliefs.push(d);
                        //msg.push($"{i} {b} {d.i} {d.x},{d.y} {d.s}");
                    }
                }
                void placeReliefIcons()
                {
                    var radius = 2d / density;
                    string icon;
                    double h;
                    getReliefIcon(i, height, out icon, out h);

                    var poisson = poissonDiscSampler(e[0], e[1], e[2], e[3], radius);
                    foreach (var pp in poisson)
                    {
                        double cx = pp[0], cy = pp[1];
                        if (!D3.polygonContains(polygon, pp))
                            continue;

                        var d = new Relief() { i = icon, x = (float)rn(cx - h, 2), y = (float)rn(cy - h, 2), s = (float)h * 2 };
                        reliefs.push(d);
                        //msg.push($"{i} {height} {d.i} {d.x},{d.y} {d.s}");
                    }
                }

            }
            //Debug.SaveArray("drawReliefIcons.txt", msg);

            void getReliefIcon(int i, int h, out string icon, out double outSize)
            {
                var temp = grid.cells.temp[pack.cells.g[i]];
                var type = h > 70 && temp < 0 ? "mountSnow" : h > 70 ? "mount" : "hill";

                icon = getIcon(type);
                outSize = h > 70 ? (h - 45) * mod : Math.Min(Math.Max((h - 40) * mod, 3), 6);
            }
        }

        private string getBiomeIcon(int i, string[] b)
        {
            var type = b[(int)Math.Floor(Random.NextDouble() * b.Length)];
            var temp = grid.cells.temp[pack.cells.g[i]];
            if (type == "conifer" && temp < 0)
                type = "coniferSnow";
            return getIcon(type);
        }

        private string getIcon(string type)
        {
            var set = !string.IsNullOrEmpty(Options.terrainSet) ? Options.terrainSet : "simple";
            if (set == "simple")
                return "#relief-" + getOldIcon(type) + "-1";
            if (set == "colored")
                return "#relief-" + type + "-" + getVariant(type);
            if (set == "gray")
                return "#relief-" + type + "-" + getVariant(type) + "-bw";
            return "#relief-" + getOldIcon(type) + "-1"; // simple
        }

        private int getVariant(string type)
        {
            switch (type)
            {
                case "mount":
                    return randi(2, 7);
                case "mountSnow":
                    return randi(1, 6);
                case "hill":
                    return randi(2, 5);
                case "conifer":
                    return 2;
                case "coniferSnow":
                    return 1;
                case "swamp":
                    return randi(2, 3);
                case "cactus":
                    return randi(1, 3);
                case "deadTree":
                    return randi(1, 2);
                default:
                    return 2;
            }
        }

        private string getOldIcon(string type)
        {
            switch (type)
            {
                case "mountSnow":
                    return "mount";
                case "vulcan":
                    return "mount";
                case "coniferSnow":
                    return "conifer";
                case "cactus":
                    return "dune";
                case "deadTree":
                    return "dune";
                default:
                    return type;
            }
        }
    }
}
