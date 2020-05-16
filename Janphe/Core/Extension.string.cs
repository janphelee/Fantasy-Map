using System;
using System.Collections.Generic;

namespace Janphe
{
    public static partial class Extension
    {
        public static bool includes(this string d, char c) { return d.IndexOf(c) != -1; }

        public static string toUpperCase(this char c)
        { return ("" + c).ToUpper(); }
        public static string toLowerCase(this char c)
        { return ("" + c).ToLower(); }

        public static string slice(this string d, int start)
        {
            if (d.Length == 0)
            {
                //Debug.LogWarning("d.Length == 0, Attempted to divide by zero.");
                return d;
            }
            start = start < 0 ? start + d.Length : start % d.Length;
            if (start < 0)
            {
                //Debug.LogWarning("StartIndex cannot be less than zero.");
                return d;
            }
            return d.Substring(start, d.Length - start);
        }
        public static string slice(this string d, int start, int end)
        {
            if (d.Length == 0)
            {
                //Debug.LogWarning("d.Length == 0, Attempted to divide by zero.");
                return d;
            }
            start = start < 0 ? start + d.Length : start % d.Length;
            if (start < 0)
            {
                //Debug.LogWarning("StartIndex cannot be less than zero.");
                return d;
            }
            if (end < 0)
                end += d.Length;

            return d.Substring(start, end - start);
        }

        public static string capitalize(this string d)
        { return d[0].toUpperCase() + d.slice(1); }

        public static string replace(this string d, char old, char @new) { return d.Replace(old, @new); }
        public static string replace(this string d, string old, string @new) { return d.Replace(old, @new); }
        public static string[] split(this string d, char separator) { return d.Split(separator); }
        public static string[] split(this string d, string separator) { return d.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries); }

        public static string join<T>(this IEnumerable<T> d, string separator = ",") => string.Join(separator, d);
        public static string join<T>(this T[] d, string separator = ",") => string.Join(separator, d);
        public static string join<T>(this T[][] d, string s1, string s2) => d.map(t => t.join(s2)).join(s1);
        public static string Str<T>(this T[] d, string separator = " ") => d.join(separator);

    }
}
