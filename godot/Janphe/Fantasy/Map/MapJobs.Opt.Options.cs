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
        public enum MapSetting
        {
            opt_map_size,
            opt_map_seed,
            opt_map_name,
            opt_map_year,
            opt_map_era,
            opt_map_template,
            opt_map_points_n,
            opt_map_cultures_n,
            opt_map_cultures_set,
            opt_map_states_n,
            opt_map_provinces_ratio,
            opt_map_size_variety,
            opt_map_growth_rate,
            opt_map_towns_n,
            opt_map_religions_n,
            count
        }

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
            Options = new Options()
            {
                MapSeed = 1,
                Width = 1153,
                Height = 717,
                MapTemplate = "Archipelago",

                PointsNumber = 1,
                PrecipitationInput = 107,
                WindsInput = new int[] { 270, 90, 225, 315, 135, 315 },
                TemperaturePoleInput = -28,//[-30,30]
                HeightExponentInput = 1.8f,//[1.5,2.1]

                CulturesNumber = 12,
                CulturesSet = "european",
                CulturesSet_DataMax = 15,
                PowerInput = 2,
                NeutralInput = 1,
                RegionsNumber = 15,
                ReligionsNumber = 5,

                ManorsInput = 1000,// burgs
                StatesNeutral = 1,

                ProvincesInput = 38,
            };
            Options.TemperatureEquator = new Options.Value() { min = -30, max = 30, value = 29 };
            Options.TemperatureScale = "°C";
        }

        public string[] Get_Options()
        {
            string _(int i)
            {
                return App.Tr(((MapSetting)i).ToString());
            }
            return D3.range((int)MapSetting.count).map(i => _(i)).ToArray();
        }

        public int[] Get_On_Options()
        {
            return LayersOn.map((b, i) => b ? i : -1).filter(i => i >= 0).ToArray();
        }

        public void On_Options_Toggled(JObject biz)
        {
            var layersOn = LayersOn;
            layersOn.forEach((l, i) => layersOn[i] = false);
            layers.forEach(i => layersOn[i] = true);
        }
    }
}
