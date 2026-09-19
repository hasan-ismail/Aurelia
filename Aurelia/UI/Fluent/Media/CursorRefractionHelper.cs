using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Aurelia.UI.Fluent.Media
{
    /// <summary>
    /// Gives a flyout card a soft highlight that follows the mouse across it, the way light catches
    /// a piece of glass as you move past it.
    /// </summary>
    /// <remarks>
    /// Attach it to a <see cref="Border"/> layered over a card. The border's background becomes a
    /// radial gradient whose centre tracks the cursor, and the whole layer fades in on enter and out
    /// on leave so nothing is drawn while the pointer is elsewhere.
    ///
    /// The overlay itself is not hit-testable - it must not swallow clicks meant for the slider and
    /// media buttons underneath. Mouse events are therefore taken from its **parent**, which is the
    /// card's container: events from every child bubble up to it, so the highlight keeps tracking
    /// even while the pointer is over a button. Positions are still measured relative to the
    /// overlay, so the maths is unaffected.
    ///
    /// The highlight is kept circular by converting a fixed pixel radius into the relative units the
    /// brush uses, redone whenever the card resizes - cards change height as media content comes and
    /// goes.
    ///
    /// This deliberately does not reuse <see cref="RevealBrushHelper"/>: that tracks the pointer
    /// against the root visual and drives an <see cref="UIElement.OpacityMask"/> for button reveal,
    /// whereas this needs coordinates local to one card and paints a colour layer instead.
    /// </remarks>
    public static class CursorRefractionHelper
    {
        /// <summary>Radius of the highlight, in device-independent pixels.</summary>
        private const double RadiusPixels = 130.0;

        private static readonly Duration FadeIn = new(TimeSpan.FromMilliseconds(260));

        private static readonly Duration FadeOut = new(TimeSpan.FromMilliseconds(420));

        #region IsEnabled

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(CursorRefractionHelper),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Border border)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                border.Background = CreateBrush(GetAccentColor(border));
                border.Opacity = 0.0;
                border.IsHitTestVisible = false;

                border.Loaded += OnLoaded;
                border.Unloaded += OnUnloaded;
                border.SizeChanged += OnSizeChanged;

                if (border.IsLoaded)
                {
                    Subscribe(border);
                }
            }
            else
            {
                border.Loaded -= OnLoaded;
                border.Unloaded -= OnUnloaded;
                border.SizeChanged -= OnSizeChanged;

                Unsubscribe(border);
                border.Background = null;
            }
        }

        #endregion

        #region AccentColor

        /// <summary>Tint of the highlight. Bind this to the user's chosen accent colour.</summary>
        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.RegisterAttached(
                "AccentColor",
                typeof(Color),
                typeof(CursorRefractionHelper),
                new PropertyMetadata(Colors.White, OnAccentColorChanged));

        public static Color GetAccentColor(DependencyObject obj) => (Color)obj.GetValue(AccentColorProperty);

        public static void SetAccentColor(DependencyObject obj, Color value) => obj.SetValue(AccentColorProperty, value);

        private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Border border && border.Background is RadialGradientBrush brush && e.NewValue is Color colour)
            {
                ApplyStops(brush, colour);
            }
        }

        #endregion

        #region Host (the parent we take mouse events from)

        private static readonly DependencyProperty HostProperty =
            DependencyProperty.RegisterAttached(
                "Host", typeof(FrameworkElement), typeof(CursorRefractionHelper), new PropertyMetadata(null));

        #endregion

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                Subscribe(border);
                UpdateRadius(border);
            }
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                Unsubscribe(border);
            }
        }

        private static void Subscribe(Border border)
        {
            Unsubscribe(border);

            if (VisualTreeHelper.GetParent(border) is not FrameworkElement host)
            {
                return;
            }

            border.SetValue(HostProperty, host);

            host.MouseEnter += OnHostMouseEnter;
            host.MouseMove += OnHostMouseMove;
            host.MouseLeave += OnHostMouseLeave;
        }

        private static void Unsubscribe(Border border)
        {
            if (border.GetValue(HostProperty) is not FrameworkElement host)
            {
                return;
            }

            host.MouseEnter -= OnHostMouseEnter;
            host.MouseMove -= OnHostMouseMove;
            host.MouseLeave -= OnHostMouseLeave;

            border.ClearValue(HostProperty);
        }

        /// <summary>Finds the overlay belonging to the card that raised the event.</summary>
        private static Border FindOverlay(object sender)
        {
            if (sender is not FrameworkElement host)
            {
                return null;
            }

            int count = VisualTreeHelper.GetChildrenCount(host);

            for (int i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(host, i) is Border child && GetIsEnabled(child))
                {
                    return child;
                }
            }

            return null;
        }

        private static void OnHostMouseEnter(object sender, MouseEventArgs e)
        {
            Border overlay = FindOverlay(sender);

            if (overlay != null)
            {
                Track(overlay, e);
                Fade(overlay, 1.0, FadeIn);
            }
        }

        private static void OnHostMouseMove(object sender, MouseEventArgs e)
        {
            Border overlay = FindOverlay(sender);

            if (overlay != null)
            {
                Track(overlay, e);
            }
        }

        private static void OnHostMouseLeave(object sender, MouseEventArgs e)
        {
            Border overlay = FindOverlay(sender);

            if (overlay != null)
            {
                Fade(overlay, 0.0, FadeOut);
            }
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is Border border)
            {
                UpdateRadius(border);
            }
        }

        private static RadialGradientBrush CreateBrush(Color accent)
        {
            var brush = new RadialGradientBrush
            {
                MappingMode = BrushMappingMode.RelativeToBoundingBox,
                Center = new Point(0.5, 0.5),
                GradientOrigin = new Point(0.5, 0.5),
                RadiusX = 0.35,
                RadiusY = 0.35
            };

            ApplyStops(brush, accent);

            return brush;
        }

        private static void ApplyStops(RadialGradientBrush brush, Color accent)
        {
            brush.GradientStops = new GradientStopCollection
            {
                new GradientStop(Color.FromArgb(0x3A, accent.R, accent.G, accent.B), 0.0),
                new GradientStop(Color.FromArgb(0x16, accent.R, accent.G, accent.B), 0.40),
                new GradientStop(Color.FromArgb(0x00, accent.R, accent.G, accent.B), 1.0)
            };
        }

        private static void Track(Border overlay, MouseEventArgs e)
        {
            if (overlay.Background is not RadialGradientBrush brush)
            {
                return;
            }

            double w = overlay.ActualWidth;
            double h = overlay.ActualHeight;

            if (w <= 0 || h <= 0)
            {
                return;
            }

            Point p = e.GetPosition(overlay);
            var centre = new Point(p.X / w, p.Y / h);

            brush.Center = centre;
            brush.GradientOrigin = centre;

            UpdateRadius(overlay);
        }

        /// <summary>
        /// Converts the fixed pixel radius into the brush's relative units, so the highlight stays
        /// round on a card that is much wider than it is tall.
        /// </summary>
        private static void UpdateRadius(Border overlay)
        {
            if (overlay.Background is not RadialGradientBrush brush)
            {
                return;
            }

            double w = overlay.ActualWidth;
            double h = overlay.ActualHeight;

            if (w <= 0 || h <= 0)
            {
                return;
            }

            brush.RadiusX = RadiusPixels / w;
            brush.RadiusY = RadiusPixels / h;
        }

        private static void Fade(Border overlay, double to, Duration duration)
        {
            overlay.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation
            {
                To = to,
                Duration = duration,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
        }
    }
}
