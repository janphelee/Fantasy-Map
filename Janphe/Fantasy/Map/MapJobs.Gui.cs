using System.Numerics;
using ImGuiNET;

namespace Janphe.Fantasy.Map
{
    partial class MapJobs
    {
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
        private bool isLayerOn(Layers l) => layers[(int)l];

        private enum Cells
        {
            cells_vert,
            cells_line,
            cells_region,
            cells_side,
            count
        }
        private bool isCellsOn(Cells c) => cells[(int)c];


        private bool open_1 = true;
        private bool[] layers = new bool[(int)Layers.count];
        private bool[] cells = new bool[(int)Cells.count];

        private void initLayers()
        {
            layers[(int)Layers.opt_layers_heightmap] = true;
        }


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
                var tab_bar_flags = ImGuiTabBarFlags.None;
                if (ImGui.BeginTabBar("opt_wnd_tab", tab_bar_flags))
                {
                    if (ImGui.BeginTabItem(_("opt_layers")))
                    {
                        ImGui.Text(_("opt_layers_preset"));
                        ImGui.SameLine();
                        int sl = 0;
                        ImGui.Combo("?", ref sl, new string[] {
                            "aaaaaaaaaaaaa",
                            "bbbbbbbbbbbbb",
                            "bbbbbbbbbbbbb",
                            "bbbbbbbbbbbbb",
                            "bbbbbbbbbbbbb",
                            "bbbbbbbbbbbbb",
                            "bbbbbbbbbbbbb"
                        }, 5);
                        ImGui.Separator();

                        ImGui.Text(_("opt_layers_displayed"));
                        ImGui.Columns(3, "opt_layers_cols", false);  // 3-ways, no border
                        for (int n = 0; n < layers.Length; n++)
                        {
                            var last = layers[n];
                            if (ImGui.Selectable($"{_(((Layers)n).ToString())}", ref layers[n]))
                            {
                                if (!needUpdate)
                                    needUpdate = last != layers[n];
                            }
                            ImGui.NextColumn();
                        }
                        ImGui.Columns(1);
                        ImGui.Separator();

                        if (isLayerOn(Layers.opt_layers_cells))
                        {
                            ImGui.Columns(2, "opt_layers_cells_cols", false);
                            for (var n = 0; n < cells.Length; ++n)
                            {
                                var last = cells[n];
                                if (ImGui.Selectable($"{_(((Cells)n).ToString())}", ref cells[n]))
                                {
                                    if (!needUpdate)
                                        needUpdate = last != cells[n];
                                }
                                ImGui.NextColumn();
                            }
                            ImGui.Columns(1);
                            ImGui.Separator();
                        }

                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem(_("opt_style")))
                    {
                        ImGui.Text("This is the Broccoli tab!\nblah blah blah blah blah");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem(_("opt_options")))
                    {
                        ImGui.Text("This is the Cucumber tab!\nblah blah blah blah blah");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem(_("opt_tools")))
                    {
                        ImGui.Text("This is the Cucumber tab!\nblah blah blah blah blah");
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem(_("opt_about")))
                    {
                        ImGui.Text(_("opt_about_info"));
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
            }
            ImGui.End();
        }

    }
}
