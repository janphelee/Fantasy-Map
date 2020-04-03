using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Janphe
{
    public static partial class Extension
    {
        private static readonly float Darker = 0.7f;
        private static readonly float Brighter = 1 / Darker;

        public static Color darker(this Color color, float darker)
        {
            darker = darker == 0 ? Darker : (float)Math.Pow(Darker, darker);

            var a = color.a;
            color *= darker;
            color.a = a;
            return color;
        }
        public static Color brighter(this Color color, float brighter)
        {
            brighter = brighter == 0 ? Brighter : (float)Math.Pow(Brighter, brighter);

            var a = color.a;
            color *= brighter;
            color.a = a;
            return color;
        }

        public static string hex(this Color color, bool alpha = false)
        {
            if (!alpha)
                return string.Format("#{0:x2}{1:x2}{2:x2}", color.r8, color.g8, color.b8);
            return string.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", color.r8, color.g8, color.b8, color.a8);
        }

        /// <summary>
        /// The EM itself that does the job
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns></returns>
        public static Color ToColor(this string colorString)
        {
            colorString = ExtractHexDigits(colorString);

            //Color color = Color.white;
            var color = new Color();

            if (colorString.Length == 6)
            {
                var r = colorString.Substring(0, 2);
                var g = colorString.Substring(2, 2);
                var b = colorString.Substring(4, 2);

                try
                {
                    byte rc = Byte.Parse(r, NumberStyles.HexNumber);
                    byte gc = Byte.Parse(g, NumberStyles.HexNumber);
                    byte bc = Byte.Parse(b, NumberStyles.HexNumber);
                    color = Color.Color8(rc, gc, bc, 255);
                }
                catch (Exception)
                {
                    return Color.White;
                    throw;
                }
            }
            if (colorString.Length == 8)
            {
                var a = colorString.Substring(0, 2);
                var r = colorString.Substring(2, 2);
                var g = colorString.Substring(4, 2);
                var b = colorString.Substring(6, 2);

                try
                {
                    byte ac = Byte.Parse(a, NumberStyles.HexNumber);
                    byte rc = Byte.Parse(r, NumberStyles.HexNumber);
                    byte gc = Byte.Parse(g, NumberStyles.HexNumber);
                    byte bc = Byte.Parse(b, NumberStyles.HexNumber);
                    color = Color.Color8(rc, gc, bc, ac);
                }
                catch (Exception)
                {
                    return Color.White;
                    throw;
                }
            }
            return color;
        }
        public static SKColor SK(this Color color) => new SKColor(color.r8, color.g8, color.b8, color.a8);

        /// <summary>
        /// Extracts the hex digits from the string.
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns></returns>
        private static string ExtractHexDigits(string colorString)
        {
            Regex HexDigits = new Regex(@"[abcdefABCDEF\d]+", RegexOptions.Compiled);

            var hexnum = new StringBuilder();
            foreach (char c in colorString)
                if (HexDigits.IsMatch(c.ToString()))
                    hexnum.Append(c.ToString());

            return hexnum.ToString();
        }
    }
}
