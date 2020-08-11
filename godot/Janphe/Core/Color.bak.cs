using System;

namespace Janphe
{
#if false
    partial struct Color
    {
        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return r;
                    case 1:
                        return g;
                    case 2:
                        return b;
                    case 3:
                        return a;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0:
                        r = value;
                        return;
                    case 1:
                        g = value;
                        return;
                    case 2:
                        b = value;
                        return;
                    case 3:
                        a = value;
                        return;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public static Color ColorN(string name, float alpha = 1f)
        {
            name = name.Replace(" ", String.Empty);
            name = name.Replace("-", String.Empty);
            name = name.Replace("_", String.Empty);
            name = name.Replace("'", String.Empty);
            name = name.Replace(".", String.Empty);
            name = name.ToLower();

            if (!namedColors.ContainsKey(name))
            {
                throw new ArgumentOutOfRangeException($"Invalid Color Name: {name}");
            }

            Color color = namedColors[name];
            color.a = alpha;
            return color;
        }

        public void ToHsv(out float hue, out float saturation, out float value)
        {
            float max = (float)Math.Max(r, Math.Max(g, b));
            float min = (float)Math.Min(r, Math.Min(g, b));

            float delta = max - min;

            if (delta == 0)
            {
                hue = 0;
            }
            else
            {
                if (r == max)
                    hue = (g - b) / delta; // Between yellow & magenta
                else if (g == max)
                    hue = 2 + (b - r) / delta; // Between cyan & yellow
                else
                    hue = 4 + (r - g) / delta; // Between magenta & cyan

                hue /= 6.0f;

                if (hue < 0)
                    hue += 1.0f;
            }

            saturation = max == 0 ? 0 : 1f - 1f * min / max;
            value = max;
        }

        public Color Blend(Color over)
        {
            Color res;

            float sa = 1.0f - over.a;
            res.a = a * sa + over.a;

            if (res.a == 0)
            {
                return new Color(0, 0, 0, 0);
            }

            res.r = (r * a * sa + over.r * over.a) / res.a;
            res.g = (g * a * sa + over.g * over.a) / res.a;
            res.b = (b * a * sa + over.b * over.a) / res.a;

            return res;
        }

        public Color Contrasted()
        {
            return new Color(
                (r + 0.5f) % 1.0f,
                (g + 0.5f) % 1.0f,
                (b + 0.5f) % 1.0f,
                a
            );
        }

        public Color Darkened(float amount)
        {
            Color res = this;
            res.r = res.r * (1.0f - amount);
            res.g = res.g * (1.0f - amount);
            res.b = res.b * (1.0f - amount);
            return res;
        }

        public Color Inverted()
        {
            return new Color(
                1.0f - r,
                1.0f - g,
                1.0f - b,
                a
            );
        }

        public Color Lightened(float amount)
        {
            Color res = this;
            res.r = res.r + (1.0f - res.r) * amount;
            res.g = res.g + (1.0f - res.g) * amount;
            res.b = res.b + (1.0f - res.b) * amount;
            return res;
        }

        public Color LinearInterpolate(Color c, float t)
        {
            var res = this;

            res.r += t * (c.r - r);
            res.g += t * (c.g - g);
            res.b += t * (c.b - b);
            res.a += t * (c.a - a);

            return res;
        }

        public int ToAbgr32()
        {
            int c = (byte)Math.Round(a * 255);
            c <<= 8;
            c |= (byte)Math.Round(b * 255);
            c <<= 8;
            c |= (byte)Math.Round(g * 255);
            c <<= 8;
            c |= (byte)Math.Round(r * 255);

            return c;
        }

        public long ToAbgr64()
        {
            long c = (ushort)Math.Round(a * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(b * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(g * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(r * 65535);

            return c;
        }

        public long ToArgb64()
        {
            long c = (ushort)Math.Round(a * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(r * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(g * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(b * 65535);

            return c;
        }

        public int ToRgba32()
        {
            int c = (byte)Math.Round(r * 255);
            c <<= 8;
            c |= (byte)Math.Round(g * 255);
            c <<= 8;
            c |= (byte)Math.Round(b * 255);
            c <<= 8;
            c |= (byte)Math.Round(a * 255);

            return c;
        }

        public long ToRgba64()
        {
            long c = (ushort)Math.Round(r * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(g * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(b * 65535);
            c <<= 16;
            c |= (ushort)Math.Round(a * 65535);

            return c;
        }

        public string ToHtml(bool includeAlpha = true)
        {
            var txt = string.Empty;

            txt += ToHex32(r);
            txt += ToHex32(g);
            txt += ToHex32(b);

            if (includeAlpha)
                txt = ToHex32(a) + txt;

            return txt;
        }

        internal static bool HtmlIsValid(string color)
        {
            if (color.Length == 0)
                return false;

            if (color[0] == '#')
                color = color.Substring(1, color.Length - 1);

            bool alpha;

            switch (color.Length)
            {
                case 8:
                    alpha = true;
                    break;
                case 6:
                    alpha = false;
                    break;
                default:
                    return false;
            }

            if (alpha)
            {
                if (ParseCol8(color, 0) < 0)
                    return false;
            }

            int from = alpha ? 2 : 0;

            if (ParseCol8(color, from + 0) < 0)
                return false;
            if (ParseCol8(color, from + 2) < 0)
                return false;
            if (ParseCol8(color, from + 4) < 0)
                return false;

            return true;
        }

        public bool IsEqualApprox(Color other)
        {
            return Mathf.IsEqualApprox(r, other.r) && Mathf.IsEqualApprox(g, other.g) && Mathf.IsEqualApprox(b, other.b) && Mathf.IsEqualApprox(a, other.a);
        }

        public override string ToString()
        {
            return String.Format("{0},{1},{2},{3}", r.ToString(), g.ToString(), b.ToString(), a.ToString());
        }

        public string ToString(string format)
        {
            return String.Format("{0},{1},{2},{3}", r.ToString(format), g.ToString(format), b.ToString(format), a.ToString(format));
        }

    }
#endif
}
