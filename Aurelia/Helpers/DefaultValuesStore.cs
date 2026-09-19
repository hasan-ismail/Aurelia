using Aurelia.Controls;
using Aurelia.Core.UI;
using Aurelia.UI;
using Aurelia.UI.Media;
using ModernWpf;
using System.Windows;
using System.Windows.Controls;

namespace Aurelia.Helpers
{
    internal class DefaultValuesStore
    {
        #region General

        public const bool AudioModuleEnabled = true;

        public const bool BrightnessModuleEnabled = true;

        public const bool AirplaneModeModuleEnabled = true;

        public const bool LockKeysModuleEnabled = true;

        public const DefaultFlyout PreferredDefaultFlyout = DefaultFlyout.Aurelia;

        public static BindablePoint DefaultFlyoutPosition => new(50, 60);

        #endregion

        #region Layout

        public const FlyoutWindowPlacementMode OnScreenFlyoutWindowPlacementMode = FlyoutWindowPlacementMode.Auto;

        public const FlyoutWindowAlignments OnScreenFlyoutWindowAlignment = FlyoutWindowAlignments.Top | FlyoutWindowAlignments.Left;

        public static Thickness OnScreenFlyoutWindowMargin = new(10);

        public const FlyoutWindowExpandDirection OnScreenFlyoutWindowExpandDirection = FlyoutWindowExpandDirection.Auto;

        public const StackingDirection OnScreenFlyoutContentStackingDirection = StackingDirection.Ascending;

        #endregion

        #region Module specific

        #region Audio module related

        public const bool ShowGSMTCInVolumeFlyout = true;

        public const bool ShowVolumeControlInGSMTCFlyout = true;

        #endregion

        #region Lock keys module related

        public const bool LockKeysModule_CapsLockEnabled = true;

        public const bool LockKeysModule_NumLockEnabled = true;

        public const bool LockKeysModule_ScrollLockEnabled = true;

        public const bool LockKeysModule_InsertEnabled = true;

        #endregion

        #endregion

        #region UI

        /// <summary>
        /// Collapsed by default: the top bar is a settings/pin/close strip, and the flyout reads as
        /// a cleaner object without it. It is still one setting away for anyone who wants it.
        /// </summary>
        public const TopBarVisibility DefaultTopBarVisibility = TopBarVisibility.Collapsed;

        public const ElementTheme AppTheme = ElementTheme.Default;

        public const ElementTheme FlyoutTheme = ElementTheme.Default;

        public const int FlyoutTimeout = 2750;

        public static string RecommendedFlyoutTimeout = FlyoutTimeout.ToString();

        public const double FlyoutBackgroundOpacity = 100.0;

        /// <summary>
        /// Glass is on by default where the compositor can blur (Windows 10 1803+); the flyout
        /// falls back to a solid surface automatically everywhere else.
        /// </summary>
        public const bool FlyoutGlassEffectEnabled = true;

        /// <summary>
        /// Tint of the glass sheen, rim light and the cursor highlight. White keeps the surface
        /// neutral; anything else colours the light catching the glass.
        /// </summary>
        /// <summary>Follow the Windows accent colour unless the user picks their own.</summary>
        public const bool UseSystemAccentColor = true;

        /// <summary>Corner rounding of the flyout cards, in device-independent pixels.</summary>
        public const double FlyoutCornerRadius = 8.0;

        /// <summary>Name of the last applied theme preset, or empty once anything is hand-tuned.</summary>
        public const string FlyoutThemePreset = "";

        /// <summary>
        /// The accent used when <see cref="UseSystemAccentColor"/> is off. Only a starting point -
        /// the real default is read from Windows on first run.
        /// </summary>
        public const string FlyoutAccentColor = "#FF4CC2FF";



        /// <summary>
        /// Link every display's brightness by default. On a dual-screen laptop the two panels are
        /// effectively one surface, so moving them together is what you want; it can be unlinked
        /// from the brightness flyout.
        /// </summary>
        public const bool BrightnessSyncEnabled = true;

        /// <summary>
        /// How opaque the flyout stays when glass is on. Below roughly 0.6 the text starts to
        /// struggle against a busy wallpaper.
        /// </summary>
        public const double FlyoutGlassOpacityFactor = 0.80;

        public const bool TrayIconEnabled = true;

        public const bool UseColoredTrayIcon = true;

        public const bool FlyoutAnimationEnabled = true;

        public const bool AlignGSMTCThumbnailToRight = true;

        public const bool UseGSMTCThumbnailAsBackground = true;

        public const int MaxVerticalSessionControlsCount = 1;

        public const Orientation SessionsPanelOrientation = Orientation.Horizontal;

        #endregion
    }
}
