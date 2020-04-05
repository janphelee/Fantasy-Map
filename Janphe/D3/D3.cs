using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe
{
    public partial class D3
    {
        public static int ascending<T>(T a, T b) where T : IComparable { return a.CompareTo(b); }
        public static int descending<T>(T a, T b) where T : IComparable { return b.CompareTo(a); }

        public static T number<T>(T x) { return x; }
        public static T number<T>(T x, int i, T[] d) { return x; }

        public static double threshold<T>(T[] values, double p, Func<T, int, T[], T> valueof, Func<T, double> CT)
        {
            if (valueof == null)
                valueof = number;
            var n = values.Length;

            if (n == 0)
                return 0;
            if (p <= 0 || n < 2)
                return CT(valueof(values[0], 0, values));
            if (p >= 1)
                return CT(valueof(values[n - 1], n - 1, values));

            double i = (n - 1) * p;
            int i0 = (int)Math.Floor(i);
            var value0 = CT(valueof(values[i0], i0, values));
            var value1 = CT(valueof(values[i0 + 1], i0 + 1, values));
            return value0 + (value1 - value0) * (i - i0);
        }
        public static double threshold(int[] values, double p) { return threshold(values, p, null, x => x); }
        public static double threshold(double[] values, double p) { return threshold(values, p, null, x => x); }

        public static double median<T>(T[] values, Func<T, int, T[], T> valueof, Func<T, double> CT)
        {
            var numbers = values.Select((v, i) => CT(valueof(v, i, values)));
            numbers = Utils.LinqSort(numbers, ascending);
            return threshold(numbers.ToArray(), 0.5);
        }
        public static double median<T>(IEnumerable<T> values, Func<T, double> CT)
        {
            var numbers = values.Select((v, i) => CT(v));
            numbers = Utils.LinqSort(numbers, ascending);
            return threshold(numbers.ToArray(), 0.5);
        }
        public static double median(IEnumerable<ushort> values) { return median(values, x => x); }

        public static int scan<T>(T[] values, Comparison<T> compare)
        {
            int n = values.Length,
                i = 0, j = 0;
            T xi, xj = values[j];
            while (++i < n)
            {
                if (compare(xi = values[i], xj) < 0 || compare(xj, xj) != 0)
                {
                    xj = xi;
                    j = i;
                }
            }
            if (compare(xj, xj) == 0)
                return j;
            return -1;
        }

        public static double mean(byte[] d) { return d.Select(t => (int)t).Average(); }
        public static double mean(ushort[] d) { return d.Select(t => (int)t).Average(); }
        public static double mean(int[] d) { return d.Average(); }
        public static double mean(double[] d) { return d.Average(); }
        public static double mean(IEnumerable<int> d) { return d.Average(); }
        public static double sum(IEnumerable<int> d) { return d.Sum(); }
        public static double sum(IEnumerable<double> d) { return d.Sum(); }
        public static T min<T>(IEnumerable<T> d) { return d.Min(); }
        public static T max<T>(IEnumerable<T> d) { return d.Max(); }


        public static int[] range(int n)
        {
            var ret = new int[n];
            for (var i = 0; i < n; ++i)
                ret[i] = i;
            return ret;
        }

        public static int[] range(int start, int stop, int step)
        {
            return sequence(start, stop, step);
        }

        public delegate double NormalFunc();
        public delegate NormalFunc NormalBody(double mu, double sigma);
        public static NormalBody randomNormal { get; } = sourceRandomNormal(defaultSourceSet1);

        public static T[] shuffle<T>(T[] array, int i0 = 0, int i1 = 0)
        {
            var m = (i1 == 0 ? array.Length : i1) - i0;
            T t;
            int i;
            while (m > 0)
            {
                i = (int)(Random.NextDouble() * m--);
                t = array[m + i0];
                array[m + i0] = array[i + i0];
                array[i + i0] = t;
            }
            return array;
        }
    }
}
