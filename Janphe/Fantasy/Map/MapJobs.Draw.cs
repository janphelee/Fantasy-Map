using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        public SKBitmap Bitmap { get; private set; }
        private SKSurface _surface { get; set; }


        private float _sx = 1, _sy = 1;
        public void Scale(float sx, float sy) { _sx = sx; _sy = sy; }

        private float _tx = 0, _ty = 0;
        public void Translate(float x, float y) { _tx = x; _ty = y; }

        private void draw()
        {
            if (Bitmap == null)
            {
                var info = new SKImageInfo(Options.Width, Options.Height, SKColorType.Rgba8888);//指定颜色格式
                Bitmap = new SKBitmap(info);
                _surface = SKSurface.Create(info, Bitmap.GetPixels());
                //_surface = SKSurface.Create(info, Bitmap.GetPixels(), Bitmap.Info.RowBytes);
            }

            var canvas = _surface.Canvas;
            //canvas.Clear(new SKColor(128, 128, 128, 0));
            canvas.Clear();
            canvas.ResetMatrix();

            canvas.Scale(_sx, _sy);
            canvas.Translate(_tx, _ty);

            if (isLayersOn(Layers.opt_layers_texture))
            {
                drawOcean(canvas);//海购线
                drawLandmass(canvas);//陆地
            }

            if (isLayersOn(Layers.opt_layers_heightmap))
                drawHeightmap(canvas);

            if (isLayersOn(Layers.opt_layers_biomes))
                drawBiomes(canvas);

            if (isLayersOn(Layers.opt_layers_rivers))
                drawRivers(canvas);

            if (isLayersOn(Layers.opt_layers_temperature))
                map1Temperatures.draw(canvas, linePath);

            if (isLayersOn(Layers.opt_layers_precipitation))
                map2Precipitation.draw(canvas);

            if (isLayersOn(Layers.opt_layers_population))
                drawPopulation(canvas);

            if (isLayersOn(Layers.opt_layers_cultures))
                map5Cultures.draw(canvas, linePath);

            if (isLayersOn(Layers.opt_layers_religions))
                map6Religions.draw(canvas, linePath);

            if (isLayersOn(Layers.opt_layers_states))
            {
                map6BurgsAndStates.drawStates(canvas);
                map6BurgsAndStates.drawStateLabels(canvas);
            }

            if (isLayersOn(Layers.opt_layers_relief))
                drawReliefIcons(canvas);

            if (isLayersOn(Layers.opt_layers_cells))
                drawCells(canvas);
        }

        private float urbanization = 1;
        private void drawPopulation(SKCanvas canvas)
        {
            var cells = pack.cells;
            var burgs = pack.burgs;
            var p = cells.p;

            var paint = new SKPaint();
            paint.StrokeWidth = 1.6f;
            paint.StrokeCap = SKStrokeCap.Butt;

            paint.Color = SKColors.Blue;
            var rural = cells.i.filter(i => cells.pop[i] > 0);
            rural.forEach(i =>
            {
                var x = (float)p[i][0];
                var y = (float)p[i][1];
                canvas.DrawLine(x, y, x, y - cells.pop[i] / 8, paint);
            });

            paint.Color = SKColors.Red;
            var urban = burgs.filter(b => b != null && 0 != b.i && !b.removed);
            urban.forEach(b =>
            {
                var x = (float)b.x;
                var y = (float)b.y;
                var y2 = y - (float)b.population / 8 * urbanization;
                canvas.DrawLine(x, y, x, y2, paint);
            });
        }

        private void drawLandmass(SKCanvas canvas)
        {
            var features = pack.features;
            var chains = map4Coastline.Paths;
            var paint = new SKPaint() { IsAntialias = true };

            chains.Keys.forEach(f =>
            {

                var paths = chains[f];
                if (features[f].type == "lake")
                {
                    //landmask
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = Color.Black.SK();
                    paint.MaskFilter = null;
                    paths.ForEach(p => canvas.DrawPath(curvePath(p), paint));

                    //lakes
                    var lake = Lakes.find(l => l.name == features[f].group);
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = lake.fill.ToColor().Opacity(lake.opacity).SK();
                    paths.ForEach(p => canvas.DrawPath(curvePath(p), paint));

                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = lake.stroke.ToColor().Opacity(lake.opacity).SK();
                    paint.StrokeWidth = lake.strokeWidth;
                    paths.ForEach(p => canvas.DrawPath(curvePath(p), paint));
                }
                else
                {
                    //landmask
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = Color.White.SK();
                    paint.MaskFilter = null;
                    paths.ForEach(p => canvas.DrawPath(curvePath(p), paint));

                    //coastline
                    var g = features[f].group == "lake_island" ? "lake_island" : "sea_island";
                    var island = Islands.find(l => l.name == g);
                    paint.Style = SKPaintStyle.Stroke;
                    paint.Color = island.stroke.ToColor().Opacity(island.opacity).SK();
                    paint.StrokeWidth = island.strokeWidth;
                    //! TODO 滤镜怎么实现
                    paint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 0.2f);
                    paths.ForEach(p => canvas.DrawPath(curvePath(p), paint));
                }
            });
        }

        private void drawOcean(SKCanvas canvas)
        {
            var opacity = map1OceanLayers.Opacity;
            var limits = map1OceanLayers.Limits;
            var chains = map1OceanLayers.Paths;

            var c32 = "#ecf2f9".ToColor().Opacity(opacity);
            var color = c32.SK();

            var paint = new SKPaint();
            paint.Color = color;
            paint.Style = SKPaintStyle.Fill;
            paint.IsAntialias = true;

            limits.forEach(t =>
            {
                var paths = chains.filter(p => p.Key == t).map(c => c.Value);
                paths.forEach(pp =>
                {
                    //pp.forEach(p => canvas.DrawPoints(SKPointMode.Polygon, p, paint));
                    pp.forEach(p => canvas.DrawPath(curvePath(p), paint));
                });
            });
        }

        private void drawHeightmap(SKCanvas canvas)
        {
            var cells = pack.cells;
            var vertices = pack.vertices;
            var n = cells.i.Length;
            var used = new BitArray(n);
            var paths = new SKPath[101];

            var scheme = getColorScheme();
            var terracing = 0;
            var skip = 6;
            var simplification = 0;

            var currentLayer = 20;
            var heights = cells.i.sort((a, b) => cells.h[a] - cells.h[b]);
            heights.forEach(i =>
            {
                var h = cells.h[i];
                if (h > currentLayer)
                    currentLayer += skip;
                if (currentLayer > 100)
                    return; // no layers possible with height > 100
                if (h < currentLayer)
                    return;
                if (used[i])
                    return; // already marked

                var onborder = cells.c[i].some(c2 => cells.h[c2] < h);
                if (!onborder)
                    return;
                var vertex = cells.v[i].find(v2 => vertices.c[v2].some(c2 => cells.h[c2] < h));
                var chain = connectVertices(vertex, h);
                if (chain.Count < 3)
                    return;
                var points = simplifyLine(chain).map(v => vertices.p[v]);

                if (paths[h] == null)
                    paths[h] = new SKPath();

                var pp = points.map(p => new SKPoint((float)p[0], (float)p[1])).ToArray();
                //paths[h].AddPoly(pp);
                paths[h].AddPath(curvePath(pp));
                ;
            });

            var paint = new SKPaint();

            paint.Color = scheme(.8f);
            paint.IsAntialias = true;
            //canvas.DrawRect(0, 0, Options.Width, Options.Height, paint);

            for (var i = 20; i < 101; ++i)
            {
                if (paths[i] == null || paths[i].PointCount < 3)
                    continue;
                var color = getColor(i, scheme);
                if (terracing > 0)
                { }
                paint.Color = color;
                canvas.DrawPath(paths[i], paint);
            }

            IList<int> connectVertices(int start, byte h)
            {
                var chain = new List<int>();
                for (int i = 0, current = start; i == 0 || current != start && i < 20000; i++)
                {
                    var prev = chain.Count > 0 ? chain[chain.Count - 1] : -1; // previous vertex in chain
                    chain.push(current); // add current vertex to sequence
                    var c = vertices.c[current]; // cells adjacent to vertex
                    c.filter(_c => cells.h[_c] == h).forEach(_c => used[_c] = true);
                    var c0 = c[0] >= n || cells.h[c[0]] < h;
                    var c1 = c[1] >= n || cells.h[c[1]] < h;
                    var c2 = c[2] >= n || cells.h[c[2]] < h;
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

            IEnumerable<int> simplifyLine(IList<int> chain)
            {
                if (0 == simplification)
                    return chain;
                var _n = simplification + 1; // filter each nth element
                return chain.filter(i => i % _n == 0);
            }
        }

        private void drawBiomes(SKCanvas canvas)
        {
            var chains = map5Biomes.Paths;

            var paint = new SKPaint() { IsAntialias = true };
            paint.Style = SKPaintStyle.Fill;

            chains.forEach((d, i) =>
            {
                if (d == null)
                    return;
                paint.Color = biomesData.color[i].ToColor().SK();
                //d.forEach(pp => canvas.DrawPath(curvePath(pp), paint));

                var path = new SKPath();
                d.forEach(pp => path.AddPoly(pp));
                canvas.DrawPath(path, paint);
            });
        }

        private void drawRivers(SKCanvas canvas)
        {
            var chains = pack.riverPaths;

            var paint = new SKPaint() { IsAntialias = true };
            paint.Style = SKPaintStyle.Fill;
            paint.Color = "#5d97bb".ToColor().SK();

            chains.forEach((d, i) =>
            {
                canvas.DrawPath(curvePath(d.path.map(p => new SKPoint((float)p[0], (float)p[1])).ToArray()), paint);
            });
        }

        private void drawCells(SKCanvas canvas, float scale = 1)
        {
            var paint = new SKPaint();

            paint.Style = SKPaintStyle.Stroke;
            paint.StrokeWidth = 0.05f;
            paint.StrokeJoin = SKStrokeJoin.Round;
            paint.IsAntialias = true;

            var v = pack.voronoi;
            int n;
            BitArray used;

            if (isCellsOn(Cells.cells_side))
            {
                paint.Color = SKColors.Black;
                n = pack.cells.r_points.Length;
                used = new BitArray(v.numSides);
                for (var i = 0; i < used.Length; i++)
                {
                    if (used[i])
                        continue;

                    var r0 = v.s_begin_r(i);
                    var r1 = v.s_end_r(i);
                    if (r0 < 0 || r1 < 0 || r0 >= n || r1 >= n)
                        continue;

                    var p0 = pack.cells.r_points[r0];
                    var p1 = pack.cells.r_points[r1];

                    canvas.DrawLine((float)p0[0], (float)p0[1], (float)p1[0], (float)p1[1], paint);

                    used[i] = true;
                    used[v.s_opposite_s(i)] = true;
                }
            }

            if (isCellsOn(Cells.cells_line))
            {
                paint.Color = SKColors.White;
                n = pack.vertices.t_points.Length;
                used = new BitArray(v.numSides);
                for (var i = 0; i < used.Length; i++)
                {
                    if (used[i])
                        continue;

                    var v0 = v.s_inner_t(i);
                    var v1 = v.s_outer_t(i);
                    if (v0 < 0 || v1 < 0 || v0 >= n || v1 >= n)
                        continue;

                    var p0 = pack.vertices.t_points[v0];
                    var p1 = pack.vertices.t_points[v1];

                    canvas.DrawLine((float)p0[0], (float)p0[1], (float)p1[0], (float)p1[1], paint);

                    used[i] = true;
                    used[v.s_opposite_s(i)] = true;
                }
            }

            if (isCellsOn(Cells.cells_region))
            {
                paint.Style = SKPaintStyle.Fill;
                paint.Color = SKColors.Red;
                var rpp = pack.cells.r_points;
                //canvas.DrawPoints(SKPointMode.Points, rpp.Select(p => new SKPoint((float)p[0], (float)p[1])).ToArray(), paint);
                rpp.forEach(p => canvas.DrawCircle((float)p[0], (float)p[1], 0.5f, paint));
            }

            if (isCellsOn(Cells.cells_vert))
            {
                paint.Style = SKPaintStyle.Fill;
                paint.Color = SKColors.Blue;
                var vpp = pack.vertices.t_points;
                //canvas.DrawPoints(SKPointMode.Points, vpp.Select(p => new SKPoint((float)p[0], (float)p[1])).ToArray(), paint);
                vpp.forEach(p => canvas.DrawCircle((float)p[0], (float)p[1], 0.318f, paint));
            }
        }

        private Func<float, SKColor> getColorScheme()
        {
            var s = D3.Spectral;
            SKColor func(float t)
            {
                var c = s(t);
                return new SKColor((uint)c.ToArgb32());
                //return new SKColor(c.r8, c.g8, c.b8, c.a8);
            }
            return func;
        }
        private SKColor getColor(int value, Func<float, SKColor> scheme) => scheme(1 - (value < 20 ? value - 5 : value) / 100f);
        private SKPoint scale(SKPoint a, float b) => new SKPoint(a.X * b, a.Y * b);
        private SKPath curvePath(IList<SKPoint> points)
        {
            var path = new SKPath();

            var n = points.Count;
            var p0 = points[n - 1];
            var p1 = points[0];
            path.MoveTo(scale(p0 + p1, 0.5f));

            for (var i = 0; i < n; ++i)
            {
                p0 = points[i];
                p1 = i == n - 1 ? points[0] : points[i + 1];
                var p2 = scale(p0 + p1, 0.5f);
                //path.CubicTo(p0, p0, p2);
                path.ConicTo(p0, p2, 0.5f);
            }
            path.Close();

            return path;
        }

        private SKPath linePath(IList<SKPoint> points)
        {
            var path = new SKPath();
            path.MoveTo(points[0]);

            var n = points.Count;
            for (var i = 1; i < n; ++i)
            {
                path.LineTo(points[i]);
            }
            path.Close();

            return path;
        }

    }
}
