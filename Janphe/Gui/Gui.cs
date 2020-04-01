using System;
using ImGuiNET;

namespace Janphe
{
    partial class Gui
    {
        public string _(string s) => Tr(s);

        private Action<Gui> _callback { get; set; }

        private void initImGui()
        {
            Debug.Log($"ImGui version:{ImGui.GetVersion()}");
            ImGui.CreateContext();
            ImGui.StyleColorsClassic();


            //! 必须设置 显示区域大小，字体贴图集，不然程序会挂掉
            var io = ImGui.GetIO();
            //io.Fonts.AddFontDefault();
            io.Fonts.AddFontFromFileTTF("E:\\room.godot\\Fantasy Map\\fonts\\文泉驿等宽微米黑.ttf", 16, null, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());
            //io.Fonts.AddFontFromFileTTF("E:\\room.godot\\Fantasy Map\\fonts\\文泉驿等宽微米黑.ttf", 14, null, io.Fonts.GetGlyphRangesChineseFull());
            io.Fonts.Build();

            initKeyMaps();//映射ImGui键名
        }

        private void initKeyMaps()
        {
            var io = ImGui.GetIO();
            for (var i = 0; i < io.KeyMap.Count; ++i)
                io.KeyMap[i] = i;
        }

        private void imguiDraw()
        {
            _callback?.Invoke(this);
        }

        public void OnGui(Action<Gui> callback)
        {
            _callback = callback;
        }
    }
}
