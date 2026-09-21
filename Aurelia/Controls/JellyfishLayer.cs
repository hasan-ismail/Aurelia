using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Aurelia.Controls
{
    /// <summary>
    /// Jellyfish drifting upward behind the flyout's content, drawn from scratch.
    /// </summary>
    /// <remarks>
    /// Each one is a domed bell with radiating ribs, a pair of flared veils, wide ribbon arms that
    /// undulate and taper, and finer tentacles trailing past them - the silhouette of a swimming
    /// medusa rather than a cartoon mushroom. The whole animal is sized to a slider strip, which is
    /// short and wide, so it fits the card it drifts through instead of being cropped to a blob.
    ///
    /// It is all vector geometry rendered into a single <see cref="DrawingVisual"/>, so it stays
    /// resolution-independent, ships no artwork, and costs one visual per frame instead of a tree
    /// of animated shapes.
    ///
    /// The frame hook is attached only while the layer is on screen. The flyout spends nearly all of
    /// its life hidden, and a tray utility should not hold a per-frame callback the rest of the
    /// time. When Windows' "Show animations" setting is off, the jellyfish are drawn but hold still.
    /// </remarks>
    public class JellyfishLayer : FrameworkElement
    {
        private sealed class Medusa
        {
            public double X;
            public double Y;
            public double Scale;
            public double Speed;
            public double SwayAmplitude;
            public double SwayFrequency;
            public double PulseFrequency;
            public double Phase;
            public double Alpha;
            public double Lean;
        }

        private readonly List<Medusa> medusae = new();
        private readonly DrawingVisual visual = new();
        private readonly Random random = new();

        private TimeSpan lastRender;
        private double time;
        private bool isHooked;

        public JellyfishLayer()
        {
            IsHitTestVisible = false;
            AddVisualChild(visual);

            IsVisibleChanged += OnIsVisibleChanged;
            SizeChanged += (_, _) => { Populate(); Render(); };
            Unloaded += (_, _) => Unhook();
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => visual;

        #region Tint

        public static readonly DependencyProperty TintProperty =
            DependencyProperty.Register(
                nameof(Tint),
                typeof(Color),
                typeof(JellyfishLayer),
                new PropertyMetadata(Color.FromRgb(0x3F, 0xE0, 0xCB), (d, _) => ((JellyfishLayer)d).Render()));

        /// <summary>Colour the jellyfish are drawn in; follows the theme's accent.</summary>
        public Color Tint
        {
            get => (Color)GetValue(TintProperty);
            set => SetValue(TintProperty, value);
        }

        #endregion

        #region Count

        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(
                nameof(Count),
                typeof(int),
                typeof(JellyfishLayer),
                new PropertyMetadata(6, (d, _) => { var l = (JellyfishLayer)d; l.Populate(); l.Render(); }));

        /// <summary>Upper bound on how many drift through at once; the layer's size sets the rest.</summary>
        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        #endregion

        private static bool AreAnimationsAllowed => SystemParameters.ClientAreaAnimation;

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                if (medusae.Count == 0)
                {
                    Populate();
                }

                Render();
                Hook();
            }
            else
            {
                Unhook();
            }
        }

        private void Hook()
        {
            if (isHooked || !AreAnimationsAllowed)
            {
                return;
            }

            lastRender = TimeSpan.Zero;
            CompositionTarget.Rendering += OnRendering;
            isHooked = true;
        }

        private void Unhook()
        {
            if (!isHooked)
            {
                return;
            }

            CompositionTarget.Rendering -= OnRendering;
            isHooked = false;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (e is not RenderingEventArgs args || args.RenderingTime == lastRender)
            {
                return;   // WPF can raise this more than once for the same frame
            }

            double delta = lastRender == TimeSpan.Zero
                ? 1.0 / 60.0
                : (args.RenderingTime - lastRender).TotalSeconds;

            lastRender = args.RenderingTime;

            // A hitch, or a machine resuming from sleep, must not teleport everything.
            delta = Math.Min(delta, 0.1);
            time += delta;

            foreach (Medusa m in medusae)
            {
                m.Y -= m.Speed * delta;

                // The arms trail a long way below the bell, so it is only truly gone once the
                // longest tentacle has cleared the top - otherwise strands pop out of existence.
                double bh = 13 * m.Scale;

                if (m.Y < -bh * 8)
                {
                    m.Y = ActualHeight + bh * 2.5;
                    m.X = random.NextDouble() * Math.Max(1, ActualWidth);
                }
            }

            Render();
        }

        private void Populate()
        {
            medusae.Clear();

            double w = ActualWidth, h = ActualHeight;

            if (w <= 0 || h <= 0)
            {
                return;
            }

            // A slider card is a short, wide strip, so the bell is sized off the height and the
            // count off the width. The arms are far longer than the card is tall and simply get
            // clipped - a jellyfish passing through the frame reads better than one shrunk to fit.
            double fit = Math.Clamp(h / 95.0, 0.30, 1.4);
            int count = Math.Clamp((int)Math.Round(w / 100.0), 1, Count);

            for (int i = 0; i < count; i++)
            {
                double bh = 13 * fit;

                medusae.Add(new Medusa
                {
                    X = (i + 0.5) / count * w + (random.NextDouble() - 0.5) * w * 0.2,
                    // Spread over the whole travel, including the stretch below the card, so they
                    // do not all arrive in a row on the first frame.
                    Y = -bh * 8 + random.NextDouble() * (h + bh * 10),
                    Scale = fit * (0.78 + random.NextDouble() * 0.5),
                    Speed = 7 + random.NextDouble() * 10,
                    SwayAmplitude = 3 + random.NextDouble() * 7,
                    SwayFrequency = 0.10 + random.NextDouble() * 0.16,
                    PulseFrequency = 0.30 + random.NextDouble() * 0.22,
                    Phase = random.NextDouble() * Math.PI * 2,
                    Alpha = 0.50 + random.NextDouble() * 0.30,
                    Lean = (random.NextDouble() - 0.5) * 0.5
                });
            }
        }

        private void Render()
        {
            double w = ActualWidth, h = ActualHeight;

            using DrawingContext dc = visual.RenderOpen();

            if (w <= 0 || h <= 0 || medusae.Count == 0)
            {
                return;
            }

            dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h)));

            foreach (Medusa m in medusae)
            {
                Draw(dc, m);
            }

            dc.Pop();
        }

        private Color Shade(double alpha, double lift = 0)
        {
            Color t = Tint;

            // "lift" washes toward white, which is what gives the bell its bright, milky core.
            byte Mix(byte c) => (byte)Math.Clamp(c + (255 - c) * lift, 0, 255);

            return Color.FromArgb((byte)Math.Clamp(alpha * 255, 0, 255), Mix(t.R), Mix(t.G), Mix(t.B));
        }

        private void Draw(DrawingContext dc, Medusa m)
        {
            double pulse = Math.Sin(time * m.PulseFrequency * Math.PI * 2 + m.Phase);
            double sway = Math.Sin(time * m.SwayFrequency * Math.PI * 2 + m.Phase) * m.SwayAmplitude;

            double cx = m.X + sway;
            double cy = m.Y;

            // Squash and stretch: a wider bell is a flatter bell, as when it contracts to swim.
            double bw = 17 * m.Scale * (1 + pulse * 0.13);
            double bh = 13 * m.Scale * (1 - pulse * 0.17);

            double a = m.Alpha;

            // Back to front: veils, then the fine tentacles, then the arms over them, then
            // the bell last so its rim reads cleanly against everything it overlaps.
            DrawVeils(dc, m, cx, cy, bw, bh, a);
            DrawTentacles(dc, m, cx, cy, bw, bh, a);
            DrawRibbons(dc, m, cx, cy, bw, bh, a);
            DrawBell(dc, m, cx, cy, bw, bh, a);
        }

        /// <summary>The dome, its rim light, and the ribs fanning across it.</summary>
        private void DrawBell(DrawingContext dc, Medusa m, double cx, double cy, double bw, double bh, double a)
        {
            var dome = new StreamGeometry();
            using (StreamGeometryContext g = dome.Open())
            {
                g.BeginFigure(new Point(cx - bw, cy), true, true);
                g.BezierTo(new Point(cx - bw * 1.02, cy - bh * 1.95),
                           new Point(cx + bw * 1.02, cy - bh * 1.95),
                           new Point(cx + bw, cy), true, true);
                // A scalloped hem rather than a flat cut, so it reads as a bell.
                g.BezierTo(new Point(cx + bw * 0.52, cy + bh * 0.38),
                           new Point(cx - bw * 0.52, cy + bh * 0.38),
                           new Point(cx - bw, cy), true, true);
            }
            dome.Freeze();

            var fill = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.18),
                Center = new Point(0.5, 0.30),
                RadiusX = 0.72,
                RadiusY = 0.85,
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Shade(a * 1.00, 0.55), 0.0),
                    new GradientStop(Shade(a * 0.70, 0.20), 0.45),
                    new GradientStop(Shade(a * 0.30, 0.00), 0.80),
                    new GradientStop(Shade(0.0), 1.0)
                }
            };
            fill.Freeze();
            dc.DrawGeometry(fill, null, dome);

            var rim = new Pen(new SolidColorBrush(Shade(a * 0.90, 0.65)), Math.Max(0.9, 1.6 * m.Scale));
            rim.Freeze();
            dc.DrawGeometry(null, rim, dome);

            var ribPen = new Pen(new SolidColorBrush(Shade(a * 0.45, 0.35)), Math.Max(0.6, 1.1 * m.Scale));
            ribPen.Freeze();

            const int ribs = 5;
            for (int i = 0; i < ribs; i++)
            {
                double t = (i + 0.5) / ribs * 2 - 1;          // -1 .. 1
                double topX = cx + t * bw * 0.34;
                double hemX = cx + t * bw * 0.94;

                var rib = new StreamGeometry();
                using (StreamGeometryContext g = rib.Open())
                {
                    g.BeginFigure(new Point(topX, cy - bh * 1.35), false, false);
                    g.BezierTo(new Point(topX + (hemX - topX) * 0.4, cy - bh * 0.7),
                               new Point(hemX, cy - bh * 0.35),
                               new Point(hemX, cy + bh * 0.05), true, true);
                }
                rib.Freeze();
                dc.DrawGeometry(null, ribPen, rib);
            }
        }

        /// <summary>Translucent sheets flaring off the bell, which give it depth.</summary>
        private void DrawVeils(DrawingContext dc, Medusa m, double cx, double cy, double bw, double bh, double a)
        {
            double wobble = Math.Sin(time * m.SwayFrequency * Math.PI * 2 + m.Phase - 0.8);

            for (int side = -1; side <= 1; side += 2)
            {
                var veil = new StreamGeometry();
                using (StreamGeometryContext g = veil.Open())
                {
                    double outer = side * bw * (1.55 + wobble * 0.12);
                    g.BeginFigure(new Point(cx + side * bw * 0.35, cy - bh * 1.1), true, true);
                    g.BezierTo(new Point(cx + outer, cy - bh * 1.1),
                               new Point(cx + outer * 1.05, cy + bh * 1.4),
                               new Point(cx + side * bw * 0.75, cy + bh * 2.3), true, true);
                    g.BezierTo(new Point(cx + side * bw * 0.72, cy + bh * 0.7),
                               new Point(cx + side * bw * 0.62, cy - bh * 0.4),
                               new Point(cx + side * bw * 0.35, cy - bh * 1.1), true, true);
                }
                veil.Freeze();

                var brush = new SolidColorBrush(Shade(a * 0.20, 0.25));
                brush.Freeze();
                dc.DrawGeometry(brush, null, veil);
            }
        }

        /// <summary>The wide oral arms: tapering ribbons that undulate and trail behind the bell.</summary>
        private void DrawRibbons(DrawingContext dc, Medusa m, double cx, double cy, double bw, double bh, double a)
        {
            const int ribbons = 3;
            const int samples = 16;

            for (int r = 0; r < ribbons; r++)
            {
                double offset = (r - (ribbons - 1) / 2.0) / Math.Max(1, (ribbons - 1) / 2.0);
                double rootX = cx + offset * bw * 0.45;
                double length = bh * (3.2 + r % 2 * 1.0);
                double phase = m.Phase - r * 0.7;

                var left = new Point[samples];
                var right = new Point[samples];

                for (int i = 0; i < samples; i++)
                {
                    double u = i / (double)(samples - 1);          // 0 at the bell, 1 at the tip

                    // Each segment lags the one above it, so the ribbon ripples along its length.
                    double wave = Math.Sin(time * 0.9 * Math.PI + phase - u * 3.4);
                    double drift = m.Lean * length * u * 0.5;
                    double x = rootX + wave * bw * 0.55 * u + drift;
                    double y = cy + bh * 0.25 + length * u;

                    // Wide where it leaves the bell, pinched to nothing at the tip, with a ruffle.
                    double halfWidth = bw * 0.50 * (1 - u) * (1 - u * 0.25)
                                     + bw * 0.08 * Math.Sin(u * 14 + phase);

                    left[i] = new Point(x - halfWidth, y);
                    right[i] = new Point(x + halfWidth, y);
                }

                var ribbon = new StreamGeometry();
                using (StreamGeometryContext g = ribbon.Open())
                {
                    g.BeginFigure(left[0], true, true);
                    for (int i = 1; i < samples; i++)
                    {
                        g.LineTo(left[i], true, true);
                    }
                    for (int i = samples - 1; i >= 0; i--)
                    {
                        g.LineTo(right[i], true, true);
                    }
                }
                ribbon.Freeze();

                var fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Shade(a * 0.72, 0.30), 0.0),
                        new GradientStop(Shade(a * 0.40, 0.10), 0.45),
                        new GradientStop(Shade(0.0), 1.0)
                    }
                };
                fill.Freeze();
                dc.DrawGeometry(fill, null, ribbon);
            }
        }

        /// <summary>Fine tentacles trailing well past the arms.</summary>
        private void DrawTentacles(DrawingContext dc, Medusa m, double cx, double cy, double bw, double bh, double a)
        {
            const int tentacles = 5;
            const int samples = 14;

            var pen = new Pen(new SolidColorBrush(Shade(a * 0.45, 0.15)), Math.Max(0.8, 1.3 * m.Scale))
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            pen.Freeze();

            for (int t = 0; t < tentacles; t++)
            {
                double offset = (t - (tentacles - 1) / 2.0) / ((tentacles - 1) / 2.0);
                double rootX = cx + offset * bw * 0.85;
                double length = bh * (4.2 + (t % 3) * 1.0);
                double phase = m.Phase - t * 0.45;

                var path = new StreamGeometry();
                using (StreamGeometryContext g = path.Open())
                {
                    var pts = new Point[samples];
                    for (int i = 0; i < samples; i++)
                    {
                        double u = i / (double)(samples - 1);
                        double wave = Math.Sin(time * 0.8 * Math.PI + phase - u * 4.2);
                        pts[i] = new Point(
                            rootX + wave * bw * 0.42 * u + m.Lean * length * u * 0.55,
                            cy + bh * 0.2 + length * u);
                    }

                    g.BeginFigure(pts[0], false, false);
                    for (int i = 1; i < samples; i++)
                    {
                        g.LineTo(pts[i], true, true);
                    }
                }
                path.Freeze();
                dc.DrawGeometry(null, pen, path);
            }
        }
    }
}
