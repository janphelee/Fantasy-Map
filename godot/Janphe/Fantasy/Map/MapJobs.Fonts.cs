using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        private static readonly string[] fonts = {
            "AlmendraSC-Regular.ttf"
        };
        private Dictionary<string, SKTypeface> faces = new Dictionary<string, SKTypeface>();

        private void initFonts()
        {
            fonts.forEach(s =>
            {
                var buf = App.LoadData($"fonts/{s}");
                var data = SKData.CreateCopy(buf);
                faces[s] = SKTypeface.FromData(data);
                data.Dispose();
            });
        }
        private SKTypeface getFace(string s) => faces.ContainsKey(s) ? faces[s] : null;

    }
}
