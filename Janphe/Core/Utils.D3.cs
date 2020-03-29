using System;
using System.Linq;
using static Janphe.D3;

namespace Janphe
{
    public partial class Utils
    {
        // random number (normal or gaussian distribution)
        public static double gauss(double expected = 100, double deviation = 30, double min = 0, double max = 300, int round = 0)
        {
            return rn(Math.Max(Math.Min(randomNormal(expected, deviation)(), max), min), round);
        }

        public static string[] getColors(int number)
        {
            // const c12 = d3.scaleOrdinal(d3.schemeSet3);
            string[] c12 = { "#dababf", "#fb8072", "#80b1d3", "#fdb462", "#b3de69", "#fccde5", "#c6b9c1", "#bc80bd", "#ccebc5", "#ffed6f", "#8dd3c7", "#eb8de7" };
            var cRB = Rainbow;
            var colors = range(number)
                .Select(i => i < 12 ? c12[i] : cRB((i - 12) / (number - 12)).hex())
                .ToArray();
            shuffle(colors);
            //debug.selectAll("circle").data(colors).enter().append("circle").attr("r", 15).attr("cx", (d,i) => 60 + i * 40).attr("cy", 20).attr("fill", d => d);
            return colors;
        }
        public static string getRandomColor() { return Rainbow(Random.NextDouble()).hex(); }
        public static string getMixedColor(string color, double mix = .2, double bright = .3)
        {
            var c = (!string.IsNullOrEmpty(color) && color[0] == '#') ? color : getRandomColor();
            return interpolate(c, getRandomColor())(mix).brighter((float)bright).hex();
        }

    }
}
