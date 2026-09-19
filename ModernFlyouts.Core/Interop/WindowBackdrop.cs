using System;
using System.Runtime.InteropServices;
using ModernFlyouts.Core.Helpers;

namespace ModernFlyouts.Core.Interop
{
    /// <summary>
    /// The kind of material drawn behind a flyout.
    /// </summary>
    public enum BackdropMaterial
    {
        /// <summary>A plain opaque surface - the classic look.</summary>
        Solid,

        /// <summary>Translucent, with the desktop behind it blurred by the compositor.</summary>
        Acrylic
    }

    /// <summary>
    /// Applies compositor-level effects - a blurred backdrop and rounded corners - to a window.
    /// </summary>
    /// <remarks>
    /// The flyout windows are created with <c>WS_EX_NOREDIRECTIONBITMAP</c> and per-pixel alpha, so
    /// they have no redirection surface. That rules out <c>DWMWA_SYSTEMBACKDROP_TYPE</c> (the
    /// documented Mica/Acrylic switch), which needs one. What does work on these windows is the
    /// older accent policy, so that is what is used here.
    ///
    /// Both calls are best-effort. <c>SetWindowCompositionAttribute</c> is undocumented and the
    /// corner preference only exists on Windows 11, so every failure is treated as "this machine
    /// doesn't support it" and the flyout simply keeps whatever the XAML painted. Nothing here is
    /// load-bearing for the flyout actually appearing.
    /// </remarks>
    public static class WindowBackdrop
    {
        private enum AccentState
        {
            Disabled = 0,
            EnableBlurBehind = 3,
            EnableAcrylicBlurBehind = 4
        }

        [Flags]
        private enum AccentFlags
        {
            None = 0,
            GradientColour = 0x02
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public AccentFlags AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private const int WCA_ACCENT_POLICY = 19;

        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

        private const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hWnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);

        /// <summary>
        /// Acrylic needs the compositor's blur, which only exists from Windows 10 1803 onwards.
        /// </summary>
        public static bool IsAcrylicSupported => OSVersionHelper.Build >= 17134;

        /// <summary>
        /// Applies <paramref name="material"/> to the window.
        /// </summary>
        /// <param name="hWnd">The window to affect.</param>
        /// <param name="material">The material to draw behind the window.</param>
        /// <param name="tint">
        /// The tint laid over the blur, as 0xAARRGGBB. The alpha controls how much of the backdrop
        /// shows through: too low and text stops being readable over a busy wallpaper.
        /// </param>
        public static void Apply(IntPtr hWnd, BackdropMaterial material, uint tint)
        {
            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            AccentState state = material == BackdropMaterial.Acrylic && IsAcrylicSupported
                ? AccentState.EnableAcrylicBlurBehind
                : AccentState.Disabled;

            var policy = new AccentPolicy
            {
                AccentState = state,
                AccentFlags = AccentFlags.GradientColour,
                // The accent policy wants 0xAABBGGRR, which is the reverse byte order of the
                // 0xAARRGGBB callers naturally write.
                GradientColor = SwapRedAndBlue(tint),
                AnimationId = 0
            };

            int size = Marshal.SizeOf<AccentPolicy>();
            IntPtr buffer = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(policy, buffer, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WCA_ACCENT_POLICY,
                    Data = buffer,
                    SizeOfData = size
                };

                SetWindowCompositionAttribute(hWnd, ref data);
            }
            catch (EntryPointNotFoundException)
            {
                // Not available on this build; the flyout stays as the XAML painted it.
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Asks DWM to round the window's corners. No-op before Windows 11.
        /// </summary>
        public static void ApplyRoundedCorners(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !OSVersionHelper.IsWindows11OrGreater)
            {
                return;
            }

            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private static uint SwapRedAndBlue(uint argb)
        {
            uint a = argb & 0xFF000000;
            uint r = (argb & 0x00FF0000) >> 16;
            uint g = argb & 0x0000FF00;
            uint b = (argb & 0x000000FF) << 16;

            return a | b | g | r;
        }
    }
}
