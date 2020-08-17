using System;
using Godot;
using Janphe;
using Janphe.Fantasy.Map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        [Export] private NodePath serverApi { get; set; }

        public override void _Ready()
        {
            var api = GetNode<FileServerApi>(serverApi);
            api.AddPath("on_layers_toggled", req =>
            {
                Debug.Log($"on_layers_toggled method:{req.method} query:{req.query} body:{req.body}");
                if (req.method.Equals("GET"))
                {
                    //var j = JObject.Parse(d);
                    //var i = j["i"].Value<int>();
                    //var pressed = j["pressed"].Value<bool>();
                    //on_layers_toggled(i, pressed);
                }
                if (req.method.Equals("POST"))
                {
                    var j = JObject.Parse(req.body);
                    var i = j["i"].Value<int>();
                    var pressed = j["pressed"].Value<bool>();
                    on_layers_toggled(i, pressed);
                }

                return JsonConvert.SerializeObject(_mapJobs.LayersOn);
            });

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
                Update();// for call _Draw
            }
        }

        private void start()
        {
            areaSize = GetParentAreaSize();//不能在ready时候读取

            _mapJobs = new MapJobs();
            _mapJobs.Options.Width = (int)areaSize.x;
            _mapJobs.Options.Height = (int)areaSize.y;
            Debug.Log($"GetParentAreaSize w:{areaSize.x} h:{areaSize.y}");

            generate();
        }

        private void on_layers_toggled(int i, bool pressed)
        {
            var layersOn = _mapJobs.LayersOn;
            layersOn[i] = pressed;
            generate();
        }

        private void on_button_up()
        {
            GetTree().Quit();
        }


        public void on_gui_input(InputEvent @event)
        {
            if (@event is InputEventMouseButton)
            {
                //var evt = (InputEventMouseButton)@event;
                //switch ((ButtonList)evt.ButtonIndex)
                //{
                //    case ButtonList.Right:
                //        if (_gui != null && evt.Pressed)
                //            _gui.Visible = !_gui.Visible;
                //        break;
                //}
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
