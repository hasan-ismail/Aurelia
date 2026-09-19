using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ModernFlyouts.Core.UI
{
    public partial class FlyoutWindow
    {
        private DispatcherTimer timer;

        private void OpenFlyout()
        {
            Show();
            timer?.Stop();

            RoutedEventArgs args = new(OpenedEvent);
            RaiseEvent(args);


            if (FlyoutAnimationEnabled && AreAnimationsAllowed)
            {
                c_translation = 40;
                PlayOpenAnimation();
            }
            else
            {
                c_translation = 0;
                RenderTransform = CreateRenderTransform();
                BeginAnimation(VisibilityProperty, null);
                BeginAnimation(OpacityProperty, null);
                Visibility = Visibility.Visible;
                Opacity = 1.0;
            }

            timer?.Start();
        }

        private void CloseFlyout()
        {
            RoutedEventArgs args = new(ClosingEvent);
            RaiseEvent(args);

            if (FlyoutAnimationEnabled && AreAnimationsAllowed)
            {
                PlayCloseAnimation();
            }
            else
            {
                Opacity = 0.0;
                Visibility = Visibility.Hidden;
            }
        }

        #region Close Timer

        private void SetupCloseTimer()
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Timeout) };

            timer.Tick += (_, __) =>
            {
                timer.Stop();

                if (!IsTimeoutEnabled || IsDragMoving)
                    return;

                RoutedEventArgs args = new(ClosingEvent);
                RaiseEvent(args);

                if (!IsMouseOver && !args.Handled)
                {
                    IsOpen = false;
                }
            };

            MouseEnter += (_, e) =>
            {
                timer.Stop();
            };
            MouseLeave += (_, e) =>
            {
                timer.Start();
            };

            PreviewMouseDown += (_, __) => timer.Stop();
            PreviewStylusDown += (_, __) => timer.Stop();
            PreviewTouchDown += (_, __) => timer.Stop();
            PreviewMouseUp += (_, __) => { timer.Stop(); timer.Start(); };
            PreviewStylusUp += (_, __) => { timer.Stop(); timer.Start(); };
            PreviewTouchUp += (_, __) => { timer.Stop(); timer.Start(); };
        }

        public void StartCloseTimer()
        {
            if (IsTimeoutEnabled && timer != null && !timer.IsEnabled) { timer?.Start(); }
        }

        public void StopCloseTimer()
        {
            timer?.Stop();
        }

        public void UpdateCloseTimerInterval(double timeout)
        {
            if (timer == null)
                return;

            var isRunning = timer.IsEnabled;

            timer.Stop();
            timer.Interval = TimeSpan.FromMilliseconds(timeout);

            if (IsTimeoutEnabled && isRunning) timer.Start();
        }

        #endregion

        #region Animations

        private bool hasAnimationsCreated;
        private Storyboard openingStoryboard;
        private Storyboard closingStoryboard;
        private DoubleKeyFrame fromHorizontalOffsetKeyFrameOpening;
        private DoubleKeyFrame fromVerticalOffsetKeyFrameOpening;
        private DoubleKeyFrame fromHorizontalOffsetKeyFrameClosing;
        private DoubleKeyFrame fromVerticalOffsetKeyFrameClosing;

        private double c_translation = 40;
        private static readonly TimeSpan translateDuration = TimeSpan.FromMilliseconds(367);

        private static readonly PropertyPath opacityPath = new(OpacityProperty);
        private static readonly PropertyPath visibilityPath = new(VisibilityProperty);
        // RenderTransform is a TransformGroup of [0] scale, [1] translate, so the flyout can rise
        // and swell very slightly at the same time.
        private static readonly PropertyPath translateXPath = new("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.X)");
        private static readonly PropertyPath translateYPath = new("(UIElement.RenderTransform).(TransformGroup.Children)[1].(TranslateTransform.Y)");
        private static readonly PropertyPath scaleXPath = new("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)");
        private static readonly PropertyPath scaleYPath = new("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)");

        private static readonly KeySpline decelerateKeySplineOpening = new(0.1, 0.9, 0.2, 1);
        private static readonly KeySpline decelerateKeySplineClosing = new(1, 0.2, 0.9, 0.1);

        // Gentle ease for opacity, so the flyout washes in rather than snapping on.
        private static readonly KeySpline fadeKeySpline = new(0.25, 0.1, 0.25, 1);

        // How small the flyout starts before settling at full size.
        private const double openScale = 0.94;

        private static readonly TimeSpan fadeInDuration = TimeSpan.FromMilliseconds(220);
        private static readonly TimeSpan fadeOutDuration = TimeSpan.FromMilliseconds(180);

        /// <summary>
        /// The Windows "Show animations" accessibility setting. When someone has turned animations
        /// off system-wide, honour it rather than animating anyway.
        /// </summary>
        private static bool AreAnimationsAllowed => SystemParameters.ClientAreaAnimation;

        private static TransformGroup CreateRenderTransform() => new()
        {
            Children =
            {
                new ScaleTransform(1.0, 1.0),
                new TranslateTransform()
            }
        };


        private void PrepareAnimations()
        {
            EnsureClosingStoryboard();
            EnsureOpeningStoryboard();

            Opacity = 0.0;
            if (RenderTransform is not TransformGroup)
            {
                RenderTransform = CreateRenderTransform();
                RenderTransformOrigin = new Point(0.5, 0.5);
            }

            hasAnimationsCreated = true;

            UpdateAnimations(ActualExpandDirection);
        }

        private void EnsureOpeningStoryboard()
        {
            if (openingStoryboard == null)
            {
                ObjectAnimationUsingKeyFrames visibilityAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteObjectKeyFrame(Visibility.Visible, TimeSpan.Zero)
                    }
                    
                };
                Storyboard.SetTarget(visibilityAnim, this);
                Storyboard.SetTargetProperty(visibilityAnim, visibilityPath);
                

                // Fades from the first frame on an ease, rather than sitting invisible for 83ms
                // and then snapping in linearly - that hold is what made the entrance feel abrupt.
                DoubleAnimationUsingKeyFrames opacityAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(0, TimeSpan.Zero),
                        new SplineDoubleKeyFrame(1, fadeInDuration, fadeKeySpline)
                    }
                };
                Storyboard.SetTarget(opacityAnim, this);
                Storyboard.SetTargetProperty(opacityAnim, opacityPath);

                DoubleAnimationUsingKeyFrames scaleXAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(openScale, TimeSpan.Zero),
                        new SplineDoubleKeyFrame(1.0, translateDuration, decelerateKeySplineOpening)
                    }
                };
                Storyboard.SetTarget(scaleXAnim, this);
                Storyboard.SetTargetProperty(scaleXAnim, scaleXPath);

                DoubleAnimationUsingKeyFrames scaleYAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(openScale, TimeSpan.Zero),
                        new SplineDoubleKeyFrame(1.0, translateDuration, decelerateKeySplineOpening)
                    }
                };
                Storyboard.SetTarget(scaleYAnim, this);
                Storyboard.SetTargetProperty(scaleYAnim, scaleYPath);

                DoubleAnimationUsingKeyFrames xAnim = new()
                {
                    KeyFrames =
                    {
                        (fromHorizontalOffsetKeyFrameOpening = new DiscreteDoubleKeyFrame(0, TimeSpan.Zero)),
                        new SplineDoubleKeyFrame(0, translateDuration, decelerateKeySplineOpening)
                    }
                };
                Storyboard.SetTarget(xAnim, this);
                Storyboard.SetTargetProperty(xAnim, translateXPath);

                DoubleAnimationUsingKeyFrames yAnim = new()
                {
                    KeyFrames =
                    {
                        (fromVerticalOffsetKeyFrameOpening = new DiscreteDoubleKeyFrame(0, TimeSpan.Zero)),
                        new SplineDoubleKeyFrame(0, translateDuration, decelerateKeySplineOpening)
                    }
                };
                Storyboard.SetTarget(yAnim, this);
                Storyboard.SetTargetProperty(yAnim, translateYPath);

                openingStoryboard = new()
                {
                    Children = { visibilityAnim, opacityAnim, scaleXAnim, scaleYAnim, xAnim, yAnim },
                };
            }
        }

        private void EnsureClosingStoryboard()
        {
            if (closingStoryboard == null)
            {
                DoubleAnimationUsingKeyFrames opacityAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(1, TimeSpan.Zero),
                        new SplineDoubleKeyFrame(0, fadeOutDuration, fadeKeySpline)
                    }
                };
                Storyboard.SetTarget(opacityAnim, this);
                Storyboard.SetTargetProperty(opacityAnim, opacityPath);

                DoubleAnimationUsingKeyFrames scaleXAnimClosing = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(1.0, TimeSpan.Zero),
                        new SplineDoubleKeyFrame(openScale, fadeOutDuration, decelerateKeySplineClosing)
                    }
                };
                Storyboard.SetTarget(scaleXAnimClosing, this);
                Storyboard.SetTargetProperty(scaleXAnimClosing, scaleXPath);

                DoubleAnimationUsingKeyFrames scaleYAnimClosing = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(1.0, TimeSpan.Zero),
                        new SplineDoubleKeyFrame(openScale, fadeOutDuration, decelerateKeySplineClosing)
                    }
                };
                Storyboard.SetTarget(scaleYAnimClosing, this);
                Storyboard.SetTargetProperty(scaleYAnimClosing, scaleYPath);

                DoubleAnimationUsingKeyFrames xAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(0, TimeSpan.Zero),
                        (fromHorizontalOffsetKeyFrameClosing = new SplineDoubleKeyFrame(
                            0, translateDuration, decelerateKeySplineClosing))
                    }
                };
                Storyboard.SetTarget(xAnim, this);
                Storyboard.SetTargetProperty(xAnim, translateXPath);

                DoubleAnimationUsingKeyFrames yAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteDoubleKeyFrame(0, TimeSpan.Zero),
                        (fromVerticalOffsetKeyFrameClosing = new SplineDoubleKeyFrame(
                            0, translateDuration, decelerateKeySplineClosing))
                    }
                };
                Storyboard.SetTarget(yAnim, this);
                Storyboard.SetTargetProperty(yAnim, translateYPath);

                ObjectAnimationUsingKeyFrames visibilityAnim = new()
                {
                    KeyFrames =
                    {
                        new DiscreteObjectKeyFrame(Visibility.Hidden, TimeSpan.FromMilliseconds(250))
                    }
                };
                Storyboard.SetTarget(visibilityAnim, this);
                Storyboard.SetTargetProperty(visibilityAnim, visibilityPath);

                closingStoryboard = new()
                {
                    Children = { opacityAnim, scaleXAnimClosing, scaleYAnimClosing, xAnim, yAnim, visibilityAnim },
                };
            }
        }

        private void PlayOpenAnimation()
        {
            openingStoryboard.Begin(this, true);
        }

        private void PlayCloseAnimation()
        {
            closingStoryboard.Begin(this, true);
        }

        private void UpdateAnimations(FlyoutWindowExpandDirection expandDirection)
        {
            if (!hasAnimationsCreated)
                return;

            switch (expandDirection)
            {
                case FlyoutWindowExpandDirection.Auto:
                    fromHorizontalOffsetKeyFrameOpening.Value = 0;
                    fromHorizontalOffsetKeyFrameClosing.Value = 0;
                    fromVerticalOffsetKeyFrameOpening.Value = 0;
                    fromVerticalOffsetKeyFrameClosing.Value = 0;
                    break;

                case FlyoutWindowExpandDirection.Up:
                    fromHorizontalOffsetKeyFrameOpening.Value = 0;
                    fromHorizontalOffsetKeyFrameClosing.Value = 0;
                    fromVerticalOffsetKeyFrameOpening.Value = c_translation;
                    fromVerticalOffsetKeyFrameClosing.Value = c_translation;
                    break;

                case FlyoutWindowExpandDirection.Down:
                    fromHorizontalOffsetKeyFrameOpening.Value = 0;
                    fromHorizontalOffsetKeyFrameClosing.Value = 0;
                    fromVerticalOffsetKeyFrameOpening.Value = -c_translation;
                    fromVerticalOffsetKeyFrameClosing.Value = -c_translation;
                    break;

                case FlyoutWindowExpandDirection.Left:
                    fromHorizontalOffsetKeyFrameOpening.Value = c_translation;
                    fromHorizontalOffsetKeyFrameClosing.Value = c_translation;
                    fromVerticalOffsetKeyFrameOpening.Value = 0;
                    fromVerticalOffsetKeyFrameClosing.Value = 0;
                    break;

                case FlyoutWindowExpandDirection.Right:
                    fromHorizontalOffsetKeyFrameOpening.Value = -c_translation;
                    fromHorizontalOffsetKeyFrameClosing.Value = -c_translation;
                    fromVerticalOffsetKeyFrameOpening.Value = 0;
                    fromVerticalOffsetKeyFrameClosing.Value = 0;
                    break;
            }
        }

        #endregion
    }
}
