using System;

namespace Janphe
{
    public partial class D3
    {
        public static Func<string[], Func<float, Color>> rgbSpline(Func<float[], Func<float, float>> spline)
        {
            return colors =>
            {
                var n = colors.Length;
                var r = new float[n];
                var g = new float[n];
                var b = new float[n];

                Color color = Color.White;

                for (var i = 0; i < n; ++i)
                {
                    color = colors[i].ToColor();
                    //Debug.Log($"{i} {colors[i]} {color}");
                    r[i] = color.r;
                    g[i] = color.g;
                    b[i] = color.b;
                }

                var fr = spline(r);
                var fg = spline(g);
                var fb = spline(b);
                color.a = 1;
                return t =>
                {
                    color.r = fr(t);
                    color.g = fg(t);
                    color.b = fb(t);
                    return color;
                };
            };
        }

        public static readonly Func<string[], Func<float, Color>> rgbBasis = rgbSpline(basis);

        private static Func<Color, Color, Func<double, Color>> rgbGamma(double y)
        {
            var color = gamma(y);

            Func<double, Color> rgb(Color start, Color end)
            {
                var r = color(start.r, end.r);
                var g = color(start.g, end.g);
                var b = color(start.b, end.b);
                var opacity = nogamma(start.a, end.a);
                return t =>
                {
                    start.r = (float)r(t);
                    start.g = (float)g(t);
                    start.b = (float)b(t);
                    start.a = (float)opacity(t);
                    return start;
                };
            }
            //rgb.gamma = rgbGamma;
            return rgb;
        }

        public static Func<Color, Color, Func<double, Color>> interpolateRgb()
        {
            return rgbGamma(1);
        }
    }
}
