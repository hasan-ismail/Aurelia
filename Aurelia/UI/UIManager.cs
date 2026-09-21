using CommunityToolkit.Mvvm.ComponentModel;
using Aurelia.Controls;
using Aurelia.Core.UI;
using Aurelia.Helpers;
using ModernWpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Aurelia.UI
{
    public class UIManager : ObservableObject
    {
        public const double FlyoutWidth = 360;

        public const double DefaultSessionControlHeight = 208;

        public const double DefaultVerticalSpacing = 8;

        public const double FlyoutShadowDepth = 32;

        public static Thickness FlyoutShadowMargin = GetFlyoutShadowMargin(FlyoutShadowDepth);

        private ElementTheme currentSystemTheme = ElementTheme.Dark;
        private ThemeResources themeResources;
        private ResourceDictionary lightResources;
        private ResourceDictionary darkResources;

        private bool _isThemeUpdated;

        #region Properties

        private bool restartRequired;

        public bool RestartRequired
        {
            get => restartRequired;
            set => SetProperty(ref restartRequired, value);
        }

        #region General

        private TopBarVisibility topBarVisibility = DefaultValuesStore.DefaultTopBarVisibility;

        public TopBarVisibility TopBarVisibility
        {
            get => topBarVisibility;
            set
            {
                if (SetProperty(ref topBarVisibility, value))
                {
                    AppDataHelper.TopBarVisibility = value;
                }
            }
        }

        private ElementTheme appTheme = DefaultValuesStore.AppTheme;

        public ElementTheme AppTheme
        {
            get => appTheme;
            set
            {
                if (SetProperty(ref appTheme, value))
                {
                    UpdateAppTheme();
                    AppDataHelper.AppTheme = value;
                }
            }
        }

        private ElementTheme flyoutTheme = DefaultValuesStore.FlyoutTheme;

        public ElementTheme FlyoutTheme
        {
            get => flyoutTheme;
            set
            {
                if (SetProperty(ref flyoutTheme, value))
                {
                    UpdateTheme();
                    AppDataHelper.FlyoutTheme = value;
                }
            }
        }

        private ElementTheme actualFlyoutTheme = ElementTheme.Dark;

        public ElementTheme ActualFlyoutTheme
        {
            get => actualFlyoutTheme;
            private set => SetProperty(ref actualFlyoutTheme, value);
        }

        private int flyoutTimeout = DefaultValuesStore.FlyoutTimeout;

        public int FlyoutTimeout
        {
            get => flyoutTimeout;
            set
            {
                if (SetProperty(ref flyoutTimeout, value))
                {
                    AppDataHelper.FlyoutTimeout = flyoutTimeout;
                }
            }
        }

        private double flyoutBackgroundOpacity = DefaultValuesStore.FlyoutBackgroundOpacity;

        public double FlyoutBackgroundOpacity
        {
            get => flyoutBackgroundOpacity;
            set
            {
                if (SetProperty(ref flyoutBackgroundOpacity, value))
                {
                    OnFlyoutBackgroundOpacityChanged();
                }
            }
        }

        private bool flyoutGlassEffectEnabled = DefaultValuesStore.FlyoutGlassEffectEnabled;

        /// <summary>
        /// Whether the flyout is drawn as translucent glass over a blurred desktop, rather than
        /// as a plain opaque surface.
        /// </summary>
        public bool FlyoutGlassEffectEnabled
        {
            get => flyoutGlassEffectEnabled;
            set
            {
                if (SetProperty(ref flyoutGlassEffectEnabled, value))
                {
                    OnFlyoutGlassEffectEnabledChanged();
                }
            }
        }

        /// <summary>
        /// The glass surface is rendered by WPF, so it works on every supported Windows version.
        /// </summary>
        public bool IsGlassEffectSupported => true;

        /// <summary>The built-in looks offered in settings.</summary>
        public IReadOnlyList<FlyoutThemePreset> ThemePresets => FlyoutThemePreset.All;

        private string flyoutThemePreset = DefaultValuesStore.FlyoutThemePreset;

        /// <summary>
        /// Name of the preset currently in effect, or empty once any individual setting has been
        /// changed by hand.
        /// </summary>
        public string FlyoutThemePresetName
        {
            get => flyoutThemePreset;
            private set
            {
                if (SetProperty(ref flyoutThemePreset, value))
                {
                    AppDataHelper.FlyoutThemePreset = value;
                }
            }
        }

        private bool flyoutJellyfishEnabled = DefaultValuesStore.FlyoutJellyfishEnabled;

        /// <summary>Whether jellyfish drift behind the flyout's content.</summary>
        public bool FlyoutJellyfishEnabled
        {
            get => flyoutJellyfishEnabled;
            set
            {
                if (SetProperty(ref flyoutJellyfishEnabled, value))
                {
                    AppDataHelper.FlyoutJellyfishEnabled = value;
                    MarkThemeCustomised();
                }
            }
        }

        private double flyoutCornerRadius = DefaultValuesStore.FlyoutCornerRadius;

        /// <summary>Corner rounding of the flyout cards.</summary>
        public double FlyoutCornerRadius
        {
            get => flyoutCornerRadius;
            set
            {
                if (SetProperty(ref flyoutCornerRadius, value))
                {
                    AppDataHelper.FlyoutCornerRadius = value;
                    UpdateCornerRadius();
                    MarkThemeCustomised();
                }
            }
        }

        private bool isApplyingPreset;

        /// <summary>
        /// Clears the preset name when a value it owns is changed directly, so the settings page
        /// stops claiming a preset that is no longer accurate.
        /// </summary>
        private void MarkThemeCustomised()
        {
            if (!isApplyingPreset)
            {
                FlyoutThemePresetName = string.Empty;
            }
        }

        private void UpdateCornerRadius()
        {
            if (Application.Current?.Resources is ResourceDictionary resources)
            {
                resources["FlyoutCornerRadius"] = new CornerRadius(flyoutCornerRadius);
            }
        }

        /// <summary>Applies a preset by writing its values into the individual settings.</summary>
        public void ApplyThemePreset(FlyoutThemePreset preset)
        {
            if (preset == null)
            {
                return;
            }

            isApplyingPreset = true;

            try
            {
                UseSystemAccentColor = false;
                FlyoutAccentColor = preset.Accent;
                FlyoutBackgroundOpacity = preset.BackgroundOpacity;
                FlyoutCornerRadius = preset.CornerRadius;
                FlyoutTheme = preset.Theme;
                FlyoutJellyfishEnabled = preset.Jellyfish;

            }
            finally
            {
                isApplyingPreset = false;
            }

            FlyoutThemePresetName = preset.Name;
        }

        private bool useSystemAccentColor = DefaultValuesStore.UseSystemAccentColor;

        /// <summary>
        /// Follow the Windows accent colour. Turning this off hands control of the accent - the
        /// slider fills, toggles and highlights - to <see cref="FlyoutAccentColor"/>.
        /// </summary>
        public bool UseSystemAccentColor
        {
            get => useSystemAccentColor;
            set
            {
                if (SetProperty(ref useSystemAccentColor, value))
                {
                    AppDataHelper.UseSystemAccentColor = value;
                    ApplyAccentColor();
                    MarkThemeCustomised();
                }
            }
        }

        private Color flyoutAccentColor = ParseColor(DefaultValuesStore.FlyoutAccentColor, Colors.White);

        /// <summary>Tint of the glass sheen, rim light and the cursor highlight.</summary>
        public Color FlyoutAccentColor
        {
            get => flyoutAccentColor;
            set
            {
                if (SetProperty(ref flyoutAccentColor, value))
                {
                    AppDataHelper.FlyoutAccentColor = value.ToString();
                    ApplyAccentColor();
                    MarkThemeCustomised();
                }
            }
        }


        private bool trayIconEnabled = DefaultValuesStore.TrayIconEnabled;

        public bool TrayIconEnabled
        {
            get => trayIconEnabled;
            set
            {
                if (SetProperty(ref trayIconEnabled, value))
                {
                    OnTrayIconEnabledChanged();
                }
            }
        }

        private bool useColoredTrayIcon = DefaultValuesStore.UseColoredTrayIcon;

        public bool UseColoredTrayIcon
        {
            get => useColoredTrayIcon;
            set
            {
                if (SetProperty(ref useColoredTrayIcon, value))
                {
                    OnUseColoredTrayIconChanged();
                }
            }
        }

        private bool flyoutAnimationEnabled = DefaultValuesStore.FlyoutAnimationEnabled;

        public bool FlyoutAnimationEnabled
        {
            get => flyoutAnimationEnabled;
            set
            {
                if (SetProperty(ref flyoutAnimationEnabled, value))
                {
                    OnFadeAnimationEnabledChanged();
                }
            }
        }

        #endregion

        #region Layout

        private FlyoutWindowPlacementMode onScreenFlyoutWindowPlacementMode;

        public FlyoutWindowPlacementMode OnScreenFlyoutWindowPlacementMode
        {
            get => onScreenFlyoutWindowPlacementMode;
            set
            {
                if (SetProperty(ref onScreenFlyoutWindowPlacementMode, value))
                {
                    AppDataHelper.OnScreenFlyoutWindowPlacementMode = value;
                }
            }
        }

        private FlyoutWindowAlignments onScreenFlyoutWindowAlignment;

        public FlyoutWindowAlignments OnScreenFlyoutWindowAlignment
        {
            get => onScreenFlyoutWindowAlignment;
            set
            {
                if (SetProperty(ref onScreenFlyoutWindowAlignment, value))
                {
                    AppDataHelper.OnScreenFlyoutWindowAlignment = value;
                }
            }
        }

        private Thickness onScreenFlyoutWindowMargin;

        public Thickness OnScreenFlyoutWindowMargin
        {
            get => onScreenFlyoutWindowMargin;
            set
            {
                if (SetProperty(ref onScreenFlyoutWindowMargin, value))
                {
                    AppDataHelper.OnScreenFlyoutWindowMargin = value;
                }
            }
        }

        private FlyoutWindowExpandDirection onScreenFlyoutWindowExpandDirection;

        public FlyoutWindowExpandDirection OnScreenFlyoutWindowExpandDirection
        {
            get => onScreenFlyoutWindowExpandDirection;
            set
            {
                if (SetProperty(ref onScreenFlyoutWindowExpandDirection, value))
                {
                    AppDataHelper.OnScreenFlyoutWindowExpandDirection = value;
                }
            }
        }

        private StackingDirection onScreenFlyoutContentStackingDirection;

        public StackingDirection OnScreenFlyoutContentStackingDirection
        {
            get => onScreenFlyoutContentStackingDirection;
            set
            {
                if (SetProperty(ref onScreenFlyoutContentStackingDirection, value))
                {
                    AppDataHelper.OnScreenFlyoutContentStackingDirection = value;
                }
            }
        }

        #endregion

        #region Media Controls

        private bool alignGSMTCThumbnailToRight = DefaultValuesStore.AlignGSMTCThumbnailToRight;

        public bool AlignGSMTCThumbnailToRight
        {
            get => alignGSMTCThumbnailToRight;
            set
            {
                if (SetProperty(ref alignGSMTCThumbnailToRight, value))
                {
                    AppDataHelper.AlignGSMTCThumbnailToRight = value;
                }
            }
        }

        private bool useGSMTCThumbnailAsBackground = DefaultValuesStore.UseGSMTCThumbnailAsBackground;

        public bool UseGSMTCThumbnailAsBackground
        {
            get => useGSMTCThumbnailAsBackground;
            set
            {
                if (SetProperty(ref useGSMTCThumbnailAsBackground, value))
                {
                    AppDataHelper.UseGSMTCThumbnailAsBackground = value;
                }
            }
        }

        private Orientation sessionsPanelOrientation = DefaultValuesStore.SessionsPanelOrientation;

        public Orientation SessionsPanelOrientation
        {
            get => sessionsPanelOrientation;
            set
            {
                if (SetProperty(ref sessionsPanelOrientation, value))
                {
                    OnSessionsPanelOrientation();
                }
            }
        }

        private int maxVerticalSessionControlsCount = DefaultValuesStore.MaxVerticalSessionControlsCount;

        public int MaxVerticalSessionControlsCount
        {
            get => maxVerticalSessionControlsCount;
            set
            {
                if (SetProperty(ref maxVerticalSessionControlsCount, value))
                {
                    OnMaxVerticalSessionControlsCount();
                }
            }
        }

        private double calculatedSessionsPanelMaxHeight = DefaultSessionControlHeight;

        public double CalculatedSessionsPanelMaxHeight
        {
            get => calculatedSessionsPanelMaxHeight;
            private set => SetProperty(ref calculatedSessionsPanelMaxHeight, value);
        }

        private double calculatedSessionsPanelSpacing;

        public double CalculatedSessionsPanelSpacing
        {
            get => calculatedSessionsPanelSpacing;
            private set => SetProperty(ref calculatedSessionsPanelSpacing, value);
        }

        #endregion

        #endregion

        public void Initialize()
        {
            OnScreenFlyoutWindowPlacementMode = AppDataHelper.OnScreenFlyoutWindowPlacementMode;
            OnScreenFlyoutWindowAlignment = AppDataHelper.OnScreenFlyoutWindowAlignment;
            OnScreenFlyoutWindowMargin = AppDataHelper.OnScreenFlyoutWindowMargin;
            OnScreenFlyoutWindowExpandDirection = AppDataHelper.OnScreenFlyoutWindowExpandDirection;
            OnScreenFlyoutContentStackingDirection = AppDataHelper.OnScreenFlyoutContentStackingDirection;

            TopBarVisibility = AppDataHelper.TopBarVisibility;
            FlyoutTimeout = AppDataHelper.FlyoutTimeout;
            AlignGSMTCThumbnailToRight = AppDataHelper.AlignGSMTCThumbnailToRight;
            UseGSMTCThumbnailAsBackground = AppDataHelper.UseGSMTCThumbnailAsBackground;
            MaxVerticalSessionControlsCount = AppDataHelper.MaxVerticalSessionControlsCount;
            SessionsPanelOrientation = AppDataHelper.SessionsPanelOrientation;

            themeResources = (ThemeResources)Application.Current.Resources
                .MergedDictionaries.FirstOrDefault(x => x is ThemeResources);
            lightResources = themeResources.ThemeDictionaries["Light"];
            darkResources = themeResources.ThemeDictionaries["Dark"];

            FlyoutGlassEffectEnabled = AppDataHelper.FlyoutGlassEffectEnabled;
            flyoutThemePreset = AppDataHelper.FlyoutThemePreset;
            flyoutCornerRadius = AppDataHelper.FlyoutCornerRadius;
            flyoutJellyfishEnabled = AppDataHelper.FlyoutJellyfishEnabled;
            OnPropertyChanged(nameof(FlyoutJellyfishEnabled));
            OnPropertyChanged(nameof(FlyoutCornerRadius));
            UpdateCornerRadius();

            useSystemAccentColor = AppDataHelper.UseSystemAccentColor;
            OnPropertyChanged(nameof(UseSystemAccentColor));

            // Seed the custom colour from whatever Windows is using, so switching to manual starts
            // from the current look instead of jumping to some unrelated default.
            flyoutAccentColor = ParseColor(AppDataHelper.FlyoutAccentColor, GetSystemAccentColor());
            OnPropertyChanged(nameof(FlyoutAccentColor));

            ApplyAccentColor();
            FlyoutBackgroundOpacity = AppDataHelper.FlyoutBackgroundOpacity;

            TrayIconManager.SetupTrayIcon();

            TrayIconEnabled = AppDataHelper.TrayIconEnabled;
            UseColoredTrayIcon = AppDataHelper.UseColoredTrayIcon;
            FlyoutAnimationEnabled = AppDataHelper.FlyoutAnimationEnabled;

            FlyoutTheme = AppDataHelper.FlyoutTheme;
            AppTheme = AppDataHelper.AppTheme;

            SystemTheme.SystemThemeChanged += OnSystemThemeChanged;
            SystemTheme.Initialize();
        }

        private void OnFlyoutBackgroundOpacityChanged()
        {
            UpdateFlyoutBackgroundOpacity();
            AppDataHelper.FlyoutBackgroundOpacity = flyoutBackgroundOpacity;
        }

        private void OnTrayIconEnabledChanged()
        {
            TrayIconManager.UpdateTrayIconVisibility(trayIconEnabled);
            AppDataHelper.TrayIconEnabled = TrayIconEnabled;
        }

        private void OnUseColoredTrayIconChanged()
        {
            UpdateTrayIcon();
            AppDataHelper.UseColoredTrayIcon = useColoredTrayIcon;
        }

        private void OnFadeAnimationEnabledChanged()
        {
            AppDataHelper.FlyoutAnimationEnabled = flyoutAnimationEnabled;
        }

        private void OnSystemThemeChanged(object sender, SystemThemeChangedEventArgs args)
        {
            currentSystemTheme = args.IsSystemLightTheme ? ElementTheme.Light : ElementTheme.Dark;
            UpdateTheme();
        }

        private void UpdateAppTheme()
        {
            ThemeManager.Current.ApplicationTheme = appTheme switch
            {
                ElementTheme.Default => null,
                ElementTheme.Light => ApplicationTheme.Light,
                ElementTheme.Dark => ApplicationTheme.Dark,
                _ => null,
            };
        }

        private void UpdateTheme()
        {
            ActualFlyoutTheme = flyoutTheme == ElementTheme.Default ? currentSystemTheme : flyoutTheme;

            if (!_isThemeUpdated)
            {
                _isThemeUpdated = true;
            }

            UpdateFlyoutBackgroundOpacity();
            UpdateTrayIcon();

            // ModernWpf recomputes its accent brushes whenever the theme changes, which puts the
            // Windows accent back and silently undoes a custom one. Re-assert it here so the
            // user's choice survives light/dark switches and system preference changes.
            ApplyAccentColor();
        }

        private void UpdateFlyoutBackgroundOpacity()
        {
            if (!_isThemeUpdated) return;

            var themeResource = actualFlyoutTheme == ElementTheme.Light ? lightResources : darkResources;
            var brush = themeResource["FlyoutBackground"] as Brush;
            brush = brush.Clone();

            // With glass on, the surface has to let the blurred backdrop through, so the user's
            // opacity is scaled down rather than replaced - someone who picked 60% still gets a
            // thinner surface than someone who picked 100%.
            double opacity = flyoutBackgroundOpacity * 0.01;

            if (IsGlassEffectActive)
            {
                opacity *= DefaultValuesStore.FlyoutGlassOpacityFactor;
            }

            brush.Opacity = opacity;
            themeResource["FlyoutBackground"] = brush;
        }

        /// <summary>Glass is drawn purely in XAML, so there is nothing extra to feature-detect.</summary>
        private bool IsGlassEffectActive => flyoutGlassEffectEnabled;

        private void OnFlyoutGlassEffectEnabledChanged()
        {
            AppDataHelper.FlyoutGlassEffectEnabled = flyoutGlassEffectEnabled;

            UpdateFlyoutBackgroundOpacity();
        }

        private static Color GetSystemAccentColor()
        {
            if (Application.Current?.Resources["SystemAccentColor"] is Color accent)
            {
                return accent;
            }

            return Color.FromRgb(0x4C, 0xC2, 0xFF);
        }

        private static Color ParseColor(string value, Color fallback)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(value)
                    && ColorConverter.ConvertFromString(value) is Color parsed)
                {
                    return parsed;
                }
            }
            catch
            {
                // A hand-edited settings file shouldn't stop the app starting.
            }

            return fallback;
        }

        /// <summary>
        /// Pushes the chosen accent into ModernWpf, which is what actually colours the slider
        /// fills, toggles and highlights. Setting <c>ThemeManager.AccentColor</c> to null hands
        /// control back to Windows.
        /// </summary>
        /// <remarks>
        /// Tinting the glass alone was not enough: everything the eye reads as "the accent" comes
        /// from ModernWpf's SystemAccentColor family, so a custom colour appeared to do nothing.
        /// </remarks>
        private void ApplyAccentColor()
        {
            ThemeManager.Current.AccentColor = useSystemAccentColor ? null : flyoutAccentColor;

            UpdateGlassBrushes();
        }

        /// <summary>
        /// Rebuilds the sheen and rim brushes. They are app-level resources referenced with
        /// DynamicResource, so replacing them repaints every card.
        /// </summary>
        private void UpdateGlassBrushes()
        {
            var resources = Application.Current?.Resources;

            if (resources == null)
            {
                return;
            }

            // The sheen and rim stay white: they are specular highlights - reflected light - and
            // tinting them with the accent makes the glass look stained rather than lit. The
            // accent shows up in the controls and in the cursor highlight instead.
            Color a = Colors.White;

            Color Tint(byte alpha) => Color.FromArgb(alpha, a.R, a.G, a.B);

            var sheen = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0.35, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Tint(0x26), 0.0),
                    new GradientStop(Tint(0x0D), 0.35),
                    new GradientStop(Tint(0x00), 0.75)
                }
            };
            sheen.Freeze();

            var rim = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Tint(0x40), 0.0),
                    new GradientStop(Tint(0x14), 0.5),
                    new GradientStop(Tint(0x0A), 1.0)
                }
            };
            rim.Freeze();

            resources["FlyoutGlassSheenBrush"] = sheen;
            resources["FlyoutGlassRimBrush"] = rim;
        }

        // Nothing is applied to the flyout window itself, and two compositor effects were tried
        // and removed:
        //
        //   * Acrylic blur (accent policy) applies to the whole window rectangle. That rectangle
        //     is much larger than the cards you see - the surplus is the transparent margin the
        //     drop shadow needs - so it painted a dark blurred halo around the flyout.
        //
        //   * DWMWA_WINDOW_CORNER_PREFERENCE also makes DWM draw the window's frame border. On a
        //     window that is meant to be invisible apart from its cards, that border showed up as
        //     a rectangle outlining the entire window, margin included. The cards already round
        //     themselves via FlyoutCornerRadius, so it gained nothing either.
        //
        // The glass is rendered entirely in XAML, which clips correctly to each card.

        private void UpdateTrayIcon()
        {
            if (!_isThemeUpdated) return;

            TrayIconManager.UpdateTrayIconInternal(currentSystemTheme, useColoredTrayIcon);
        }

        private void OnMaxVerticalSessionControlsCount()
        {
            UpdateCalculatedSessionsPanelMaxHeight();
            AppDataHelper.MaxVerticalSessionControlsCount = maxVerticalSessionControlsCount;
        }

        private void OnSessionsPanelOrientation()
        {
            UpdateCalculatedSessionsPanelMaxHeight();
            AppDataHelper.SessionsPanelOrientation = sessionsPanelOrientation;
        }

        private void UpdateCalculatedSessionsPanelMaxHeight()
        {
            if (sessionsPanelOrientation == Orientation.Vertical)
            {
                var n = maxVerticalSessionControlsCount;
                CalculatedSessionsPanelMaxHeight = (DefaultSessionControlHeight * n) + (DefaultVerticalSpacing * (n - 1));
                CalculatedSessionsPanelSpacing = DefaultVerticalSpacing;
            }
            else
            {
                CalculatedSessionsPanelMaxHeight = DefaultSessionControlHeight;
                CalculatedSessionsPanelSpacing = 0;
            }
        }

        internal static Thickness GetFlyoutShadowMargin(double depth)
        {
            double radius = 0.9 * depth;
            double offset = 0.4 * depth;

            return new Thickness(
                radius,
                radius,
                radius,
                radius + offset);
        }
    }

    public enum TopBarVisibility
    {
        Visible = 0,
        AutoHide = 1,
        Collapsed = 2
    }
}
