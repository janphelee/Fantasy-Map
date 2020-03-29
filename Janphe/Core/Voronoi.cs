using System;
using System.Collections;
using System.Collections.Generic;

namespace Janphe
{
    public class Voronoi
    {
        public int numSides { get { return s_triangles_r.Length; } }
        public int numTriangles { get { return numSides / 3; } }

        public int[] s_triangles_r { get; set; }
        public int[] s_halfedges_s { get; set; }

        public int s_begin_r(int s) { return s_triangles_r[s]; }
        public int s_end_r(int s) { return s_begin_r(s_next_s(s)); }
        // s_oppo_r == s_end_r
        public int s_oppo_r(int s) { return s_begin_r(s_opposite_s(s)); }

        public int s_inner_t(int s) { return s_to_t(s); }
        public int s_outer_t(int s) { return s_to_t(s_halfedges_s[s]); }
        public int s_opposite_s(int s) { return s_halfedges_s[s]; }

        public static int s_to_t(int s) { return (int)Math.Floor(s / 3.0); }
        public static int s_prev_s(int s) { return (s % 3 == 0) ? s + 2 : s - 1; }
        // 边顺序顺时针方向
        public static int s_next_s(int s) { return (s % 3 == 2) ? s - 2 : s + 1; }

        public static int[] t_circulate_s(int t) { var out_s = new int[3]; for (var i = 0; i < 3; i++) { out_s[i] = 3 * t + i; } return out_s; }
        public int[] t_circulate_r(int t) { var out_r = new int[3]; for (var i = 0; i < 3; i++) { out_r[i] = s_triangles_r[3 * t + i]; } return out_r; }
        public int[] t_circulate_t(int t) { var out_t = new int[3]; for (var i = 0; i < 3; i++) { out_t[i] = s_outer_t(3 * t + i); } return out_t; }

        public int[] s_circulate_s(int s0)
        {
            var out_s = new List<int>();
            var incoming = s0;
            do
            {
                out_s.Add(incoming);
                var outgoing = Voronoi.s_next_s(incoming);
                incoming = s_opposite_s(outgoing);
            } while (incoming != -1 && incoming != s0);
            return out_s.ToArray();
        }

        // 以r为起点的边
        public int[][] r_raycast_s(int numRegions)
        {
            var raycast = new int[numRegions][];

            var used = new BitArray(numSides);
            for (var s = 0; s < numSides; s++)
            {
                if (used[s]) continue;

                var r = s_begin_r(s);
                if (r >= numRegions) continue;

                var out_s = raycast_s(s);
                for (var i = 0; i < out_s.Length; ++i) used[out_s[i]] = true;
                raycast[r] = out_s;
            }
            return raycast;
        }
        private int[] raycast_s(int first_s)
        {
            var out_s = new List<int>();
            var incoming = first_s;
            do
            {
                out_s.Add(incoming);
                incoming = s_next_s(s_opposite_s(incoming));
            } while (incoming != first_s);
            ////丢弃不能走一圈的区域
            //if (incoming == -1)
            //{
            //    UnityEngine.Debug.Log($"incoming == -1 drop s:{first_s}");
            //    return null;
            //}
            return out_s.ToArray();
        }

        //public int[] r_circulate_s(int r)
        //{
        //    int s0 = _r_in_s[r];
        //    int incoming = s0;
        //    var out_s = new List<int>();
        //    do
        //    {
        //        out_s.Add(s_halfedges_s[incoming]);
        //        var outgoing = s_next_s(incoming);
        //        incoming = s_halfedges_s[outgoing];
        //    } while (incoming != -1 && incoming != s0);
        //    return out_s.ToArray();
        //}

        //public int[] r_circulate_r(int r)
        //{
        //    int s0 = _r_in_s[r];
        //    int incoming = s0;
        //    var out_r = new List<int>();
        //    do
        //    {
        //        out_r.Add(s_begin_r(incoming));
        //        var outgoing = s_next_s(incoming);
        //        incoming = s_halfedges_s[outgoing];
        //    } while (incoming != -1 && incoming != s0);
        //    return out_r.ToArray();
        //}

        //public int[] r_circulate_t(int r)
        //{
        //    int s0 = _r_in_s[r];
        //    int incoming = s0;
        //    var out_t = new List<int>();
        //    do
        //    {
        //        out_t.Add(s_to_t(incoming));
        //        var outgoing = s_next_s(incoming);
        //        incoming = s_halfedges_s[outgoing];
        //    } while (incoming != -1 && incoming != s0);
        //    return out_t.ToArray();
        //}
    }
}
