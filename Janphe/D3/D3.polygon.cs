using System;

namespace Janphe
{
    public partial class D3
    {
        public static double polygonArea(double[][] polygon)
        {
            int i = -1,
                n = polygon.Length;
            double[]
                a,
                b = polygon[n - 1];
            double area = 0;

            while (++i < n)
            {
                a = b;
                b = polygon[i];
                area += a[1] * b[0] - a[0] * b[1];
            }

            return area / 2;
        }

        public static bool polygonContains(double[][] polygon, double[] point)
        {
            var n = polygon.Length;
            var p = polygon[n - 1];
            double
                x = point[0], y = point[1],
               x0 = p[0], y0 = p[1],
               x1, y1;

            var inside = false;

            for (var i = 0; i < n; ++i)
            {
                p = polygon[i];
                x1 = p[0];
                y1 = p[1];
                if (((y1 > y) != (y0 > y)) && (x < (x0 - x1) * (y - y1) / (y0 - y1) + x1))
                    inside = !inside;
                x0 = x1;
                y0 = y1;
            }

            return inside;
        }

    }
}
