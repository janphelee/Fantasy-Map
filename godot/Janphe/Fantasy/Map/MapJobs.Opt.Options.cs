using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {

        /**
            canvasSize: { w: 500, h: 617 },
            mapSeed: [972402960, 1, 999999999, 1],
            mapName: "Persainy",
            pointsNumber: [10, 10, 100, 1],
            yearAndEra: { y: 166, e: "Helbo Era" },
            mapTemplate: "Island",
            culturesNumber: [11, 1, 32, 1],
            culturesSet: "All-world",
            statesNumber: [14, 0, 99, 1],
            provincesRatio: [20, 0, 100, 1],
            sizeVariety: [2.93, 0, 10, 0],
            growthRate: [1.4, 0.1, 2, 0],
            townsNumber: [1000, 0, 1000, 1],
            religionsNumber: [4, 0, 50, 1],
        */
        public Options Options { get; set; }

        private void initOptions()
        {
            Options = new Options();
        }

        public string Get_Options() => Options.GetOptions();

        public JObject Get_On_Options() => Options.ToJson();

        public void On_Options_Toggled(JObject obj) => Options.FromJson(obj);
    }
}
