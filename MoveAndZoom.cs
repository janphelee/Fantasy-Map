using Godot;

namespace FantasyMap
{
    class MoveAndZoom : Control
    {
        [Export] private NodePath mapJobsScreen { get; set; }
        private MapJobsScreen screen { get; set; }

        public override void _Ready()
        {
            CallDeferred("start");
            screen = GetNode<MapJobsScreen>(mapJobsScreen);
        }

        //=====================================================================
        private Vector2 _absSize { get; set; }
        private Vector2 locPosition;
        private float locScale;

        private void start()
        {
            _absSize = RectSize;//不能在ready时候读取
            locPosition = new Vector2(0, 0);
            locScale = 1;
        }

        //=====================================================================
        private bool buttonPressed = false;
        private Vector2 lastMousePosition;

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton)
            {
                var evt = (InputEventMouseButton)@event;
                switch ((ButtonList)evt.ButtonIndex)
                {
                    case ButtonList.Left:
                        buttonPressed = evt.Pressed;
                        if (buttonPressed)
                            lastMousePosition = evt.GlobalPosition;
                        else
                            moveTo(evt.GlobalPosition - lastMousePosition);
                        break;
                    case ButtonList.WheelUp:
                        lastMousePosition = evt.GlobalPosition;
                        if (evt.Pressed)
                            zoomBy(_absSize * 0.1f);
                        else
                            zoomTo(_absSize * 0.1f);
                        break;
                    case ButtonList.WheelDown:
                        lastMousePosition = evt.GlobalPosition;
                        if (evt.Pressed)
                            zoomBy(_absSize * -0.1f);
                        else
                            zoomTo(_absSize * -0.1f);
                        break;
                }
            }
            if (@event is InputEventMouseMotion)
            {
                var evt = (InputEventMouseMotion)@event;
                if (buttonPressed)
                    moveBy(evt.GlobalPosition - lastMousePosition);
            }

            screen.on_gui_input(@event);
        }

        //=====================================================================
        private void moveTo(Vector2 p)
        {
            locPosition += p / locScale;
        }
        private void moveBy(Vector2 p)
        {
            p = locPosition + p / locScale;

            screen._on_ViewportContainer_MoveTo(p);
        }

        //=====================================================================
        private Vector2 _pivotPS(Vector2 p, float s) => _pivotPS(p, s, lastMousePosition);
        private Vector2 _pivotPS(Vector2 p, float s, Vector2 m) => ((m / s) - p) / _absSize;

        private void zoomTo(Vector2 p)
        {
            var scale = locScale * (1 + p.x / _absSize.x);
            p = locPosition * locScale / scale + (locScale - scale) * _absSize * _pivotPS(locPosition, locScale) / scale;

            locPosition = p;
            locScale = scale;
            //Debug.Log($"zoomTo 1111 locPosition:{locPosition} locScale:{locScale}");

        }
        private void zoomBy(Vector2 p)
        {
            var scale = locScale * (1 + p.x / _absSize.x);
            p = locPosition * locScale / scale + (locScale - scale) * _absSize * _pivotPS(locPosition, locScale) / scale;
            //Debug.Log($"zoomBy 2222 _pivot:{_pivotPS(locPosition, locScale)}=>{_pivotPS(p, scale)} p:{locPosition}=>{p}");

            screen._on_ViewportContainer_ZoomTo(p, scale);
        }
    }
}
