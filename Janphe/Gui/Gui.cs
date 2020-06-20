using System;
using ImGuiNET;

namespace Janphe
{
    partial class Gui
    {
        // Extend ImGuiTabItemFlags_
        internal enum ImGuiTabItemFlagsPrivate_
        {
            NoCloseButton = 1 << 20   // Track whether p_open was set or not (we'll need this info on the next frame to recompute ContentWidth during layout)
        };
        public ImGuiTabItemFlags tabFlags(bool select)
        {
            var ret = (ImGuiTabItemFlags)ImGuiTabItemFlagsPrivate_.NoCloseButton;
            if (select)
                ret |= ImGuiTabItemFlags.SetSelected;
            return ret;
        }

        public string _(string s) => Tr(s);

        private Action<Gui> _callback { get; set; }
        private IntPtr _context { get; set; }

        private void initImGui()
        {
            Debug.Log($"ImGui version:{ImGui.GetVersion()}");
            _context = ImGui.CreateContext();
            ImGui.StyleColorsClassic();


            //! 必须设置 显示区域大小，字体贴图集，不然程序会挂掉
            var io = ImGui.GetIO();
            //io.Fonts.AddFontDefault();
            //io.Fonts.AddFontFromFileTTF(path, 16, null, io.Fonts.GetGlyphRangesChineseSimplifiedCommon());

            var path = "fonts/文泉驿等宽微米黑.ttf";
            App.LoadRes(path, ptr =>
            {
                io.Fonts.AddFontFromMemoryTTF(ptr, 14, 14, null, io.Fonts.GetGlyphRangesChineseFull());
                io.Fonts.Build();
            });

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

        private void releaseImGui()
        {
            ImGui.DestroyContext(_context);
            _context = IntPtr.Zero;
        }
    }
}
