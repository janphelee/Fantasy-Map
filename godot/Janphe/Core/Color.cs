using System;
using System.Runtime.InteropServices;

namespace Janphe
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Color : IEquatable<Color>
    {
        //(255, 255, 255, 1)
        private float r { get; set; }
        private float g { get; set; }
        private float b { get; set; }
        private float a { get; set; }

        public float R { get { return r / 255; } set { r = value * 255; } }
        public float G { get { return g / 255; } set { g = value * 255; } }
        public float B { get { return b / 255; } set { b = value * 255; } }
        public float A => a;

        private byte f2b(float c) => (byte)Mathf.Clamp((int)Math.Round(c), 0, 255);

        public byte r8
        {
            get => f2b(r);
            set => r = value;
        }

        public byte g8
        {
            get => f2b(g);
            set => g = value;
        }

        public byte b8
        {
            get => f2b(b);
            set => b = value;
        }

        public byte a8
        {
            get
            {
                return (byte)Math.Round(a * 255);
            }
            set
            {
                a = value / 255.0f;
            }
        }

        //(1, 1, 1, 1)
        public Color(float r, float g, float b, float a = 1.0f)
        {
            this.r = r * 255;
            this.g = g * 255;
            this.b = b * 255;
            this.a = a;
        }
        //(255, 255, 255, 255)
        public Color(byte r, byte g, byte b, byte a = 255)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a / 255f;
        }

        public Color Opacity(float a)
        {
            this.a = a;
            return this;
        }

        public override bool Equals(object obj)
        {
            if (obj is Color)
            {
                return Equals((Color)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode() ^ a.GetHashCode();
        }

        public bool Equals(Color other)
        {
            return r == other.r && g == other.g && b == other.b && a == other.a;
        }

        public string rgb()
        {
            return $"rgb({r8}, {g8}, {b8})";
        }

        public int ToArgb32()
        {
            int c = a8;
            c <<= 8;
            c |= r8;
            c <<= 8;
            c |= g8;
            c <<= 8;
            c |= b8;

            return c;
        }

        public static Color operator +(Color left, Color right)
        {
            left.r += right.r;
            left.g += right.g;
            left.b += right.b;
            left.a += right.a;
            return left;
        }

        public static Color operator -(Color left, Color right)
        {
            left.r -= right.r;
            left.g -= right.g;
            left.b -= right.b;
            left.a -= right.a;
            return left;
        }

        public static Color operator -(Color color)
        {
            return Color.White - color;
        }

        public static Color operator *(Color color, float scale)
        {
            color.r = color.r8 * scale;
            color.g = color.g8 * scale;
            color.b = color.b8 * scale;
            color.a *= scale;
            return color;
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
