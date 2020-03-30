using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Godot;
using ImGuiNET;
using Janphe;
using Color = Godot.Color;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

public class Panel : Godot.Panel
{
    private bool needUpdate = true;
    private Texture texture;

    public override void _Ready()
    {
        Debug.Log($"ImGui version:{ImGui.GetVersion()}");
        ImGui.CreateContext();
        ImGui.StyleColorsDark();

        //! 必须设置 显示区域大小，字体贴图集，不然程序会挂掉
        var io = ImGui.GetIO();
        io.DisplaySize.X = 1024;
        io.DisplaySize.Y = 600;
        //io.Fonts.AddFontFromFileTTF("E:\\room.godot\\Fantasy Map\\fonts\\文泉驿等宽微米黑.ttf", 13f);
        io.Fonts.AddFontDefault();
        io.Fonts.Build();

        IntPtr pixels;
        int width, height;
        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);
        Debug.Log($"GetTexDataAsRGBA32 w:{width} h:{height}");

        var data = new byte[width * height * 4];
        Marshal.Copy(pixels, data, 0, data.Length);

        var img = new Image();
        img.CreateFromData(width, height, false, Image.Format.Rgba8, data);
        var tex = new ImageTexture();
        tex.CreateFromImage(img);

        texture = tex;
    }
    public override void _Input(InputEvent @event)
    {
        var io = ImGui.GetIO();

        if (@event is InputEventMouseButton)
        {
            var evt = @event as InputEventMouseButton;
            if (evt.ButtonIndex < io.MouseDown.Count)
                io.MouseDown[evt.ButtonIndex] = evt.Pressed;
            needUpdate = true;
        }

        if (@event is InputEventMouseMotion)
        {
            var evt = @event as InputEventMouseMotion;
            io.MousePos = new System.Numerics.Vector2(evt.GlobalPosition.x, evt.GlobalPosition.y);
            needUpdate = true;
        }
    }

    public override void _Process(float delta)
    {
        var io = ImGui.GetIO();
        //io.DeltaTime = delta;

        ImGui.NewFrame();
        ImGui.ShowDemoWindow();
        ImGui.EndFrame();

        if (needUpdate)
        {
            needUpdate = false;
            ImGui.Render();
            Update();
        }
    }

    unsafe public override void _Draw()
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
                //if (pcmd.UserCallback != IntPtr.Zero)
                //{
                //}
                //else
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

                        for (var i = 0; i < pcmd.ElemCount; i++)
                        {
                        }
                    }
                }
            }

            DrawTexture(texture, Vector2.Zero);

            var pp = new Vector2[3];
            var cc = new Color[3];
            var uv = new Vector2[3];
            for (var i = 0; i < idx_size; i += 3)
            {
                for (var j = 0; j < 3; ++j)
                {
                    var vtx = cmd_list.VtxBuffer[cmd_list.IdxBuffer[i + j]];
                    pp[j] = new Vector2(vtx.pos.X, vtx.pos.Y);
                    cc[j] = new Color(vtx.col);
                    uv[j] = new Vector2(vtx.uv.X, vtx.uv.Y);
                }
                DrawPrimitive(pp, cc, uv, texture);
            }
        }
    }

    private void _on_Button_button_down()
    {
        var pp = GetNode<Label>("Label");
        pp.Text = "范文芳发射点发顺丰";
    }

}
