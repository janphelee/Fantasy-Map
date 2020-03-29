using System;

namespace Janphe
{
    public partial class D3
    {
        private static Func<T, T> constant<T>(T x) { return t => x; }

        private static Func<double, double> linear(double a, double b)
        {
            return t => a + t * b;
        }

        private static Func<double, double> exponential(double a, double b, double y)
        {
            a = a.pow(y);
            b = b.pow(y) - a;
            y = 1 / y;
            return t => (a + t * b).pow(y);
        }

        public static Func<double, double> hue(double a, double b)
        {
            var d = b - a;
            return d != 0 ?
                linear(a, d > 180 || d < -180 ? d - 360 * Math.Round(d / 360) : d) :
                constant(double.IsNaN(a) ? b : a);
        }

        public static Func<double, double> nogamma(double a, double b)
        {
            var d = b - a;
            return d != 0 ? linear(a, d) : constant(double.IsNaN(a) ? b : a);
        }
        public static Func<double, double, Func<double, double>> gamma(double y)
        {
            Func<double, double, Func<double, double>> gamma = (a, b) =>
            {
                return b - a != 0 ? exponential(a, b, y) : constant(double.IsNaN(a) ? b : a);
            };
            return y == 1 ? nogamma : gamma;
        }
    }
}
