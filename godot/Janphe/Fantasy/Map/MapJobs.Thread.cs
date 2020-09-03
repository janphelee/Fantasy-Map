using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Janphe.Utils;
using static Janphe.PointsSelection;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        private long _lastTicks;
        private double elapsed(Stopwatch watcher)
        {
            var deltaMs = rn((watcher.ElapsedTicks - _lastTicks) / 10000f, 2);
            _lastTicks = watcher.ElapsedTicks;
            return deltaMs;
        }

        private Map1OceanLayers map1OceanLayers;
        private Map1Temperatures map1Temperatures;
        private Map4Coastline map4Coastline;
        private Map5BiomesSystem map5Biomes;
        private Map2Precipitation map2Precipitation;
        private Map5Cultures map5Cultures;
        private Map6Religions map6Religions;
        private Map6BurgsAndStates map6BurgsAndStates;

        private void generate(Stopwatch watcher)
        {
            Random.Seed(Options.MapSeed);

            biomesData = Map5BiomesSystem.applyDefaultBiomesSystem();
            nameBases = NamesGenerator.getNameBases();
            Names = new NamesGenerator(this);
            Routes = new Map6Routes(this);

            grid = new Grid();
            _placePoints();
            Debug.Log($"1 placePoints {Random.NextDouble()} number:{Options.PointsNumber} => points:{grid.points.Count}");

            _calculateVoronoi(grid, grid.points, grid.boundary);
            Debug.Log($"2 calculateVoronoi {Random.NextDouble()}");

            new HeightmapGenerator(this).generate();
            Debug.Log($"3 HeightmapGenerator {Random.NextDouble()}");

            var feature = new Map1Features(this);
            feature.markFeatures();
            Debug.Log($"4 markFeatures {Random.NextDouble()}");

            feature.openNearSeaLakes();
            Debug.Log($"5 openNearSeaLakes {Random.NextDouble()}");

            map1OceanLayers = new Map1OceanLayers(this);
            map1OceanLayers.generate();
            Debug.Log($"6 Map1OceanLayers {Random.NextDouble()}");

            double mapSize = 0, latitude = 0;
            defineMapSize(Options.MapTemplate, grid, ref mapSize, ref latitude);
            Debug.Log($"7 defineMapSize {Random.NextDouble()}");

            mapCoordinates = calculateMapCoordinates(Options.Width, Options.Height, mapSize, latitude);
            Debug.Log($"8 calculateMapCoordinates {Random.NextDouble()}");

            map1Temperatures = new Map1Temperatures(this);
            map1Temperatures.calculateTemperatures();
            //Debug.SaveArray("temp.txt", grid.cells.temp);
            map1Temperatures.generate();
            Debug.Log($"9 calculateTemperatures {Random.NextDouble()} {Options.TemperatureEquator.value} {Options.TemperaturePoleInput} {Options.HeightExponentInput}");

            map2Precipitation = new Map2Precipitation(this);
            map2Precipitation.generatePrecipitation();
            Debug.Log("map2Precipitation winds => " + Debug.toString(Options.WindsInput));
            Debug.Log($"10 generatePrecipitation {Random.NextDouble()} modifier:{Options.PrecipitationInput / 100d}");
            //Debug.SaveArray("prec.txt", grid.cells.prec);
            //Debug.SaveArray("grid.cells.h.txt", grid.cells.h);

            pack = new Grid();
            reGraph();
            Debug.Log($"11 reGraph {Random.NextDouble()}");
            //Debug.SaveArray("pack.cells.area.txt", pack.cells.area);

            new Map3Features(this).reMarkFeatures();

            map4Coastline = new Map4Coastline(this);
            map4Coastline.generate();
            Debug.Log($"12 drawCoastline {Random.NextDouble()}");

            new Map4Lakes(this).elevateLakes();
            Debug.Log($"13 elevateLakes {Random.NextDouble()}");
            //Debug.SaveArray("Map4Lakes.h.txt", pack.cells.r_height);

            Random.Seed(Options.MapSeed);
            var rivers = new Map4Rivers(this);
            rivers.generate();
            Debug.Log($"14 Map4Rivers {Random.NextDouble()}");

            map5Biomes = new Map5BiomesSystem(this);
            map5Biomes.defineBiomes();
            Debug.Log($"15 defineBiomes {Random.NextDouble()}");

            map5Biomes.rankCells();
            map5Biomes.generate();
            //Debug.SaveArray("pack.cells.biome.txt", pack.cells.biome);
            //Debug.SaveArray("pack.cells.s.txt", pack.cells.s);
            //Debug.SaveArray("pack.cells.pop.txt", pack.cells.pop);
            Debug.Log($"16 rankCells {Random.NextDouble()} rankCells: {elapsed(watcher)}ms");

            map5Cultures = new Map5Cultures(this);
            map5Cultures.generate();
            Debug.Log($"17 Cultures {Random.NextDouble()} cultures.generate: {elapsed(watcher)}ms");

            map5Cultures.expand();
            Debug.Log($"18 Cultures.expand {Random.NextDouble()} neutralInput:{Options.NeutralInput}");
            //Debug.SaveArray("pack.cells.culture.txt", pack.cells.culture);

            var burgs = map6BurgsAndStates = new Map6BurgsAndStates(this);
            burgs.generate();
            Debug.Log($"19 Map6BurgsAndStates.generate {Random.NextDouble()} {elapsed(watcher)}ms");

            map6Religions = new Map6Religions(this);
            map6Religions.generate();
            Debug.Log($"20 Map6Religions.generate {Random.NextDouble()} {elapsed(watcher)}ms");

            burgs.defineStateForms();
            burgs.generateProvinces();
            burgs.defineBurgFeatures();
            Debug.Log($"21 Map6BurgsAndStates.defineBurgFeatures {Random.NextDouble()} {elapsed(watcher)}ms");

            burgs.generateProvincesPath();
            burgs.generateStatesPath();
            burgs.generateStateLabels();
            burgs.generateBorders();
            Debug.Log($"22 Map6BurgsAndStates.drawStateLabels {Random.NextDouble()} {elapsed(watcher)}ms");
        }

        protected override void process(Action<long> callback)
        {
            var watcher = new Stopwatch();
            watcher.Start();

            if (grid == null)
                generate(watcher);

            draw();

            watcher.Stop();
            callback?.Invoke(watcher.ElapsedMilliseconds);
        }

        private void reGraph()
        {
            var newP = new List<double[]>();
            var newG = new List<int>();
            var newH = new List<byte>();
            {
                //DebugHelper.SaveArray("grid.cells.i.txt", grid.cells.i);
                //DebugHelper.SaveArray("grid.cells.t.txt", grid.cells.t);
                //DebugHelper.SaveArray("grid.cells.b.txt", grid.cells.r_near_border.Select(b => b ? 1 : 0).ToArray());

                var cells = grid.cells;
                var points = grid.points;
                var features = grid.features;
                var spacing2 = grid.spacing * grid.spacing;

                foreach (var i in cells.i)
                {
                    var height = cells.r_height[i];
                    var type = cells.t[i];
                    if (height < 20 && type != -1 && type != -2)
                        continue; // exclude all deep ocean points
                    if (type == -2 && (i % 4 == 0 || features[cells.f[i]].type == "lake"))
                        continue; // exclude non-coastal lake points
                    double x = points[i][0], y = points[i][1];

                    addNewPoint(i, height, x, y);// add point to array
                                                 // add additional points for cells along coast
                    if (type == 1 || type == -1)
                    {
                        if (cells.r_near_border[i])
                            continue; // not for near-border cells

                        foreach (var e in cells.r_neighbor_r[i])
                        {
                            if (i > e)
                                continue;
                            if (cells.t[e] == type)
                            {
                                var dist2 = Math.Pow(y - points[e][1], 2) + Math.Pow(x - points[e][0], 2);
                                if (dist2 < spacing2)
                                    continue; // too close to each other

                                var x1 = (x + points[e][0]) / 2;// rn(, 1);
                                var y1 = (y + points[e][1]) / 2;// rn(, 1);
                                x1 = rn(x1, 4);
                                y1 = rn(y1, 4);
                                addNewPoint(i, height, x1, y1);
                            }
                        }
                    }
                }

                void addNewPoint(int i, byte height, double x, double y)
                {
                    newP.Add(new double[] { x, y });
                    newG.Add(i);
                    newH.Add(height);
                }
            }
            {
                //Debug.SaveArray("addOldPoint.txt", grid.points);
                //Debug.SaveArray("addNewPoint.txt", newP);
                //var msg = new List<string>();

                pack.voronoi = _calculateVoronoi(pack, newP, grid.boundary);

                var cells = pack.cells;
                cells.r_points = newP.ToArray();
                cells.g = newG.ToArray();
                cells.q = // points quadtree for fast search
                    D3.quadtree(cells.r_points.Select((p, d) => new D3.Quadtree.Value(p[0], p[1], d)).ToArray());
                cells.r_height = newH.ToArray();
                cells.area = // cell area
                    cells.i.Select(i =>
                    {
                        var pp = pack.getGridPolygon(i);
                        var aa = D3.polygonArea(pp);
                        //msg.push($"i:{i} area:{aa} {pp.join(" ", ",")}");
                        return (ushort)Math.Abs(aa);
                    }).ToArray();

                //Debug.SaveArray("pack.cells.area2.txt", msg);
                //Debug.SaveArray("pack.cells.v.txt", pack.cells.v);
            }
        }

        private void _placePoints()
        {
            var denisty = Options.PointsNumber;
            var width = Options.Width;
            var height = Options.Height;

            double cellsDesired = 10000 * denisty; // generate 10k points for each densityInput point
            double spacing = rn(Math.Sqrt(width * height / cellsDesired), 2); // spacing between points before jirrering
            Debug.Log($"_placePoints width:{width} height:{height} denisty:{denisty} cellsDesired:{cellsDesired} spacing:{spacing}");

            grid.boundary = getBoundaryPoints(width, height, spacing);
            grid.points = getJitteredGrid(width, height, spacing); // jittered square grid
            grid.cellsX = Math.Floor((width + 0.5 * spacing) / spacing);
            grid.cellsY = Math.Floor((height + 0.5 * spacing) / spacing);
            grid.spacing = spacing;
        }

        private static Voronoi _calculateVoronoi(Grid grid, List<double[]> points, List<double[]> boundary)
        {
            var n = points.Count;
            var allPoints = new List<double[]>(points);
            if (boundary != null)
                allPoints.AddRange(boundary);

            var delauny = fromPoints(allPoints);
            var voronoi = new Voronoi()
            {
                s_triangles_r = delauny.triangles,
                s_halfedges_s = delauny.halfedges,
            };

            var voronoiCell = new VoronoiCells(voronoi, allPoints, n);
            grid.cells = voronoiCell.cells;
            grid.cells.i = D3.range(n);
            grid.vertices = voronoiCell.vertices;

            return voronoi;
        }

        // define map size and position based on template and random factor
        private static void defineMapSize(string mapTemplate, Grid grid, ref double mapSizeOutput, ref double latitudeOutput)
        {
            var template = mapTemplate;// heightmap template
            var ret = getSizeAndLatitude();
            //if (!locked("mapSize")) mapSizeOutput.value = mapSizeInput.value = size;
            //if (!locked("latitude")) latitudeOutput.value = latitudeInput.value = latitude;
            mapSizeOutput = ret[0];
            latitudeOutput = ret[1];
            Debug.Log($"defineMapSize { mapSizeOutput} { latitudeOutput}");

            double[] getSizeAndLatitude()
            {
                var part = Array.Exists(grid.features, f => f != null && f.land && f.border);// if land goes over map borders
                var max = part ? 85 : 100;// max size
                var lat = part ? gauss(P(.5) ? 30 : 70, 15, 20, 80) : gauss(50, 20, 15, 85);// latiture shift

                if (!part)
                {
                    var temp = new double[] { 100, 50 };
                    if (template == "Pangea")
                        return temp;
                    if (template == "Shattered" && P(.7))
                        return temp;
                    if (template == "Continents" && P(.5))
                        return temp;
                    if (template == "Archipelago" && P(.35))
                        return temp;
                    if (template == "High Island" && P(.25))
                        return temp;
                    if (template == "Low Island" && P(.1))
                        return temp;
                }

                if (template == "Pangea")
                    return new double[] { gauss(75, 20, 30, max), lat };
                if (template == "Volcano")
                    return new double[] { gauss(30, 20, 10, max), lat };
                if (template == "Mediterranean")
                    return new double[] { gauss(30, 30, 15, 80), lat };
                if (template == "Peninsula")
                    return new double[] { gauss(15, 15, 5, 80), lat };
                if (template == "Isthmus")
                    return new double[] { gauss(20, 20, 3, 80), lat };
                if (template == "Atoll")
                    return new double[] { gauss(10, 10, 2, max), lat };

                return new double[] { gauss(40, 20, 15, max), lat }; // Continents, Archipelago, High Island, Low Island
            }
        }

        // calculate map position on globe
        private static Coordinates calculateMapCoordinates(int width, int height, double mapSize, double latitude)
        {
            var latShift = latitude;
            var latT = mapSize / 100 * 180;
            var latN = 90 - (180 - latT) * latShift / 100;
            var latS = latN - latT;

            var lon = Math.Min(latT / 2 * width / height, 180);
            Debug.Log($"calculateMapCoordinates {mapSize} {latShift} {latT} {latN} {latS} {lon}");

            return new Coordinates()
            {
                latT = latT,
                latN = latN,
                latS = latS,
                lonT = lon * 2,
                lonW = -lon,
                lonE = lon
            };
        }

    }
}
