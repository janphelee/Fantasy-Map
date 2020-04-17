using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Janphe
{
    public partial class Utils
    {
        public static bool not(string d) { return string.IsNullOrEmpty(d); }
        public static bool @is(string d) { return !string.IsNullOrEmpty(d); }

        // C#中的Math.Round()默认并不是使用的"四舍五入"法。
        public static int round(double x) { return (int)Math.Round(x, MidpointRounding.AwayFromZero); }
        public static decimal round(decimal x) { return Math.Round(x, MidpointRounding.AwayFromZero); }

        public static double rn(double v, int d = 0) { return (double)rn((decimal)v, d); }
        public static decimal rn(decimal v, int d = 0)
        {
            var m = (long)Math.Pow(10, d);
            var n = (long)v;
            var f = (v - n) * m;
            return n + round(f) / m;
        }

        public static double[] rn(double[] vv, int d)
        {
            return vv.Select(v => rn(v, d)).ToArray();
        }

        // get number from string in format "1-3" or "2" or "0.5"
        public static double getNumberInRange(string s)
        {
            double result;
            if (double.TryParse(s, out result))
            {
                var f = Math.Floor(result);
                return f + (P(result - f) ? 1 : 0);
            }
            //int sign = r[0] == '-' ? -1 : 1;//以负数开始

            var range = s.Contains("-") ? s.Split('-') : null;
            if (range == null || range.Length != 2)
            { return 0; }

            var count = rand(double.Parse(range[0]), double.Parse(range[1]));
            return count;
        }

        // return value in range [0, 100] (height range)
        public static byte lim(double v)
        {
            return (byte)Math.Max(Math.Min((int)v, 100), 0);
        }

        public static double rand() { return Random.NextDouble(); }
        public static double rand(double min, double max = 0)
        {
            if (max < min)
            { max = min; min = 0; }
            return Math.Floor(Random.NextDouble() * (max - min + 1)) + min;
        }
        public static int randi(int min, int max)
        {
            return (int)rand(min, max);
        }

        // probability shorthand
        public static bool P(double probability)
        {
            return Random.NextDouble() < probability;
        }

        public static T pop<T>(List<T> d)
        {
            var ret = d[d.Count - 1];
            d.RemoveAt(d.Count - 1);
            return ret;
        }

        //public static float angle(float x0, float y0, float x1, float y1)
        //{
        //    var p1 = new Vector2(x0, y0).normalized;
        //    var p2 = new Vector2(x1, y1).normalized;

        //    var aa = angle(p1, p2);
        //    var cc = Vector3.Cross(p1, p2).z < 0;
        //    if (cc) aa = 360 - aa;//反向旋转角度
        //    return aa;
        //}

        //public static float angle(Vector2 p1, Vector2 p2)
        //{
        //    var ret = Mathf.Acos(Vector2.Dot(p1, p2)) * Mathf.Rad2Deg;
        //    //Debug.Log($"angle {p1} {p2} ret:{ret}");
        //    return ret;
        //}

        // Array.Sort 为不稳定排序
        public static T[] ArraySort<T>(T[] d, Comparison<T> comparison)
        {
            var w = new Stopwatch();
            w.Start();

            Array.Sort(d, comparison);

            w.Stop();
            Debug.Log($"ArraySort cost:{w.ElapsedMilliseconds}ms {w.ElapsedTicks}ts");
            return d;
        }

        class TComparer<T> : IComparer<T> { public Comparison<T> func { get; set; } public int Compare(T x, T y) => func(x, y); }
        // 哇，最快稳定排序是这个
        // 最好直接用IOrderedEnumerable 不要转成Array或者List，会降低效率
        public static IOrderedEnumerable<T> LinqSort<T>(IList<T> d, Comparison<T> comparison)
        {
            //var w = new Stopwatch();
            //w.Start();

            var ret = d.OrderBy(x => x, new TComparer<T>() { func = comparison });

            //w.Stop();
            //UnityEngine.Debug.Log($"LinqSort cost:{w.ElapsedMilliseconds}ms {w.ElapsedTicks}ts");
            return ret;
        }
        public static IOrderedEnumerable<T> LinqSort<T>(IEnumerable<T> d, Comparison<T> comparison)
        {
            //var w = new Stopwatch();
            //w.Start();

            var ret = d.OrderBy(x => x, new TComparer<T>() { func = comparison });

            //w.Stop();
            //UnityEngine.Debug.Log($"LinqSort cost:{w.ElapsedMilliseconds}ms {w.ElapsedTicks}ts");
            return ret;
        }

        // 但是InsertionSort太慢了。。。
        public static void InsertionSort<T>(T[] d, Comparison<T> comparison)
        {
            var w = new Stopwatch();
            w.Start();

            int count = d.Length;
            for (int j = 1; j < count; j++)
            {
                T key = d[j];

                int i = j - 1;
                for (; i >= 0 && comparison(d[i], key) > 0; i--)
                {
                    d[i + 1] = d[i];
                }
                d[i + 1] = key;
            }

            w.Stop();
            Debug.Log($"InsertionSort cost:{w.ElapsedMilliseconds}ms {w.ElapsedTicks}ts");
        }

        // 这个冒泡排序最慢了
        public static void BubbleSort<T>(T[] a, Comparison<T> comparison)
        {
            var w = new Stopwatch();
            w.Start();

            int len = a.Length;
            T temp;
            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < len - 1 - i; j++)
                {
                    if (comparison(a[j], a[j + 1]) > 0)
                    {
                        temp = a[j];
                        a[j] = a[j + 1];
                        a[j + 1] = temp;
                    }
                }
            }

            w.Stop();
            Debug.Log($"BubbleSort cost:{w.ElapsedMilliseconds}ms {w.ElapsedTicks}ts");
        }

        public static double hypot(double[] d)
        {
            double s = 0;
            for (var i = 0; i < d.Length; ++i)
                s += d[i] * d[i];
            return Math.Sqrt(s);
        }
        public static double hypot(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        public static T last<T>(T[] d) { return d[d.Length - 1]; }
        public static char last(string d) { return d[d.Length - 1]; }

        // return random value from the array
        public static T ra<T>(IList<T> array) { return array[(int)Math.Floor(Random.NextDouble() * array.Count)]; }
        public static string ra(JToken array)
        {
            string ret;
            try
            {
                IList<JToken> strs = array.Value<JArray>();
                ret = ra(strs).Value<string>();
            }
            catch (Exception e)
            {
                ret = "";
                Debug.Log($"{e.Message} Type:{array.Type}");
            }
            return ret;
        }

        public static IList<string> ww(Dictionary<string, int> d) { return d.ww(); }

        // return random value from weighted array {"key1":weight1, "key2":weight2}
        public static string rw(Dictionary<string, int> d) { return d.rw(); }
        public static string rw(JToken d) { return d.dict().rw(); }
        public static string rw(string d) { return rw(JObject.Parse(d).dict()); }

        public static double normalize(double val, double min, double max)
        {
            return Math.Min(Math.Max((val - min) / (max - min), 0), 1);
        }
        // return a random integer from min to max biased towards one end based on exponent distribution (the bigger ex the higher bias towards min)
        // from https://gamedev.stackexchange.com/a/116875
        public static int biased(double min, double max, double ex)
        {
            return (int)round(min + (max - min) * Math.Pow(Random.NextDouble(), ex));
        }

        public static IEnumerable<T> intersect<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            var setA = new HashSet<T>(a);
            var setB = new HashSet<T>(b);
            return setA.Where(_a => setB.Contains(_a));
        }

        // check if char is vowel(元音)
        public static bool vowel(char c) { return "aeiouy".IndexOf(c) != -1; }
        public static bool vowel(string c) { return "aeiouy".IndexOf(c) != -1; }

        // remove vowels from the end of the string
        public static string trimVowels(string str)
        {
            while (str.Length > 3 && vowel(last(str)))
            { str = str.slice(0, -1); }
            return str;
        }

        // get adjective form from noun
        public static string getAdjective(string str)
        {
            // special cases for some suffixes
            if (str.Length > 8 && str.slice(-6) == "orszag")
                return str.slice(0, -6);
            if (str.Length > 6 && str.slice(-4) == "stan")
                return str.slice(0, -4);
            if (P(.5) && str.slice(-4) == "land")
                return str + "ic";
            if (str.slice(-4) == " Guo")
                str = str.slice(0, -4);

            // don't change is name ends on suffix
            if (str.slice(-2) == "an")
                return str;
            if (str.slice(-3) == "ese")
                return str;
            if (str.slice(-1) == "i")
                return str;

            var end = str.slice(-1); // last letter of string
            if (end == "a")
                return str += "n";
            if (end == "o")
                return str = trimVowels(str) + "an";
            if (vowel(end) || end == "c")
                return str += "an"; // ceiuy
            if (end == "m" || end == "n")
                return str += "ese";
            if (end == "q")
                return str += "i";
            return trimVowels(str) + "ian";
        }

        public static Color HSL2RGB(float h, float s, float l, float a = 1)
        {
            float r, g, b;
            r = l;
            g = l;
            b = l;

            float v = (l <= 0.5f) ? (l * (1 + s)) : (l + s - l * s);
            if (v > 0)
            {
                float m = l + l - v;
                float sv = (v - m) / v;
                h *= 6f;
                int sextant = (int)h;
                float fract = h - sextant;
                float vsf = v * sv * fract;
                float mid1 = m + vsf;
                float mid2 = v - vsf;

                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;

                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }

            Color rgb = new Color(r, g, b, a);
            return rgb;

        }
        // Return H,S,L in range of 0-1
        public static void RGB2HSL(Color rgb, out float h, out float s, out float l)
        {

            float r = rgb.r8 / 255f;
            float g = rgb.g8 / 255f;
            float b = rgb.b8 / 255f;

            h = 0;
            s = 0;
            l = 0;

            float v = Mathf.Max(r, g, b);
            float m = Mathf.Min(r, g, b);

            l = (m + v) / 2f;
            if (l <= 0)
                return;

            float vm = v - m;

            s = vm;

            if (s > 0.0)
                s /= (l <= 0.5f) ? (v + m) : (2.0f - v - m);
            else
                return;

            float r2 = (v - r) / vm;

            float g2 = (v - g) / vm;

            float b2 = (v - b) / vm;


            if (r == v)
                h = (g == m ? 5.0f + b2 : 1.0f - g2);
            else if (g == v)
                h = (b == m ? 1.0f + r2 : 3.0f - b2);
            else
                h = (r == m ? 3.0f + g2 : 5.0f - r2);

            h /= 6f;
        }

        public static string ColorToHex(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255.0f);
            int g = Mathf.RoundToInt(color.g * 255.0f);
            int b = Mathf.RoundToInt(color.b * 255.0f);
            int a = Mathf.RoundToInt(color.a * 255.0f);
            string hex = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
            return hex;
        }

        public static Color HexToColor(string hex)
        {
            byte br = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte bg = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte bb = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            byte cc = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
            float r = br / 255f;
            float g = bg / 255f;
            float b = bb / 255f;
            float a = cc / 255f;
            return new Color(r, g, b, a);
        }
    }
}
