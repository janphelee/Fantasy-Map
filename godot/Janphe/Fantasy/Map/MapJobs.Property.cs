using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        public Coordinates mapCoordinates { get; set; }

        public Grid grid { get; private set; }
        public Grid pack { get; private set; }


        public Biomes biomesData { get; set; }
        public NamesGenerator Names { get; set; }
        public NamesGenerator.Names[] nameBases { get; set; }
        public Map6Routes Routes { get; set; }

        public readonly Style[] Lakes = new Style[] {
            new Style{ name="freshwater", opacity=0.5f, fill="#a6c1fd", stroke="#5f799d", strokeWidth=0.7f, filter=null },
            new Style{ name="salt",       opacity=0.5f, fill="#409b8a", stroke="#388985", strokeWidth=0.7f, filter=null },
            new Style{ name="sinkhole",   opacity=1.0f, fill="#5bc9fd", stroke="#53a3b0", strokeWidth=0.7f, filter=null   },
            new Style{ name="frozen",     opacity=.95f, fill="#cdd4e7", stroke="#cfe0eb", strokeWidth=0f, filter=null  },
            new Style{ name="lava",       opacity=0.7f, fill="#90270d", stroke="#f93e0c", strokeWidth=2f, filter="crumpled" },
        };

        public readonly Style[] Islands = new Style[] {
            new Style{ name="sea_island", opacity=0.5f, stroke="#1f3846", strokeWidth=0.7f, filter="dropShadow", autoFilter=true },
            new Style{ name="lake_island", opacity=1f, stroke="#7c8eaf", strokeWidth=0.35f},
        };


        internal List<int[]> connectVertices(int start, ushort p0, ushort p1, ushort[] pp, BitArray used)
        {
            var cells = pack.cells;
            var vertices = pack.vertices;
            var n = cells.i.Length;

            var chain = new List<int[]>();

            var land = vertices.c[start].some(c => cells.h[c] >= 20 && pp[c] != p0);
            void check(int i)
            { p1 = pp[i]; land = cells.h[i] >= 20; }

            for (int i = 0, current = start; i == 0 || current != start && i < 20000; i++)
            {
                var prev = chain.Count > 0 ? chain.Last()[0] : -1;
                chain.push(new int[] { current, p1, land ? 1 : 0 }); // add current vertex to sequence
                var c = vertices.c[current]; // cells adjacent to vertex
                c.filter(ci => pp[ci] == p0).forEach(ci => used[ci] = true);
                var c0 = c[0] >= n || pp[c[0]] != p0;
                var c1 = c[1] >= n || pp[c[1]] != p0;
                var c2 = c[2] >= n || pp[c[2]] != p0;
                var v = vertices.v[current]; // neighboring vertices
                if (v[0] != prev && c0 != c1)
                {
                    current = v[0];
                    check(c0 ? c[0] : c[1]);
                }
                else if (v[1] != prev && c1 != c2)
                {
                    current = v[1];
                    check(c1 ? c[1] : c[2]);
                }
                else if (v[2] != prev && c0 != c2)
                {
                    current = v[2];
                    check(c2 ? c[2] : c[0]);
                }
                if (current == chain[chain.Count - 1][0])
                { Debug.Log("Next vertex is not found"); break; }
            }
            chain.push(new int[] { start, p1, land ? 1 : 0 }); // add starting vertex to sequence to close the path
            return chain;
        }

    }
}
