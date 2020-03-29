using System;

namespace Janphe.Fantasy.Map
{
    internal class Map1Temperatures
    {
        private Grid grid { get; set; }
        private Grid.Cells cells { get; set; }
        private int graphHeight { get; set; }
        private MapJobs.Coordinates mapCoordinates { get; set; }

        private double temperatureEquatorInput { get; set; }
        private double temperaturePoleInput { get; set; }
        private double heightExponentInput { get; set; }


        public Map1Temperatures(MapJobs map)
        {
            grid = map.grid;
            cells = grid.cells;

            graphHeight = map.Options.Height;
            mapCoordinates = map.mapCoordinates;

            temperatureEquatorInput = map.Options.TemperatureEquatorInput;
            temperaturePoleInput = map.Options.TemperaturePoleInput;
            heightExponentInput = map.Options.HeightExponentInput;
        }

        public void calculateTemperatures()
        {
            Debug.Log($"calculateTemperatures {temperatureEquatorInput} {temperaturePoleInput} {heightExponentInput}");

            var temp = new sbyte[cells.i.Length];

            var tEq = +temperatureEquatorInput;
            var tPole = +temperaturePoleInput;
            var tDelta = tEq - tPole;

            //var msg = new List<string>();
            //msg.Add($"{tEq} {tPole} {heightExponentInput}");

            var range = D3.range(0, cells.i.Length, (int)grid.cellsX);
            foreach (var r in range)
            {
                var y = grid.points[r][1];
                var lat = Math.Abs(mapCoordinates.latN - y / graphHeight * mapCoordinates.latT);
                var initTemp = tEq - lat / 90 * tDelta;
                for (var i = r; i < r + grid.cellsX; i++)
                {
                    temp[i] = (sbyte)(initTemp - convertToFriendly(cells.r_height[i]));
                    //msg.Add($"{r} {i} {temp[i]}");
                }
            }
            //DebugHelper.SaveArray("calculateTemperatures.txt", msg);
            cells.temp = temp;
        }

        // temperature decreases by 6.5 degree C per 1km
        private double convertToFriendly(double h)
        {
            if (h < 20) return 0;
            var exponent = +heightExponentInput;
            var height = Math.Pow(h - 18, exponent);
            return Utils.rn(height / 1000 * 6.5);
        }
    }
}
