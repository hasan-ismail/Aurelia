using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Aurelia.Controls
{
    /// <summary>A preset swatch and a hex field, both driving one <see cref="SelectedColor"/>.</summary>
    /// <remarks>
    /// Neither ModernWpf nor the toolkit package referenced here ships a colour picker, and a full
    /// HSV picker is more surface than this needs. Presets cover the common choices; the hex field
    /// covers everything else.
    /// </remarks>
    public partial class ColorSelector : UserControl
    {
        /// <summary>Guards the two-way sync between the hex box and the property.</summary>
        private bool isSyncing;

        public ColorSelector()
        {
            InitializeComponent();
            UpdateHexBox(SelectedColor);
        }

        public record ColorPreset(string Name, Color Value);

        public IReadOnlyList<ColorPreset> Presets { get; } = new List<ColorPreset>
        {
            new("White",     Color.FromRgb(0xFF, 0xFF, 0xFF)),
            new("Ice",       Color.FromRgb(0x9F, 0xC4, 0xFF)),
            new("Sky",       Color.FromRgb(0x4F, 0xA8, 0xFF)),
            new("Cyan",      Color.FromRgb(0x36, 0xD8, 0xE0)),
            new("Mint",      Color.FromRgb(0x4F, 0xE0, 0xA8)),
            new("Lime",      Color.FromRgb(0xA8, 0xE0, 0x4F)),
            new("Amber",     Color.FromRgb(0xFF, 0xC4, 0x4F)),
            new("Orange",    Color.FromRgb(0xFF, 0x8A, 0x3D)),
            new("Rose",      Color.FromRgb(0xFF, 0x5F, 0x8A)),
            new("Violet",    Color.FromRgb(0xB0, 0x7F, 0xFF))
        };

        #region Header

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(string), typeof(ColorSelector),
                new PropertyMetadata(string.Empty, OnHeaderChanged));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        private static readonly DependencyPropertyKey HeaderVisibilityPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HeaderVisibility), typeof(Visibility), typeof(ColorSelector),
                new PropertyMetadata(Visibility.Collapsed));

        public static readonly DependencyProperty HeaderVisibilityProperty = HeaderVisibilityPropertyKey.DependencyProperty;

        /// <summary>
        /// A dependency property rather than a computed CLR one: the latter is evaluated once when
        /// the binding is set up - while Header is still empty - and never updates, which left the
        /// labels invisible.
        /// </summary>
        public Visibility HeaderVisibility
        {
            get => (Visibility)GetValue(HeaderVisibilityProperty);
            private set => SetValue(HeaderVisibilityPropertyKey, value);
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorSelector selector)
            {
                selector.HeaderVisibility = string.IsNullOrEmpty(e.NewValue as string)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            }
        }

        #endregion

        #region SelectedColor

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(ColorSelector),
                new FrameworkPropertyMetadata(Colors.White,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorSelector selector && !selector.isSyncing && e.NewValue is Color colour)
            {
                selector.UpdateHexBox(colour);
            }
        }

        #endregion

        private void UpdateHexBox(Color colour)
        {
            if (HexBox == null)
            {
                return;
            }

            isSyncing = true;
            HexBox.Text = $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";
            isSyncing = false;
        }

        private void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: Color colour })
            {
                SelectedColor = colour;
            }
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSyncing || sender is not TextBox box)
            {
                return;
            }

            string text = box.Text?.Trim();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!text.StartsWith('#'))
            {
                text = "#" + text;
            }

            // Only #RGB, #RRGGBB and #AARRGGBB are complete; anything else is mid-typing, so leave
            // the current colour alone rather than flickering on every keystroke.
            if (text.Length is not (4 or 7 or 9))
            {
                return;
            }

            try
            {
                if (ColorConverter.ConvertFromString(text) is Color parsed)
                {
                    isSyncing = true;
                    SelectedColor = parsed;
                    isSyncing = false;
                }
            }
            catch
            {
                // Not a valid colour yet - keep what we have.
            }
        }
    }
}
