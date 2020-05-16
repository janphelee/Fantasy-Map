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
                    r[i] = color.R;
                    g[i] = color.G;
                    b[i] = color.B;
                }

                var fr = spline(r);
                var fg = spline(g);
                var fb = spline(b);
                color.Opacity(1);
                return t =>
                {
                    color.R = fr(t);
                    color.G = fg(t);
                    color.B = fb(t);
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
                var r = color(start.R, end.R);
                var g = color(start.G, end.G);
                var b = color(start.B, end.B);
                var opacity = nogamma(start.A, end.A);
                return t =>
                {
                    start.R = (float)r(t);
                    start.G = (float)g(t);
                    start.B = (float)b(t);
                    start.Opacity((float)opacity(t));
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
