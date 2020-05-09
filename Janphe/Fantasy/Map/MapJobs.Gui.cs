using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using ImGuiNET;
using SkiaSharp;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
        private enum Tabs
        {
            opt_layers,
            opt_style,
            opt_options,
            opt_tools,
            opt_about,
            count
        }
        private delegate void DrawTab(Gui gui, ref bool needUpdate, Func<string, string> _);

        private int tabIndex = 0;
        private DrawTab[] drawTabs = new DrawTab[(int)Tabs.count];


        private enum Layers
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


        private bool open_1 = true;
        private int locale = -1;

        private void initLayers()
        {
            layersOn[(int)Layers.opt_layers_texture] = true;
            layersOn[(int)Layers.opt_layers_states] = true;
            layersOn[(int)Layers.opt_layers_labels] = true;

            cellsOn[(int)Cells.cells_region] = true;
            cellsOn[(int)Cells.cells_side] = true;

            var data = Gui.GetLocales();
            var lang = Gui.GetLocale();
            locale = Array.FindIndex(data, d => d.StartsWith(lang));

            drawTabs[(int)Tabs.opt_layers] = drawTabLayers;
            drawTabs[(int)Tabs.opt_style] = drawTabStyle;
            drawTabs[(int)Tabs.opt_options] = drawTabOptions;
            drawTabs[(int)Tabs.opt_tools] = drawTabTools;
            drawTabs[(int)Tabs.opt_about] = drawTabAbout;
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

        public void OnGui(Gui gui, ref bool needUpdate)
        {
            string _(string s) => gui._(s);

            //ImGui.ShowDemoWindow();

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                open_1 = !open_1;

            if (!open_1)
                return;

            var flags =
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.None;


            ImGui.SetNextWindowPos(new Vector2(8, 8), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(330, 380), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("opt_wnd", flags))
            {
                ImGui.Columns((int)Tabs.count);
                for (var n = 0; n < (int)Tabs.count; ++n)
                {
                    var s = ((Tabs)n).ToString();
                    if (ImGui.Selectable(_(s), n == tabIndex))
                    { tabIndex = n; }
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
                ImGui.Separator();

                drawTabs[tabIndex]?.Invoke(gui, ref needUpdate, _);
            }
            ImGui.End();
        }

        int sl = 0;
        string[] presets = new string[] {
                            "aaaaa",
                            "bbbbb",
                            "bbbbb",
                            "bbbbb",
                            "bbbbb",
                            "bbbbb",
                            "bbbbb"
                        };
        private void drawTabLayers(Gui gui, ref bool needUpdate, Func<string, string> _)
        {
            ImGui.Text(_("opt_layers_preset"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("?", ref sl, presets, 5))
            {
                Debug.Log($"opt_layers_preset:{sl} {presets[sl]}");
            }
            ImGui.Separator();

            ImGui.Text(_("opt_layers_displayed"));
            ImGui.Columns(3, "opt_layers_cols", false);  // 3-ways, no border
            for (var n = 0; n < layersOn.Length; n++)
            {
                var last = layersOn[n];
                var s = ((Layers)n).ToString();
                if (ImGui.Selectable($"{_(s)}", ref layersOn[n]))
                {
                    if (!needUpdate)
                        needUpdate = last != layersOn[n];
                }
                ImGui.NextColumn();
            }
            ImGui.Columns(1);
            ImGui.Separator();

            if (isLayersOn(Layers.opt_layers_cells))
            {
                ImGui.Spacing();
                ImGui.Text(_("cells_draw_tells"));
                ImGui.Columns(2, "opt_layers_cells_cols", false);

                bool last;
                string s;

                for (var n = 0; n < cellsOn.Length; n++)
                {
                    last = cellsOn[n];
                    s = ((Cells)n).ToString();

                    //! label 需要唯一性，否则鼠标点击无效
                    var signed = n < 2 ? '#' : '^';
                    if (ImGui.Checkbox($"{signed} {_(s)}", ref cellsOn[n]))
                    {
                        if (!needUpdate)
                            needUpdate = last != cellsOn[n];
                    }
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
                ImGui.Separator();
            }
        }

        private void drawTabStyle(Gui gui, ref bool needUpdate, Func<string, string> _) { }
        private void drawTabOptions(Gui gui, ref bool needUpdate, Func<string, string> _) { }
        private void drawTabTools(Gui gui, ref bool needUpdate, Func<string, string> _) { }
        private void drawTabAbout(Gui gui, ref bool needUpdate, Func<string, string> _)
        {
            ImGui.Text(_("opt_about_info"));
            ImGui.Separator();

            if (locale >= 0)
            {
                ImGui.Spacing();
                ImGui.Text(_("opt_about_switch_language"));

                var data = Gui.GetLocales();
                for (var i = 0; i < data.Length; ++i)
                {
                    bool selected = i == locale;
                    if (ImGui.Selectable($"{_(data[i])}", ref selected))
                    {
                        if (selected && i != locale)
                        {
                            Gui.SetLocale(data[i]);
                            locale = i;
                        }
                    }
                }
            }
        }
    }
}
