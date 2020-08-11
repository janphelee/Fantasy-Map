using System.Collections.Generic;
using System.Linq;
using System;

namespace Janphe.Fantasy.Map
{
    using Cells = Grid.Cells;
    using Vertices = Grid.Vertices;

    internal class VoronoiCells
    {
        public Cells cells { get; private set; }
        public Vertices vertices { get; private set; }

        public Voronoi voronoi { get; private set; }

        public VoronoiCells(Voronoi voronoi, List<double[]> points, int numRegions)
        {
            var numSides = voronoi.numSides;
            var numTriangles = voronoi.numTriangles;

            cells = new Cells()//数组大小为区域数
            {
                v = new int[numRegions][],
                r_neighbor_r = new int[numRegions][],
                r_near_border = new bool[numRegions],
                //i = new int[numTriangles][],
            };
            vertices = new Vertices()//数组大小为三角数
            {
                t_points = new double[numTriangles][],
                v = new int[numTriangles][],
                c = new int[numTriangles][],
            };

            cells.r_raycast_s = voronoi.r_raycast_s(numRegions);

            int[] pointsOfTriangle(int t)
            {
                return Voronoi.t_circulate_s(t).Select(voronoi.s_begin_r).ToArray();
            }

            double[] triangleCenter(int t)
            {
                var vertices = pointsOfTriangle(t).Select(p => points[p]).ToArray();
                return circumcenter(vertices[0], vertices[1], vertices[2]);
            }

            int[] trianglesAdjacentToTriangle(int t)
            {
                var edges = Voronoi.t_circulate_s(t);
                return edges.Select((edge, index) =>
                {
                    var opposite = voronoi.s_opposite_s(edge);
                    return Voronoi.s_to_t(opposite);
                }).ToArray();
            }

            for (var side = 0; side < numSides; ++side)
            {
                var p = voronoi.s_begin_r(Voronoi.s_next_s(side));
                //if (p >= pointsN) Debug.Log($"e:{e} p:{p} >= pointsN:{pointsN}");
                //if (cells.c[p] != null) Debug.Log($"e:{e} cells.c[p] != null");
                if (p < numRegions && cells.r_neighbor_r[p] == null)
                {
                    var edges = voronoi.s_circulate_s(side);
                    // cell: adjacent vertex
                    cells.v[p] = edges.Select(Voronoi.s_to_t).ToArray();
                    // cell: adjacent valid cells
                    cells.r_neighbor_r[p] = edges.Select(voronoi.s_begin_r).Where(c => c < numRegions).ToArray();
                    // cell: is border
                    cells.r_near_border[p] = edges.Length > cells.r_neighbor_r[p].Length ? true : false;
                }

                var t = Voronoi.s_to_t(side);// numSides/3
                //if (vertices.p[t] != null) Debug.Log($"e:{e} t:{t} vertices.p[t] != null");
                if (vertices.t_points[t] == null)
                {
                    // vertex: coordinates
                    vertices.t_points[t] = triangleCenter(t);
                    // vertex: adjacent vertices
                    vertices.v[t] = trianglesAdjacentToTriangle(t);
                    // vertex: adjacent cells
                    vertices.c[t] = pointsOfTriangle(t);
                }
            }
        }

        private double[] circumcenter(double[] a, double[] b, double[] c)
        {
            double ad = a[0] * a[0] + a[1] * a[1],
                   bd = b[0] * b[0] + b[1] * b[1],
                   cd = c[0] * c[0] + c[1] * c[1];
            double D = 2 * (a[0] * (b[1] - c[1]) + b[0] * (c[1] - a[1]) + c[0] * (a[1] - b[1]));
            return new double[]{
              Math.Floor(1 / D * (ad * (b[1] - c[1]) + bd * (c[1] - a[1]) + cd * (a[1] - b[1]))),
              Math.Floor(1 / D * (ad * (c[0] - b[0]) + bd * (a[0] - c[0]) + cd * (b[0] - a[0])))
            };
        }
    }
}
