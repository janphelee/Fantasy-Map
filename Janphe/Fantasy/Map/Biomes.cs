using System;
using System.Collections.Generic;

namespace Janphe.Fantasy.Map
{
    internal class Biomes
    {
        public int[] i { get; set; }
        public string[] name { get; set; }
        public string[] color { get; set; }
        public byte[][] biomesMartix { get; set; }
        public byte[] habitability { get; set; }
        public byte[] iconsDensity { get; set; }
        public string[][] icons { get; set; }
        public byte[] cost { get; set; }

        public int getBiomeId(double moisture, int temperature, int height)
        {
            if (temperature < -5) return 11; // permafrost biome
            if (moisture > 40 && height < 25 || moisture > 24 && height > 24) return 12; // wetland biome
            var m = Math.Min((int)(moisture / 5), 4); // moisture band from 0 to 4
            var t = Math.Min(Math.Max(20 - temperature, 0), 25); // temparature band from 0 to 25
            return biomesMartix[m][t];
        }
    }

}
