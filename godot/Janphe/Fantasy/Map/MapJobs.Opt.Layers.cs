using System;
using System.Collections.Generic;
using System.Linq;
using SkiaSharp;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        public enum Layers
        {
            opt_layers_texture,
            opt_layers_heightmap,
            opt_layers_biomes,
            opt_layers_cells,
            opt_layers_grid,
            opt_layers_coord,
            opt_layers_wind,
            opt_layers_rivers,
            opt_layers_relief,
            opt_layers_religions,
            opt_layers_cultures,
            opt_layers_states,
            opt_layers_provinces,
            opt_layers_zones,
            opt_layers_borders,
            opt_layers_routes,
            opt_layers_temperature,
            opt_layers_population,
            opt_layers_precipitation,
            opt_layers_labels,
            opt_layers_icons,
            opt_layers_markers,
            opt_layers_rulers,
            opt_layers_scale,
            count
        }
        private bool[] layersOn = new bool[(int)Layers.count];
        private bool isLayersOn(Layers l) => layersOn[(int)l];
        public ref bool[] LayersOn => ref layersOn;

        private enum Cells
        {
            cells_vert,
            cells_line,
            cells_region,
            cells_side,
            count
        }
        private bool[] cellsOn = new bool[(int)Cells.count];
        private bool isCellsOn(Cells c) => cellsOn[(int)c];
        public ref bool[] CellsOn => ref cellsOn;


        private void initLayers()
        {
            layersOn[(int)Layers.opt_layers_texture] = true;
            layersOn[(int)Layers.opt_layers_states] = true;
            layersOn[(int)Layers.opt_layers_labels] = true;

            cellsOn[(int)Cells.cells_region] = true;
            cellsOn[(int)Cells.cells_side] = true;
        }

        public string[] Get_Layers()
        {
            string _(int i)
            {
                return App.Tr(((MapJobs.Layers)i).ToString());
            }
            return LayersOn.map((b, i) => _(i)).ToArray();
        }

        public int[] Get_On_Layers()
        {
            return LayersOn.map((b, i) => b ? i : -1).filter(i => i >= 0).ToArray();
        }

        public void On_Layers_Toggled(int[] layers)
        {
            var layersOn = LayersOn;
            layersOn.forEach((l, i) => layersOn[i] = false);
            layers.forEach(i => layersOn[i] = true);
        }
    }
}
