using Godot;
using Janphe;
using Janphe.Fantasy.Map;

namespace FantasyMap
{
    class MapJobsScreen : Godot.Panel
    {
        private MapJobs _mapJobs;
        private bool _needUpdate;

        private Image image;
        private ImageTexture texture;

        private Vector2 areaSize;

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

        private void start()
        {
            areaSize = GetParentAreaSize();//不能在ready时候读取

            _mapJobs = new MapJobs();
            _mapJobs.Options.Width = (int)areaSize.x;
            _mapJobs.Options.Height = (int)areaSize.y;
            Debug.Log($"GetParentAreaSize w:{areaSize.x} h:{areaSize.y}");

            _generate();
        }

        private void _generate()
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


        private void _on_ViewportContainer_MoveTo(Vector2 position)
        {
            _mapJobs.Translate(position.x, position.y);
            _generate();
        }


        private void _on_ViewportContainer_ZoomTo(Vector2 position, float scale)
        {
            _mapJobs.Scale(scale, scale);
            _mapJobs.Translate(position.x, position.y);
            _generate();
        }

    }
}
