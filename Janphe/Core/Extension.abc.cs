using System;
using System.Collections.Generic;
using System.Linq;

namespace Janphe
{
    public static partial class Extension
    {
        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, int, TSource> func)
        {
            int index = 0;
            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                enumerator.MoveNext();
                index++;
                TSource current = enumerator.Current;
                while (enumerator.MoveNext())
                    current = func(current, enumerator.Current, index++);
                return current;
            }
        }

        public static T reduce<T>(this T[] d, Func<T, T, int, T[], T> func) { return d.Aggregate((s, v, i) => func(s, v, i, d)); }
        public static T reduce<T>(this IList<T> d, Func<T, T, T> func, T seed) { return d.Aggregate(seed, (s, v) => func(s, v)); }

        public static string reduce(this string d, Func<string, char, int, string, string> func, string seed)
        {
            string result = seed;
            for (var i = 0; i < d.Length; ++i)
                result = func(result, d[i], i, d);
            return result;
        }

        public static bool includes<T>(this T[] d, T c) where T : IEquatable<T> { return Array.FindIndex(d, v => v.Equals(c)) != -1; }
        public static bool includes<T>(this IList<T> d, T c) where T : IEquatable<T> { return includes(d.ToArray(), c); }
        public static T[] fill<T>(this T[] d, T c) { for (var i = 0; i < d.Length; ++i) d[i] = c; return d; }
        public static int indexOf<T>(this T[] d, T c) { return Array.IndexOf(d, c); }

        public static T find<T>(this IList<T> d, Func<T, bool> func) { for (var i = 0; i < d.Count; ++i) if (func(d[i])) return d[i]; return default(T); }
        public static bool some<T>(this T[] d, Func<T, bool> func) { return Array.Exists(d, x => func(x)); }
        public static bool some<T>(this IEnumerable<T> d, Func<T, bool> func) { return some(d.ToArray(), func); }

        public static bool every<T>(this IList<T> d, Func<T, bool> func) { for (var i = 0; i < d.Count; ++i) if (!func(d[i])) return false; return true; }

        public static IEnumerable<T> filter<T>(this IList<T> d, Func<T, bool> func) { return d.Where(func); }
        public static IEnumerable<T> filter<T>(this IEnumerable<T> d, Func<T, bool> func) { return d.Where(func); }
        public static T find<T>(this IEnumerable<T> d, Func<T, bool> func) { return d.Single(func); }
        public static bool finded<T>(this IEnumerable<T> d, Func<T, bool> func)
        {
            bool ret;
            try { d.Single(func); ret = true; } catch { ret = false; }
            return ret;
        }

        // 哇，最快稳定排序是这个
        // 最好直接用IOrderedEnumerable 不要转成Array或者List，会降低效率
        private class TComparer<T> : IComparer<T> { public Comparison<T> func { get; set; } public int Compare(T x, T y) => func(x, y); }
        public static IOrderedEnumerable<T> sort<T>(this IList<T> d, Comparison<T> comparison)
        { return d.OrderBy(x => x, new TComparer<T>() { func = comparison }); }
        public static IOrderedEnumerable<T> sort<T>(this IEnumerable<T> d, Comparison<T> comparison)
        { return d.OrderBy(x => x, new TComparer<T>() { func = comparison }); }

        public static void forEach<T>(this IList<T> d, Action<T, int> func) { int i = 0; foreach (var a in d) func(a, i++); }
        public static void forEach<T>(this IList<T> d, Action<T> func) { foreach (var a in d) func(a); }
        public static void forEach<T>(this IEnumerable<T> d, Action<T, int> func) { int i = 0; foreach (var a in d) func(a, i++); }
        public static void forEach<T>(this IEnumerable<T> d, Action<T> func) { foreach (var a in d) func(a); }
        public static IEnumerable<B> map<T, B>(this IEnumerable<T> a, Func<T, B> func) { return a.Select(func); }
        public static IEnumerable<B> map<T, B>(this IEnumerable<T> a, Func<T, int, B> func) { return a.Select(func); }

        public static T shift<T>(this IList<T> d) { var e = d[0]; d.RemoveAt(0); return e; }
        public static void push<T>(this IList<T> d, T e) { d.Add(e); }
        public static T pop<T>(this IList<T> d) { var t = d.Last(); d.RemoveAt(d.Count - 1); return t; }



        public static double pow<T>(this T d, double p, Func<T, double> ct) { return Math.Pow(ct(d), p); }
        public static double pow(this double d, double p) { return pow(d, p, x => x); }
        public static double pow(this int d, double p) { return pow(d, p, x => x); }

    }
}
