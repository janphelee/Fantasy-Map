using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using static Janphe.Utils;

namespace Janphe.Fantasy.Map
{
    internal class Map1OceanLayers
    {
        public static readonly Dictionary<string, string> outlineLayers = new Dictionary<string, string>()
        {
            { "No outline","none"},
            { "Random","random"},
            { "Standard 3","-6,-3,-1"},
            { "Indented 3","-6,-4,-2"},
            { "Standard 4","-9,-6,-3,-1"},
            { "Smooth 6","-6,-5,-4,-3,-2,-1"},
            { "Smooth 9","-9,-8,-7,-6,-5,-4,-3,-2,-1"},
        };
        private string outline { get; set; }

        private Grid grid { get; set; }
        private Grid.Cells cells { get; set; }
        private Grid.Vertices vertices { get; set; }

        private int pointsN { get; set; }

        public Map1OceanLayers(MapJobs map)
        {
            grid = map.grid;

            cells = grid.cells;
            vertices = grid.vertices;
            pointsN = cells.i.Length;

            outline = outlineLayers["Standard 3"];//default style
        }

        public float Opacity { get; private set; }
        public int[] Limits { get; private set; }
        public Dictionary<int, List<SKPoint[]>> Paths { get; private set; }

        public void generate()
        {
            if (outline == "none")
                return;

            var limits =
                outline == "random" ?
                randomizeOutline() :
                outline.Split(',').Select(s => int.Parse(s)).ToArray();
            markupOcean(limits);

            var opacity = rn(0.4 / limits.Length, 2);
            var used = new byte[pointsN];

            var chains = new Dictionary<int, List<SKPoint[]>>();
            foreach (var i in cells.i)
            {
                var t = cells.t[i];
                if (t > 0)
                    continue;
                var found = Array.Exists(limits, a => a == t);
                if (used[i] != 0 || !found)
                    continue;
                var start = findStart(i, t);
                if (start < 0)
                    continue;
                used[i] = 1;
                var chain = connectVertices(start, t, used); // vertices chain to form a path
                var relaxation = 1 + t * -2; // select only n-th point
                var relaxed = chain.Where((v, d) => d % relaxation == 0 || Array.Exists(vertices.c[v], c => c >= pointsN)).ToArray();
                if (relaxed.Length >= 3)
                {
                    var points = relaxed.Select(v => vertices.t_points[v]).ToArray();

                    if (!chains.ContainsKey(t))
                        chains[t] = new List<SKPoint[]>();
                    chains[t].Add(
                      points.Select(p => new SKPoint((float)p[0], (float)p[1])).ToArray()
                    );
                }
            }

            Opacity = (float)opacity;
            Limits = limits;
            Paths = chains;

            // 深浅不一的海岸线
            foreach (var t in limits)
            {
                //const path = chains.filter(c => c[0] === t).map(c => round(lineGen(c[1]))).join();
                //if (path) oceanLayers.append("path").attr("d", path).attr("fill", "#ecf2f9").style("opacity", opacity);
                // For each layer there should outer ring. If no, layer will be upside down. Need to fix it in the future
            }
        }

        // find eligible cell vertex to start path detection
        private int findStart(int i, int t)
        {
            if (cells.r_near_border[i])
            {
                // map border cell
                return Array.Find(cells.v[i], v => Array.Exists(vertices.c[v], c => c >= pointsN));
            }
            var idx = Array.FindIndex(cells.r_neighbor_r[i], c => cells.t[c] < t || cells.t[c] == 0);
            if (idx < 0)
                return idx;
            return cells.v[i][idx];
        }

        private int[] randomizeOutline()
        {
            var limits = new List<int>();
            var odd = .2;
            for (var l = -9; l < 0; l++)
            {
                if (P(odd))
                { odd = .2; limits.Add(l); }
                else
                { odd *= 2; }
            }
            return limits.ToArray();
        }

        private void markupOcean(int[] limits)
        {
            // Define ocean cells type based on distance form land
            for (var t = -2; t >= limits[0] - 1; t--)
            {
                for (var i = 0; i < pointsN; i++)
                {
                    if (cells.t[i] != t + 1)
                        continue;
                    foreach (var e in cells.r_neighbor_r[i])
                    {
                        if (cells.t[e] == 0)
                            cells.t[e] = (sbyte)t;
                    }
                }
            }
        }

        // connect vertices to chain
        private int[] connectVertices(int start, sbyte t, byte[] used)
        {
            var chain = new List<int>(); // vertices chain to form a path
            for (int i = 0, current = start; i == 0 || current != start && i < 10000; i++)
            {
                var prev = i == 0 ? -1 : chain[chain.Count - 1]; // previous vertex in chain
                chain.Add(current); // add current vertex to sequence
                // cells adjacent to vertex
                var cc = vertices.c[current];
                var tc = cc.Where(c => c < cells.t.Length && cells.t[c] == t).ToArray();
                foreach (var c in tc)
                    used[c] = 1;

                var v = vertices.v[current]; // neighboring vertices
                var c0 = cc[0] >= 0 && cc[0] < cells.t.Length && (0 == cells.t[cc[0]] || cells.t[cc[0]] == t - 1);
                var c1 = cc[1] >= 0 && cc[1] < cells.t.Length && (0 == cells.t[cc[1]] || cells.t[cc[1]] == t - 1);
                var c2 = cc[2] >= 0 && cc[2] < cells.t.Length && (0 == cells.t[cc[2]] || cells.t[cc[2]] == t - 1);
                if (/*v[0] != undefined &&*/ v[0] != prev && c0 != c1)
                    current = v[0];
                else if (/*v[1] != undefined &&*/ v[1] != prev && c1 != c2)
                    current = v[1];
                else if (/*v[2] != undefined &&*/ v[2] != prev && c0 != c2)
                    current = v[2];
                if (current == chain[chain.Count - 1])
                {
                    //console.error("Next vertex is not found");
                    break;
                }
            }
            chain.Add(chain[0]); // push first vertex as the last one
            return chain.ToArray();
        }

    }
}
