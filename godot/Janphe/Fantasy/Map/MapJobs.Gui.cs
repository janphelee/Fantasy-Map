using System;
using System.Collections.Generic;
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

        private static readonly string[] fonts = {
            "AlmendraSC-Regular.ttf"
        };
        private Dictionary<string, SKTypeface> faces = new Dictionary<string, SKTypeface>();

        private void initFonts()
        {
            fonts.forEach(s =>
            {
                var file = new Godot.File();
                file.Open($"res://fonts/{s}", Godot.File.ModeFlags.Read);
                var buf = file.GetBuffer((int)file.GetLen());
                file.Dispose();

                var data = SKData.CreateCopy(buf);
                faces[s] = SKTypeface.FromData(data);
                data.Dispose();
            });
        }
        private SKTypeface getFace(string s) => faces.ContainsKey(s) ? faces[s] : null;
        
    }
}
