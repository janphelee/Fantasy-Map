using System;
using System.Linq;

namespace Janphe
{
    public partial class D3
    {
        private static readonly string[][] scheme_m = new string[] {
  "e5f5e0a1d99b31a354",
  "edf8e9bae4b374c476238b45",
  "edf8e9bae4b374c47631a354006d2c",
  "edf8e9c7e9c0a1d99b74c47631a354006d2c",
  "edf8e9c7e9c0a1d99b74c47641ab5d238b45005a32",
  "f7fcf5e5f5e0c7e9c0a1d99b74c47641ab5d238b45005a32",
  "f7fcf5e5f5e0c7e9c0a1d99b74c47641ab5d238b45006d2c00441b"
        }.Select(colors).ToArray();

        public static readonly Func<float, Color> Greens = ramp(scheme_m);

        private static readonly string[][] scheme_n = new string[] {
  "f0f0f0bdbdbd636363",
  "f7f7f7cccccc969696525252",
  "f7f7f7cccccc969696636363252525",
  "f7f7f7d9d9d9bdbdbd969696636363252525",
  "f7f7f7d9d9d9bdbdbd969696737373525252252525",
  "fffffff0f0f0d9d9d9bdbdbd969696737373525252252525",
  "fffffff0f0f0d9d9d9bdbdbd969696737373525252252525000000"
        }.Select(colors).ToArray();

        public static readonly Func<float, Color> Greys = ramp(scheme_n);

        private static readonly string[][] scheme_7 = new string[] {
  "fc8d59ffffbf91cf60",
  "d7191cfdae61a6d96a1a9641",
  "d7191cfdae61ffffbfa6d96a1a9641",
  "d73027fc8d59fee08bd9ef8b91cf601a9850",
  "d73027fc8d59fee08bffffbfd9ef8b91cf601a9850",
  "d73027f46d43fdae61fee08bd9ef8ba6d96a66bd631a9850",
  "d73027f46d43fdae61fee08bffffbfd9ef8ba6d96a66bd631a9850",
  "a50026d73027f46d43fdae61fee08bd9ef8ba6d96a66bd631a9850006837",
  "a50026d73027f46d43fdae61fee08bffffbfd9ef8ba6d96a66bd631a9850006837"
        }.Select(colors).ToArray();

        public static readonly Func<float, Color> RdYlGn = ramp(scheme_7);

        private static readonly string[][] scheme_8 = new string[] {
  "fc8d59ffffbf99d594",
  "d7191cfdae61abdda42b83ba",
  "d7191cfdae61ffffbfabdda42b83ba",
  "d53e4ffc8d59fee08be6f59899d5943288bd",
  "d53e4ffc8d59fee08bffffbfe6f59899d5943288bd",
  "d53e4ff46d43fdae61fee08be6f598abdda466c2a53288bd",
  "d53e4ff46d43fdae61fee08bffffbfe6f598abdda466c2a53288bd",
  "9e0142d53e4ff46d43fdae61fee08be6f598abdda466c2a53288bd5e4fa2",
  "9e0142d53e4ff46d43fdae61fee08bffffbfe6f598abdda466c2a53288bd5e4fa2"
        }.Select(colors).ToArray();

        public static readonly Func<float, Color> Spectral = ramp(scheme_8);

        public static readonly Func<double, Color> Rainbow = (t) =>
        {
            if (t < 0 || t > 1)
                t -= Math.Floor(t);
            var ts = Math.Abs(t - 0.5);
            var h = 360 * t - 100;
            var s = 1.5 - 1.5 * ts;
            var l = 0.8 - 0.9 * ts;
            var c = Cubehelix(h, s, l);
            //rainbow 0 rgb(110, 64, 170) -100 0.75 0.35
            //Debug.Log($"rainbow { t} rgb({c.r8},{c.g8},{c.b8}) {h} {s} {l}");
            return c;
        };

        const double deg2rad = Math.PI / 180;
        const double rad2deg = 180 / Math.PI;
        const double A = -0.14861,
                     B = +1.78277,
                     C = -0.29227,
                     D = -0.90649,
                     E = +1.97294;

        public static Color Cubehelix(double h, double s, double l, double opacity = 1)
        {
            var a = s * l * (1 - l);
            var b = (h + 120) * deg2rad;
            var cosh = Math.Cos(b);
            var sinh = Math.Sin(b);

            return Rgb(
                255 * (l + a * (A * cosh + B * sinh)),
                255 * (l + a * (C * cosh + D * sinh)),
                255 * (l + a * (E * cosh)),
                opacity
                );
        }

        public static Color Rgb(double r, double g, double b, double a)
        {
            Func<double, float> clamp = t => (float)(Math.Max(0, Math.Min(255, Math.Round(t))) / 255);
            return new Color(clamp(r), clamp(g), clamp(b), (float)a);
        }
    }
}
