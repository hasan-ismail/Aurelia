using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Aurelia.Core.Display;
using Aurelia.UI;

namespace Aurelia.Controls
{
    public partial class BrightnessControl : UserControl
    {
        /// <summary>
        /// Width the link button occupies once visible: the button plus its margins. The sliders
        /// give up exactly this much so the two never overlap.
        /// </summary>
        private const double LinkButtonWidth = 42.0;

        public BrightnessControl()
        {
            InitializeComponent();

            if (BrightnessManager.Instance is BrightnessManager manager)
            {
                manager.PropertyChanged += Manager_PropertyChanged;
            }

            UpdateSliderAreaWidth();
        }

        #region SliderAreaWidth

        private static readonly DependencyPropertyKey SliderAreaWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(SliderAreaWidth), typeof(double), typeof(BrightnessControl),
                new PropertyMetadata(UIManager.FlyoutWidth));

        public static readonly DependencyProperty SliderAreaWidthProperty = SliderAreaWidthPropertyKey.DependencyProperty;

        /// <summary>The width left for the sliders once the link button has taken its column.</summary>
        public double SliderAreaWidth
        {
            get => (double)GetValue(SliderAreaWidthProperty);
            private set => SetValue(SliderAreaWidthPropertyKey, value);
        }

        #endregion

        private void Manager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BrightnessManager.CanSync))
            {
                Dispatcher.Invoke(UpdateSliderAreaWidth);
            }
        }

        private void UpdateSliderAreaWidth()
        {
            bool canSync = BrightnessManager.Instance?.CanSync == true;

            SliderAreaWidth = UIManager.FlyoutWidth - (canSync ? LinkButtonWidth : 0.0);
        }
    }
}
