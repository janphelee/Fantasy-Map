namespace Janphe
{
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using ImGuiNET;
#if GODOT
    using Godot;
    using GdVect2 = Godot.Vector2;
    using GdColor = Godot.Color;

    public partial class Gui : Control
    {
        public static string[] GetLocales()
        {
            var loaded = TranslationServer.GetLoadedLocales();
            var locales = new string[loaded.Count];
            for (var i = 0; i < locales.Length; ++i)
                locales[i] = loaded[i] as string;
            return locales;
        }
        public static void SetLocale(string locale)
        {
            TranslationServer.SetLocale(locale);
        }

        private Texture texture;

        public override void _Ready()
        {
            Engine.TargetFps = 50;//限制帧率

            OS.SetImeActive(true);//可以用输入法

            initImGui();

            var io = ImGui.GetIO();

            IntPtr pixels;
            int width, height;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);
            Debug.Log($"GetTexDataAsRGBA32 w:{width} h:{height}");

            var data = new byte[width * height * 4];
            Marshal.Copy(pixels, data, 0, data.Length);

            var img = new Image();
            img.CreateFromData(width, height, false, Image.Format.Rgba8, data);
            var tex = new ImageTexture();
            tex.CreateFromImage(img, (uint)Texture.FlagsEnum.Filter);//设置纹理no mimaps，不然字体会模糊


            texture = tex;
        }

        public override void _Input(InputEvent @event)
        {
            var io = ImGui.GetIO();

            if (@event is InputEventMouseButton)
            {
                var evt = @event as InputEventMouseButton;
                if (evt.Pressed)
                {
                    switch ((ButtonList)evt.ButtonIndex)
                    {
                        case ButtonList.WheelUp:
                            io.MouseWheel += 1;
                            break;
                        case ButtonList.WheelDown:
                            io.MouseWheel -= 1;
                            break;
                        case ButtonList.WheelLeft:
                            io.MouseWheelH -= 1;
                            break;
                        case ButtonList.WheelRight:
                            io.MouseWheelH += 1;
                            break;
                    }
                }

                io.MouseDown[0] = Input.IsMouseButtonPressed((int)ButtonList.Left);
                io.MouseDown[1] = Input.IsMouseButtonPressed((int)ButtonList.Right);
                io.MouseDown[2] = Input.IsMouseButtonPressed((int)ButtonList.Middle);

                //Debug.Log($"{evt.AsText()}");
            }

            else if (@event is InputEventMouseMotion)
            {
                var evt = @event as InputEventMouseMotion;
                io.MousePos = new System.Numerics.Vector2(evt.GlobalPosition.x, evt.GlobalPosition.y);

                //Debug.Log($"{evt.AsText()}");
            }
            else if (@event is InputEventKey)
            {
                var evt = @event as InputEventKey;
                if (evt.Pressed)
                    io.AddInputCharacter(evt.Unicode);

                io.KeysDown[(int)ImGuiKey.Backspace] = Input.IsKeyPressed((int)KeyList.Backspace);//删除字符
                io.KeysDown[(int)ImGuiKey.Space] = Input.IsKeyPressed((int)KeyList.Space);
                io.KeysDown[(int)ImGuiKey.Enter] = Input.IsKeyPressed((int)KeyList.Enter);
                io.KeysDown[(int)ImGuiKey.Escape] = Input.IsKeyPressed((int)KeyList.Escape);
            }
        }

        public override void _Process(float delta)
        {
            var io = ImGui.GetIO();
            io.DeltaTime = delta > 0 ? delta : 1f / Engine.TargetFps;
            //Debug.Log($"_Process {delta} {io.DeltaTime}");// 单位为秒

            var rs = GetParentAreaSize();
            if (rs.x != io.DisplaySize.X || rs.y != io.DisplaySize.Y)
            {
                Debug.Log($"RectSize {io.DisplaySize} => {rs}");
                io.DisplaySize.X = rs.x;
                io.DisplaySize.Y = rs.y;
            }

            ImGui.NewFrame();
            imguiDraw();
            ImGui.EndFrame();
            ImGui.Render();

            Update();
        }

        public override unsafe void _Draw()
        {
            var draw_data = ImGui.GetDrawData();
            if ((IntPtr)draw_data.NativePtr == IntPtr.Zero)
                return;

            int fb_width = (int)(draw_data.DisplaySize.X * draw_data.FramebufferScale.X);
            int fb_height = (int)(draw_data.DisplaySize.Y * draw_data.FramebufferScale.Y);
            if (fb_width <= 0 || fb_height <= 0)
                return;

            var clip_off = draw_data.DisplayPos;
            var clip_scale = draw_data.FramebufferScale;

            for (var n = 0; n < draw_data.CmdListsCount; ++n)
            {
                var cmd_list = draw_data.CmdListsRange[n];

                var vtx_size = cmd_list.VtxBuffer.Size;
                var idx_size = cmd_list.IdxBuffer.Size;
                //Debug.Log($"cmd_list n:{n} vtx_size:{vtx_size} idx_size:{idx_size}");

                for (var cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; ++cmd_i)
                {
                    var pcmd = cmd_list.CmdBuffer[cmd_i];


                    //Debug.Log($"cmd_list.CmdBuffer cmd_i:{cmd_i} ElemCount:{pcmd.ElemCount} VtxOffset:{pcmd.VtxOffset} IdxOffset:{pcmd.IdxOffset} TextureId:{pcmd.TextureId}");
                    if (pcmd.UserCallback != IntPtr.Zero)
                    {
                    }
                    else
                    {
                        Vector4 clip_rect;
                        clip_rect.X = (pcmd.ClipRect.X - clip_off.X) * clip_scale.X;
                        clip_rect.Y = (pcmd.ClipRect.Y - clip_off.Y) * clip_scale.Y;
                        clip_rect.Z = (pcmd.ClipRect.Z - clip_off.X) * clip_scale.X;
                        clip_rect.W = (pcmd.ClipRect.W - clip_off.Y) * clip_scale.Y;

                        if (clip_rect.X < fb_width && clip_rect.Y < fb_height && clip_rect.Z >= 0.0f && clip_rect.W >= 0.0f)
                        {
                            var texId = pcmd.TextureId;

                            //pcmd.ElemCount;
                            //pcmd.VtxOffset;
                            //pcmd.IdxOffset;

                            var pp = new GdVect2[3];
                            var cc = new GdColor[3];
                            var uv = new GdVect2[3];
                            for (var j = 0; j < pcmd.ElemCount; j += 3)
                            {
                                for (var k = 0; k < 3; ++k)
                                {
                                    var vtx = cmd_list.VtxBuffer[cmd_list.IdxBuffer[(int)pcmd.IdxOffset + j + k]];
                                    pp[k] = new GdVect2(vtx.pos.X, vtx.pos.Y);
                                    uv[k] = new GdVect2(vtx.uv.X, vtx.uv.Y);

                                    var c = ImGui.ColorConvertU32ToFloat4(vtx.col);//bgra format
                                    cc[k] = new GdColor(c.X, c.Y, c.Z, c.W);
                                }
                                DrawPrimitive(pp, cc, uv, texture);
                            }
                        }
                    }
                }
            }//end for
        }

    }
#endif
}
