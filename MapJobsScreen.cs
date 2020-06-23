using System;
using Godot;
using Janphe;
using Janphe.Fantasy.Map;

namespace FantasyMap
{
    using Random = Janphe.Random;

    class MapJobsScreen : Panel
    {
        private MapJobs _mapJobs;
        private bool _needUpdate;

        private Image image;
        private ImageTexture texture;

        private Vector2 areaSize;
        private Control _gui;
        private int locale = -1;

        //[Export] private Theme mapTheme;

        public override void _Ready()
        {
            CallDeferred("start");
        }

        public override void _Process(float delta)
        {
            if (_needUpdate)
            {
                _needUpdate = false;

                if (texture == null)
                {
                    texture = new ImageTexture();
                    texture.CreateFromImage(image);
                }
                else
                {
                    if (image.GetFormat() == texture.GetFormat())
                        texture.SetData(image);
                }
                Update();
            }
        }

        private enum Tabs
        {
            opt_layers,
            opt_style,
            opt_options,
            opt_tools,
            opt_about,
            count
        }

        private void start()
        {
            areaSize = GetParentAreaSize();//不能在ready时候读取

            _mapJobs = new MapJobs();
            _mapJobs.Options.Width = (int)areaSize.x;
            _mapJobs.Options.Height = (int)areaSize.y;
            Debug.Log($"GetParentAreaSize w:{areaSize.x} h:{areaSize.y}");

            var data = App.GetLocales();
            var lang = App.GetLocale();
            locale = Array.FindIndex(data, d => d.StartsWith(lang));

            var tabs = new TabContainer();
            //tabs.AnchorBottom = tabs.AnchorRight = 1f;
            tabs.SetPosition(new Vector2(8, 8));
            tabs.SetSize(new Vector2(330, 380));
            tabs.TabAlign = TabContainer.TabAlignEnum.Center;

            AddChild(tabs);
            _gui = tabs;

            var draws = new Action<Control, int, string>[]{
                drawTabLayers,
                null,
                null,
                null,
                drawTabAbout
            };
            for (var i = 0; i < (int)Tabs.count; ++i)
            {
                var s = ((Tabs)i).ToString();

                var tab = new VBoxContainer();
                tabs.AddChild(tab);
                tabs.SetTabTitle(i, s);

                draws[i]?.Invoke(tab, i, s);
            }

            generate();
        }

        private void drawTabLayers(Control tab, int i, string s)
        {
            var grid = new GridContainer();
            grid.Columns = 3;
            grid.AnchorRight = 1f;

            var layersOn = _mapJobs.LayersOn;
            for (var n = 0; n < layersOn.Length; ++n)
            {
                var button = new Button();
                button.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
                button.ToggleMode = true;
                button.Pressed = layersOn[n];
                button.Text = ((MapJobs.Layers)n).ToString();
                button.Connect("toggled", this, nameof(on_layers_toggled), new Godot.Collections.Array() { n });
                grid.AddChild(button);
            }

            tab.AddChild(grid);
        }

        private void on_layers_toggled(bool pressed, int i)
        {
            var layersOn = _mapJobs.LayersOn;
            layersOn[i] = pressed;
            generate();
        }

        private void drawTabAbout(Control tab, int i, string s)
        {
            var label = new Label();
            label.Text = "opt_about_info";

            tab.AddChild(label);
            tab.AddChild(new HSeparator());

            var hbox = new HBoxContainer();
            hbox.AnchorRight = 1;
            hbox.Alignment = BoxContainer.AlignMode.Begin;
            tab.AddChild(hbox);

            label = new Label();
            label.Text = "opt_about_switch_language";
            hbox.AddChild(label);

            var option = new OptionButton();
            App.GetLocales().forEach(d => option.AddItem(d));
            option.Selected = locale;
            option.Connect("item_selected", this, nameof(on_item_selected));
            hbox.AddChild(option);

            var button = new Button();
            button.Text = "opt_quit";
            button.Connect("button_up", this, nameof(on_button_up));
            tab.AddChild(new Label());
            tab.AddChild(button);
        }

        private void on_item_selected(int i)
        {
            var data = App.GetLocales();
            App.SetLocale(data[i]);
        }

        private void on_button_up()
        {
            GetTree().Quit();
        }


        public void on_gui_input(InputEvent @event)
        {
            if (@event is InputEventMouseButton)
            {
                var evt = (InputEventMouseButton)@event;
                switch ((ButtonList)evt.ButtonIndex)
                {
                    case ButtonList.Right:
                        if (_gui != null && evt.Pressed)
                            _gui.Visible = !_gui.Visible;
                        break;
                }
            }
        }


        private void generate()
        {
            _mapJobs.processAsync(t =>
            {
                Debug.Log($"MapJobs.processAsync t:{t}ms");

                var bitmap = _mapJobs.Bitmap;
                image = new Image();
                image.CreateFromData(bitmap.Width, bitmap.Height, false, Image.Format.Rgba8, bitmap.Bytes);
                _needUpdate = true;
            });
        }

        public override void _Draw()
        {
            if (texture == null)
                return;

            var rr = GetParentAreaSize();
            var ts = texture.GetSize();
            DrawTexture(texture, (rr - ts) / 2);
        }


        public void _on_ViewportContainer_MoveTo(Vector2 position)
        {
            _mapJobs.Translate(position.x, position.y);
            generate();
        }


        public void _on_ViewportContainer_ZoomTo(Vector2 position, float scale)
        {
            _mapJobs.Scale(scale, scale);
            _mapJobs.Translate(position.x, position.y);
            generate();
        }

    }
}
