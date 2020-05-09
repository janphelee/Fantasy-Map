using System.Linq;
using System.Text;

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

        public static string LineTo(double[][] pp)
        {
            var d = "M" + pp.map(p => p.join(",")).join("L");
            return d;
        }

        public static string CurveTo(double[][] pp)
        {
            var d = "M" + pp.map(p => p.join(",")).join("L");
            return d;
        }

        public static string lineGen1(double[][] pp)
        {
            if (pp.Length == 1)
                return $"M{pp[0][0] - 50},{pp[0][1]}h{100}";
            if (pp.Length == 2)
                return $"M{pp[0].Str()}L{pp[1].Str()}";

            var mp = pp.Select((p, i) => Mid(p, pp[(i + 1) % pp.Length])).ToArray();

            var d = new StringBuilder();

            d.Append($"M{pp.First().Str()}");
            d.Append($"L{mp.First().Str()}");

            for (var i = 1; i < pp.Length - 1; ++i)
                d.Append($"Q{pp[i].Str()}, {mp[i].Str()}");

            d.Append($"L{pp.Last().Str()}");

            return d.ToString();
        }

        public static string lineGenZ(double[][] pp)
        {
            var mp = pp.Select((p, i) => Mid(p, pp[(i + 1) % pp.Length])).ToArray();

            var d = new StringBuilder();

            d.Append($"M{mp.Last().Str()}");

            for (var i = 0; i < pp.Length; ++i)
                d.Append($"Q{pp[i].Str()}, {mp[i].Str()}");

            d.Append($"Z");

            return d.ToString();
        }

        public static double[] Mul(double[] a, double s) => new double[] { a[0] * s, a[1] * s };
        public static double[] Add(double[] a, double[] b) => new double[] { a[0] + b[0], a[1] + b[1] };

        public static double[] Mid(double[] a, double[] b) => Mul(Add(a, b), 0.5);
    }
}
