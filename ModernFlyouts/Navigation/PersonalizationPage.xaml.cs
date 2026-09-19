using System.Windows;
using System.Windows.Controls;
using ModernFlyouts.UI;

namespace ModernFlyouts.Navigation
{
    public partial class PersonalizationPage : Page
    {
        public PersonalizationPage()
        {
            InitializeComponent();
        }

        private void ThemePresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: FlyoutThemePreset preset })
            {
                FlyoutHandler.Instance?.UIManager?.ApplyThemePreset(preset);
            }
        }
    }
}
