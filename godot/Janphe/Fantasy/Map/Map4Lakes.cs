namespace Janphe.Fantasy.Map
{
    internal class Map4Lakes
    {
        private Grid pack { get; set; }
        private string templateInput { get; set; }

        public Map4Lakes(MapJobs map)
        {
            pack = map.pack;
            templateInput = map.Options.MapTemplate;
        }

        // temporary elevate some lakes to resolve depressions and flux the water to form an open (exorheic) lake
        public void elevateLakes()
        {
            if (templateInput == "Atoll") return; // no need for Atolls
            var cells = pack.cells;
            var features = pack.features;

            var maxCells = cells.i.Length / 100; // size limit; let big lakes be closed (endorheic)
            foreach (var i in cells.i)
            {
                if (cells.r_height[i] >= 20) continue;
                if (features[cells.f[i]].group != "freshwater" || features[cells.f[i]].cells > maxCells) continue;
                cells.r_height[i] = 20;
                //debug.append("circle").attr("cx", cells.p[i][0]).attr("cy", cells.p[i][1]).attr("r", .5).attr("fill", "blue");
            }
        }


    }
}
