using System;
using System.Collections.Generic;
using System.Linq;
using static Janphe.D3;

namespace Janphe.Fantasy.Map
{
    internal class Grid
    {
        public class Burg
        {
            public int cell;
            public ushort i, state, culture, feature, capital, port;
            public double x, y, population;
            public string name;

            public bool removed = false;
            public Quadtree tree;

            // defineBurgFeatures 城堡, 市场, 城墙, 棚屋, 教堂
            public byte citadel, plaza, walls, shanty, temple;
        }
        public class Province
        {
            public int i, state, center, burg;
            public string name, formName, fullName, color;
            public bool removed = false;
            public double[] pole;
        }
        public class State
        {
            public int center;
            public ushort i, capital, culture;
            public double expansionism;
            public string name, color, type;
            public bool removed = false;

            public ushort cells, area, burgs;
            public double rural, urban;
            public ushort[] neighbors;

            public string[] diplomacy;

            public string form, formName, fullName;

            public List<ushort> provinces;
            public double[] pole;
        }
        public class Religion
        {
            public int i, culture, center, origin;
            public string name, code, color, type, form, deity;
            public bool removed;

            public string expansion;
            public double expansionism;

            public ushort cells, area;
            public double rural, urban;
        }
        public class Culture
        {
            public string name;
            public int i;
            public int @base;
            public double odd;
            public Func<int, double> sort { get; set; }

            public int center;
            public string color;
            public string type;
            public float expansionism;
            public int origin;
            public string code;

            public bool removed = false;
        }
        public class River
        {
            public int i { get; set; }
            public int parent { get; set; }
            public int source { get; set; }
            public int mouth { get; set; }
            public double length { get; set; }
        }
        public class RiverPath
        {
            public int r; public double[][] path; public double width, increment;
        }

        public class Feature
        {
            public int i { get; set; }
            public bool land { get; set; }
            public bool border { get; set; }
            public string type { get; set; }
            // pack ====================================
            public int cells { get; set; }
            public int firstCell { get; set; }
            public string group { get; set; }
            // =========================================
            public double area { get; set; }    //drawCoastline
            public int[] vertices { get; set; } //drawCoastline
                                                // ===========================================
            public int river { get; set; }
            public int flux { get; set; }
        }
        public class Cells
        {
            public int[][] v { get; set; }//v = cell vertices,
            public int[][] r_raycast_s { get; set; }
            public int[][] r_neighbor_r { get; set; }//c = adjacent cells,
            public bool[] r_near_border { get; set; } //b = near-border cell
            public bool[] b => r_near_border;

            public int[] i { get; set; }  //i= indices //TODO public ushort[] i { get; set; }
            public byte[] r_height { get; set; } //h = HeightmapGenerator

            public byte[] h { get { return r_height; } }
            public int[][] c { get { return r_neighbor_r; } }

            public ushort[] f { get; set; } //cell feature number
            public sbyte[] t { get; set; }  //cell type: 1 = land coast; -1 = water near coast;
            public sbyte[] temp { get; set; } // Map1Temperatures
            public byte[] prec { get; set; } // Map2Precipitation

            // pack ===========================
            public double[][] p { get { return r_points; } }
            public double[][] r_points { get; set; }//p = region coordinates
            public int[] g { get; set; }             // reGraph
            public Quadtree q { get; set; }       // reGraph
            public ushort[] area { get; set; }       // reGraph
            public int[] haven { get; set; } // reMarkFeatures
            public byte[] harbor { get; set; }

            // river...
            public ushort[] fl { get; set; }// water flux array
            public ushort[] r { get; set; }// rivers array
            public byte[] conf { get; set; }// confluences array
                                            // biome
            public byte[] biome { get; set; }
            public short[] s { get; set; }
            public float[] pop { get; set; }
            // culture
            public ushort[] culture { get; set; }

            // burgs && states
            public ushort[] burg { get; set; }
            public ushort[] road { get; set; }
            public ushort[] crossroad { get; set; }

            // Routes
            public ushort[] state { get; set; }
            public ushort[] religion { get; set; }
            public ushort[] province { get; set; }
        }
        public class Vertices
        {
            public double[][] p { get { return t_points; } }
            public double[][] t_points { get; set; }//p = vertex coordinates
            public int[][] v { get; set; }   //v = neighboring vertices
            public int[][] c { get; set; }   //c = adjacent cells

        }

        public List<double[]> boundary { get; set; }
        public List<double[]> points { get; set; }
        public double spacing { get; set; }
        public double cellsX { get; set; }
        public double cellsY { get; set; }

        public Cells cells { get; set; }
        public Vertices vertices { get; set; }
        public Feature[] features { get; set; }// MapFeatures
        public River[] rivers { get; set; }    // Map4Rivers
        public RiverPath[] riverPaths { get; set; }
        public Culture[] cultures { get; set; }

        public List<Burg> burgs { get; set; }

        public List<State> states { get; set; }
        public List<string[]> chronicle { get; set; }

        public List<Religion> religions { get; set; }
        public List<Province> provinces { get; set; }

        public int findGridCell(double x, double y)
        {
            var n = Math.Floor(Math.Min(y / spacing, cellsY - 1)) * cellsX + Math.Floor(Math.Min(x / spacing, cellsX - 1));
            return (int)n;
        }

        public int findCell(double x, double y, double radius = double.PositiveInfinity)
        {
            var found = cells.q.find(x, y, radius);
            return null != found ? found.v : -1;
        }

        public double[][] getGridPolygon(int i)
        {
            return cells.v[i].Select(v => vertices.t_points[v]).ToArray();
        }

        public IList<double[]> getFeaturePoints(int f)
        {
            var ff = features[f];
            if (ff == null)
                return null;

            var vchain = ff.vertices;
            if (vchain == null)
                return null;

            return vchain.Select(v => vertices.t_points[v]).ToList();
        }

        public bool isLand(int i) { return cells.r_height[i] >= 20; }

        public Voronoi voronoi { get; set; }
    }

}
