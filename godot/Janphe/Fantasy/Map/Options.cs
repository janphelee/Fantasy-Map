namespace Janphe.Fantasy.Map
{
    internal class Options
    {
        public struct Value
        {
            public float min;
            public float max;
            public float value;
        }

        public int MapSeed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

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

    }
}
