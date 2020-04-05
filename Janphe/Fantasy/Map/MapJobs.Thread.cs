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
        public Options Options { get; set; }

        private void initOptions()
        {
            Options = new Options()
            {
                MapSeed = 1,
                Width = 1153,
                Height = 717,
                MapTemplate = "Archipelago",

                PointsNumber = 1,
                PrecipitationInput = 127,
                WindsInput = new int[] { 270, 90, 225, 315, 135, 315 },
                TemperaturePoleInput = -26,//[-30,30]
                HeightExponentInput = 1.8f,//[1.5,2.1]

                CulturesNumber = 5,
                CulturesSet = "highFantasy",
                CulturesSet_DataMax = 23,

                NeutralInput = 1,
                PowerInput = 4,
                ReligionsNumber = 7,
            };
            Options.TemperatureEquator = new Options.Value() { min = -30, max = 30, value = 18 };
            Options.TemperatureScale = "°C";
        }

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
            //Debug.Log($"markFeatures: {elapsed(watcher)}ms");
            feature.openNearSeaLakes();
            Debug.Log($"5 openNearSeaLakes {Random.NextDouble()}");
            //Debug.Log($"openLakes: {elapsed(watcher)}ms");

            map1OceanLayers = new Map1OceanLayers(this);
            map1OceanLayers.generate();
            Debug.Log($"6 Map1OceanLayers {Random.NextDouble()}");
            //Debug.Log($"drawOceanLayers: {elapsed(watcher)}ms");

            double mapSize = 0, latitude = 0;
            defineMapSize(Options.MapTemplate, grid, ref mapSize, ref latitude);
            Debug.Log($"7 defineMapSize {Random.NextDouble()}");

            mapCoordinates = calculateMapCoordinates(Options.Width, Options.Height, mapSize, latitude);
            Debug.Log($"8 calculateMapCoordinates {Random.NextDouble()}");
            //Debug.Log(JsonUtility.ToJson(mapCoordinates));

            map1Temperatures = new Map1Temperatures(this);
            map1Temperatures.calculateTemperatures();
            map1Temperatures.generate();
            Debug.Log($"9 calculateTemperatures {Random.NextDouble()}");
            //Debug.Log($"calculateTemperatures: {elapsed(watcher)}ms");
            //DebugHelper.SaveArray("temp.txt", grid.cells.temp);

            map2Precipitation = new Map2Precipitation(this);
            map2Precipitation.generatePrecipitation();
            Debug.Log($"10 generatePrecipitation {Random.NextDouble()}");
            //Debug.Log($"generatePrecipitation: {elapsed(watcher)}ms");
            //DebugHelper.SaveArray("prec.txt", grid.cells.prec);

            pack = new Grid();
            //DebugHelper.SaveArray("grid.cells.h.txt", grid.cells.r_height);
            reGraph();
            Debug.Log($"11 reGraph {Random.NextDouble()}");
            //Debug.Log($"reGraph: {elapsed(watcher)}ms");
            //DebugHelper.SaveArray("pack.cells.h.txt", pack.cells.r_height);

            new Map3Features(this).reMarkFeatures();
            //Debug.Log($"reMarkFeatures: {elapsed(watcher)}ms");

            map4Coastline = new Map4Coastline(this);
            map4Coastline.generate();
            Debug.Log($"12 drawCoastline {Random.NextDouble()}");
            //Debug.Log($"drawCoastline: {elapsed(watcher)}ms");

            new Map4Lakes(this).elevateLakes();
            Debug.Log($"13 elevateLakes {Random.NextDouble()}");
            //Debug.Log($"elevateLakes: {elapsed(watcher)}ms");
            //DebugHelper.SaveArray("Map4Lakes.h.txt", pack.cells.r_height);

            Random.Seed(Options.MapSeed);
            var rivers = new Map4Rivers(this);
            rivers.generate();
            Debug.Log($"14 Map4Rivers {Random.NextDouble()}");
            //Debug.Log($"generateRivers: {elapsed(watcher)}ms");

            map5Biomes = new Map5BiomesSystem(this);
            map5Biomes.defineBiomes();
            Debug.Log($"15 defineBiomes {Random.NextDouble()}");
            //DebugHelper.SaveArray("pack.cells.biome.txt", pack.cells.biome);
            //Debug.Log($"defineBiomes: {elapsed(watcher)}ms");
            map5Biomes.rankCells();
            map5Biomes.generate();
            //DebugHelper.SaveArray("pack.cells.s.txt", pack.cells.s);
            //DebugHelper.SaveArray("pack.cells.pop.txt", pack.cells.pop);
            Debug.Log($"16 rankCells {Random.NextDouble()} rankCells: {elapsed(watcher)}ms");

            var cultures = new Map5Cultures(this);
            cultures.generate();
            Debug.Log($"17 Cultures {Random.NextDouble()} cultures.generate: {elapsed(watcher)}ms");

            cultures.expand();
            Debug.Log($"cultures.expand: {elapsed(watcher)}ms");
            Debug.Log($"18 Cultures.expand {Random.NextDouble()}");

            var burgs = new Map6BurgsAndStates(this);
            burgs.generate();
            Debug.Log($"19 Map6BurgsAndStates.generate: {elapsed(watcher)}ms");
            new Map6Religions(this).generate();
            burgs.defineStateForms();
            burgs.generateProvinces();
            burgs.defineBurgFeatures();
            Debug.Log($"20 Map6Religions.generate: {elapsed(watcher)}ms");

            //burgs.drawStates();
            //burgs.drawBorders();
            //burgs.drawStateLabels();
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
                                var x1 = rn((x + points[e][0]) / 2, 1);
                                var y1 = rn((y + points[e][1]) / 2, 1);
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
                //DebugHelper.SaveArray("addNewPoint.txt", newP);
                pack.voronoi = _calculateVoronoi(pack, newP, grid.boundary);

                var cells = pack.cells;
                cells.r_points = newP.ToArray();
                cells.g = newG.ToArray();
                cells.q = // points quadtree for fast search
                    D3.quadtree(cells.r_points.Select((p, d) => new D3.Quadtree.Value(p[0], p[1], d)).ToArray());
                cells.r_height = newH.ToArray();
                cells.area = // cell area
                    cells.i.Select(i => (ushort)Math.Abs(D3.polygonArea(pack.getGridPolygon(i)))).ToArray();

                //DebugHelper.SaveArray("pack.cells.i.txt", cells.i);
                //DebugHelper.SaveArray("pack.cells.h.txt", cells.r_height);
                //DebugHelper.SaveArray("pack.cells.c.txt", cells.c);
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
