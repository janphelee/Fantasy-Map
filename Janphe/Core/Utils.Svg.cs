using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkiaSharp;

namespace Janphe
{
    public partial class Utils
    {
        /**
           M = moveto
           L = lineto
           H = horizontal lineto
           V = vertical lineto
           C = curveto                            SKPath.CubicTo
           S = smooth curveto
           Q = quadratic Bézier curve             SKPath.QuadTo
           T = smooth quadratic Bézier curveto
           A = elliptical Arc                     SKPath.ArcTo
           Z = closepath
           注意：以上所有命令均允许小写字母。大写表示绝对定位，小写表示相对定位。
         */

        public static SKPath linePoly(IList<SKPoint> pp) => linePoly(pp, true);

        public static SKPath linePoly(IList<SKPoint> pp, bool close)
        {
            var d = new SKPath();
            d.AddPoly(pp.ToArray(), close);
            return d;
        }
        public static SKPath linePoly(IEnumerable<SKPoint> pp, bool close)
        {
            var d = new SKPath();
            d.AddPoly(pp.ToArray(), close);
            return d;
        }


        public static SKPath lineGen1(double[][] pp)
        {
            var d = new SKPath();
            if (pp.Length == 1)
            {
                d.MoveTo((float)pp[0][0] - 50, (float)pp[0][1]);
                d.RLineTo(100, 0);
                return d;
            }
            if (pp.Length == 2)
            {
                d.MoveTo(pp[0].SK());
                d.LineTo(pp[1].SK());
                return d;
            }

            var mp = pp.Select((p, i) => Mid(p, pp[(i + 1) % pp.Length])).ToArray();

            d.MoveTo(pp.First().SK());
            d.LineTo(mp.First().SK());

            for (var i = 1; i < pp.Length - 1; ++i)
                d.QuadTo(pp[i].SK(), mp[i].SK());

            d.LineTo(pp.Last().SK());

            return d;
        }
        public static SKPath lineGenZ(double[][] pp)
        {
            var mp = pp.Select((p, i) => Mid(p, pp[(i + 1) % pp.Length])).ToArray();

            var d = new SKPath();

            d.MoveTo(mp.Last().SK());

            for (var i = 0; i < pp.Length; ++i)
                d.QuadTo(pp[i].SK(), mp[i].SK());

            d.Close();

            return d;
        }

        public static SKPath lineGenZ(IList<SKPoint> pp)
        {
            var mp = pp.Select((p, i) => Mid(p, pp[(i + 1) % pp.Count])).ToArray();

            var d = new SKPath();

            d.MoveTo(mp.Last());

            for (var i = 0; i < pp.Count; ++i)
                d.QuadTo(pp[i], mp[i]);

            d.Close();

            return d;
        }

        public static double[] Mul(double[] a, double s) => new double[] { a[0] * s, a[1] * s };
        public static double[] Add(double[] a, double[] b) => new double[] { a[0] + b[0], a[1] + b[1] };
        public static double[] Mid(double[] a, double[] b) => Mul(Add(a, b), 0.5);

        public static SKPoint Mul(SKPoint a, float s) => new SKPoint(a.X * s, a.Y * s);
        public static SKPoint Add(SKPoint a, SKPoint b) => new SKPoint(a.X + b.X, a.Y + b.Y);
        public static SKPoint Mid(SKPoint a, SKPoint b) => Mul(Add(a, b), 0.5f);
    }
}
