using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ModernFlyouts.Behaviors
{
    /// <summary>
    /// Grows a slider's thumb while the pointer is over the slider, and presses it in slightly
    /// while it is being dragged - the same feel as the Windows 11 volume slider.
    /// </summary>
    /// <remarks>
    /// This is an attached property rather than a restyle on purpose. The sliders here use
    /// ModernWpf's default template, and the thumb's visual is defined inside it; overriding that
    /// would mean re-declaring the whole control template and inheriting the maintenance of it.
    /// Reaching into the template for the <see cref="Thumb"/> once it is loaded and animating a
    /// <see cref="ScaleTransform"/> leaves the template alone and keeps working if it changes.
    ///
    /// Attach with <c>behaviors:SliderThumbHoverBehavior.IsEnabled="True"</c> on a <see cref="Slider"/>.
    /// </remarks>
    public static class SliderThumbHoverBehavior
    {
        private const double HoverScale = 1.28;

        private const double PressedScale = 1.12;

        private static readonly Duration Grow = new(TimeSpan.FromMilliseconds(160));

        private static readonly Duration Shrink = new(TimeSpan.FromMilliseconds(220));

        #region IsEnabled

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SliderThumbHoverBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Slider slider)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                slider.Loaded += OnLoaded;
                slider.Unloaded += OnUnloaded;

                if (slider.IsLoaded)
                {
                    Attach(slider);
                }
            }
            else
            {
                slider.Loaded -= OnLoaded;
                slider.Unloaded -= OnUnloaded;
                Detach(slider);
            }
        }

        #endregion

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Slider slider)
            {
                Attach(slider);
            }
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is Slider slider)
            {
                Detach(slider);
            }
        }

        private static void Attach(Slider slider)
        {
            Detach(slider);

            Thumb thumb = FindThumb(slider);

            if (thumb == null)
            {
                // The template may not have been applied yet; try again once it has.
                slider.ApplyTemplate();
                thumb = FindThumb(slider);

                if (thumb == null)
                {
                    return;
                }
            }

            thumb.RenderTransformOrigin = new Point(0.5, 0.5);
            thumb.RenderTransform = new ScaleTransform(1.0, 1.0);

            slider.MouseEnter += OnSliderMouseEnter;
            slider.MouseLeave += OnSliderMouseLeave;
            thumb.DragStarted += OnThumbDragStarted;
            thumb.DragCompleted += OnThumbDragCompleted;
        }

        private static void Detach(Slider slider)
        {
            slider.MouseEnter -= OnSliderMouseEnter;
            slider.MouseLeave -= OnSliderMouseLeave;

            Thumb thumb = FindThumb(slider);

            if (thumb != null)
            {
                thumb.DragStarted -= OnThumbDragStarted;
                thumb.DragCompleted -= OnThumbDragCompleted;
            }
        }

        private static void OnSliderMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
            => ScaleTo(sender as Slider, HoverScale, Grow);

        private static void OnSliderMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
            => ScaleTo(sender as Slider, 1.0, Shrink);

        private static void OnThumbDragStarted(object sender, DragStartedEventArgs e)
            => ScaleTo(FindOwner(sender as Thumb), PressedScale, Grow);

        private static void OnThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            Slider slider = FindOwner(sender as Thumb);

            if (slider != null)
            {
                // Settle back to whichever state the pointer is actually in.
                ScaleTo(slider, slider.IsMouseOver ? HoverScale : 1.0, Shrink);
            }
        }

        private static void ScaleTo(Slider slider, double scale, Duration duration)
        {
            if (slider == null)
            {
                return;
            }

            if (FindThumb(slider)?.RenderTransform is not ScaleTransform transform)
            {
                return;
            }

            var animation = new DoubleAnimation
            {
                To = scale,
                Duration = duration,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private static Slider FindOwner(Thumb thumb)
        {
            DependencyObject current = thumb;

            while (current != null)
            {
                if (current is Slider slider)
                {
                    return slider;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static Thumb FindThumb(DependencyObject root)
        {
            if (root is Thumb thumb)
            {
                return thumb;
            }

            int count = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < count; i++)
            {
                Thumb found = FindThumb(VisualTreeHelper.GetChild(root, i));

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
