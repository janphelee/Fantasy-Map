using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Janphe.Fantasy.Map
{
    internal class Options
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

        public struct Value
        {
            public float min;
            public float max;
            public float value;
        }

        public bool NeedUpdate { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int MapSeed { get; set; }

        public int Year { get; set; }
        public string Era { get; set; }

        public int PointsNumber { get; set; }//[1, 10] => [10k, 100k]

        public string MapName { get; set; }
        public string MapTemplate { get; set; }

        public int CulturesNumber { get; set; }//[1, 15]
        public string CulturesSet { get; set; }
        public int CulturesSet_DataMax { get; set; }

        public int NeutralInput { get; set; }
        public int PowerInput { get; set; }
        public int ManorsInput { get; set; }

        public int StatesNumber { get; set; }//[0, 99]
        public int StatesNeutral { get; set; }
        public int ProvincesRatio { get; set; }//[0, 100]
        public int ProvincesInput { get; set; }

        public float SizeVariety { get; set; }//[0, 10]
        public float GrowthRate { get; set; }//[0.1, 2]
        public int TownsNumber { get; set; } = -1;//[0, 999] auto
        public int RegionsNumber { get; set; } = 0;
        public int ReligionsNumber { get; set; } = 0;//[0, 50]

        public int PrecipitationInput { get; set; }//[0, 500]
        public int[] WindsInput { get; set; }

        private Value _temperatureEquator;
        public ref Value TemperatureEquator => ref _temperatureEquator;

        public string TemperatureScale { get; set; }

        public float TemperaturePoleInput { get; set; }
        public float HeightExponentInput { get; set; }

        public string terrainSet { get; set; } = "simple";
        public float terrainDensity { get; set; } = 0.4f;
        public float terrainSize { get; set; } = 1f;


        public Options()
        {
            NeedUpdate = true;

            Width = 1153;
            Height = 717;
            MapSeed = 1;
            MapTemplate = "Archipelago";

            Year = 166;
            Era = "Helbo Era";

            PointsNumber = 1;
            PrecipitationInput = 107;
            WindsInput = new int[] { 270, 90, 225, 315, 135, 315 };
            TemperaturePoleInput = -28;//[-30,30]
            HeightExponentInput = 1.8f;//[1.5,2.1]

            CulturesNumber = 12;
            CulturesSet = "european";
            CulturesSet_DataMax = 15;
            PowerInput = 2;
            NeutralInput = 1;
            RegionsNumber = 15;
            ReligionsNumber = 5;

            ManorsInput = 1000;// burgs
            StatesNeutral = 1;

            ProvincesInput = 38;

            TemperatureEquator = new Options.Value() { min = -30, max = 30, value = 29 };
            TemperatureScale = "°C";

            _initFuncDict();
        }

        public string GetOptions()
        {
            // 使用JObject键名会乱序
            //var obj = new Dictionary<string, string>();
            var obj = new JObject();
            for (var i = 0; i < (int)MapSetting.count; ++i)
            {
                var k = ((MapSetting)i).ToString();
                obj.Add(k, App.Tr(k));
            }
            return JsonConvert.SerializeObject(obj);
        }

        public JObject ToJson()
        {
            var obj = new JObject();
            foreach (var kv in _funcDict)
            {
                var biz = kv.Value(null);
                obj.Add(kv.Key, biz);
            }
            return obj;
        }

        public void FromJson(JObject obj)
        {
            foreach (var kv in obj)
            {
                if (_funcDict.ContainsKey(kv.Key))
                {
                    _funcDict[kv.Key].Invoke(kv.Value);
                }
            }
            NeedUpdate = true;
        }

        private Dictionary<string, Func<JToken, JToken>> _funcDict { get; set; }

        private void _initFuncDict()
        {
            _funcDict = new Dictionary<string, Func<JToken, JToken>>();

            new Func<JToken, JToken>[] {
                _on_opt_map_size,
                _on_opt_map_seed,
                _on_opt_map_name,
                _on_opt_map_year,
                _on_opt_map_era,
                _on_opt_map_template,
                _on_opt_map_points_n,
                _on_opt_map_cultures_n,
                _on_opt_map_cultures_set,
                _on_opt_map_states_n,
                _on_opt_map_provinces_ratio,
                _on_opt_map_size_variety,
                _on_opt_map_growth_rate,
                _on_opt_map_towns_n,
                _on_opt_map_religions_n,
            }
            .forEach((func, i) =>
            {
                var key = ((MapSetting)i).ToString();
                _funcDict.Add(key, func);
            });
        }

        private JToken _on_opt_map_size(JToken biz)
        {
            if (biz != null)
            {
                Width = biz.Value<int>("w");
                Height = biz.Value<int>("h");
            }
            else
            {
                biz = JToken.Parse($"{{w:{Width},h:{Height}}}");
            }
            return biz;
        }
        private JToken _on_opt_map_seed(JToken biz)
        {
            if (biz != null)
            {
                MapSeed = biz.Value<int>();
            }
            else
            {
                biz = MapSeed;
            }
            return biz;
        }
        private JToken _on_opt_map_name(JToken biz)
        {
            if (biz != null)
            {
                MapName = biz.Value<string>();
            }
            else
            {
                biz = MapName;
            }
            return biz;
        }
        private JToken _on_opt_map_year(JToken biz)
        {
            if (biz != null)
            {
                Year = biz.Value<int>();
            }
            else
            {
                biz = Year;
            }
            return biz;
        }
        private JToken _on_opt_map_era(JToken biz)
        {
            if (biz != null)
            {
                Era = biz.Value<string>();
            }
            else
            {
                biz = Era;
            }
            return biz;
        }
        private JToken _on_opt_map_template(JToken biz)
        {
            if (biz != null)
            {
                MapTemplate = biz.Value<string>();
            }
            else
            {
                biz = MapTemplate;
            }
            return biz;
        }
        private JToken _on_opt_map_points_n(JToken biz)
        {
            if (biz != null)
            {
                PointsNumber = biz.Value<int>();
            }
            else
            {
                biz = PointsNumber;
            }
            return biz;
        }
        private JToken _on_opt_map_cultures_n(JToken biz)
        {
            if (biz != null)
            {
                CulturesNumber = biz.Value<int>();
            }
            else
            {
                biz = CulturesNumber;
            }
            return biz;
        }
        private JToken _on_opt_map_cultures_set(JToken biz)
        {
            if (biz != null)
            {
                CulturesSet = biz.Value<string>();
            }
            else
            {
                biz = CulturesSet;
            }
            return biz;
        }
        private JToken _on_opt_map_states_n(JToken biz)
        {
            if (biz != null)
            {
                StatesNumber = biz.Value<int>();
            }
            else
            {
                biz = StatesNumber;
            }
            return biz;
        }
        private JToken _on_opt_map_provinces_ratio(JToken biz)
        {
            if (biz != null)
            {
                ProvincesRatio = biz.Value<int>();
            }
            else
            {
                biz = ProvincesRatio;
            }
            return biz;
        }
        private JToken _on_opt_map_size_variety(JToken biz)
        {
            if (biz != null)
            {
                SizeVariety = biz.Value<float>();
            }
            else
            {
                biz = SizeVariety;
            }
            return biz;
        }
        private JToken _on_opt_map_growth_rate(JToken biz)
        {
            if (biz != null)
            {
                GrowthRate = biz.Value<float>();
            }
            else
            {
                biz = GrowthRate;
            }
            return biz;
        }
        private JToken _on_opt_map_towns_n(JToken biz)
        {
            if (biz != null)
            {
                TownsNumber = biz.Value<int>();
            }
            else
            {
                biz = TownsNumber;
            }
            return biz;
        }
        private JToken _on_opt_map_religions_n(JToken biz)
        {
            if (biz != null)
            {
                ReligionsNumber = biz.Value<int>();
            }
            else
            {
                biz = ReligionsNumber;
            }
            return biz;
        }
    }
}
