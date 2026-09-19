using System.Collections.Generic;
using System.Windows.Media;
using ModernWpf;

namespace ModernFlyouts.UI
{
    /// <summary>
    /// A named look for the flyout: accent colour, how solid the glass is, and how round the
    /// corners are.
    /// </summary>
    /// <remarks>
    /// A preset is only a starting point. Applying one writes its values into the individual
    /// settings, so anything can be adjusted afterwards without being locked to the preset - and
    /// the moment something is adjusted the selection simply reads as Custom.
    /// </remarks>
    public sealed class FlyoutThemePreset
    {
        public FlyoutThemePreset(
            string name,
            Color accent,
            double backgroundOpacity,
            double cornerRadius,
            ElementTheme theme = ElementTheme.Default)
        {
            Name = name;
            Accent = accent;
            BackgroundOpacity = backgroundOpacity;
            CornerRadius = cornerRadius;
            Theme = theme;
        }

        public string Name { get; }

        public Color Accent { get; }

        /// <summary>Percentage, matching the background-opacity setting.</summary>
        public double BackgroundOpacity { get; }

        public double CornerRadius { get; }

        /// <summary>Light, dark, or follow the system.</summary>
        public ElementTheme Theme { get; }

        /// <summary>A brush for the preset's swatch in the settings list.</summary>
        public Brush Swatch => new SolidColorBrush(Accent);

        /// <summary>
        /// Presets are ordered from the most restrained to the most decorative, so scanning the
        /// list roughly follows "how much does this stand out".
        /// </summary>
        public static IReadOnlyList<FlyoutThemePreset> All { get; } = new List<FlyoutThemePreset>
        {
            // Close to stock: mostly solid, modest rounding, system accent territory.
            new("Classic",   Color.FromRgb(0x4C, 0xC2, 0xFF), 100, 8),

            // Softly translucent neutral - the default liquid-glass look.
            new("Frost",     Color.FromRgb(0x9F, 0xC4, 0xFF),  80, 12),

            // Deep and cool, heavily rounded, pinned dark.
            new("Midnight",  Color.FromRgb(0x5A, 0x7C, 0xFF),  72, 16, ElementTheme.Dark),

            // Green-teal, the most see-through of the set.
            new("Aurora",    Color.FromRgb(0x36, 0xE0, 0xB0),  66, 14, ElementTheme.Dark),

            // Warm amber against a solid backing, so the accent carries the look.
            new("Ember",     Color.FromRgb(0xFF, 0x8A, 0x3D),  86, 10, ElementTheme.Dark),

            // Muted blue-grey, restrained and flat.
            new("Nord",      Color.FromRgb(0x88, 0xC0, 0xD0),  90,  6, ElementTheme.Dark),

            // Pink, light, and very round.
            new("Rose",      Color.FromRgb(0xFF, 0x7A, 0xA8),  82, 18, ElementTheme.Light),

            // Purple-cyan, glassy and round.
            new("Vapor",     Color.FromRgb(0xB0, 0x7F, 0xFF),  70, 20, ElementTheme.Dark),

            // No colour at all: a deliberately plain, square, opaque look.
            new("Mono",      Color.FromRgb(0xC8, 0xC8, 0xC8), 100,  2)
        };
    }
}
