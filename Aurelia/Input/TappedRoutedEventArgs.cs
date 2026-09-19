using System.Windows;

namespace Aurelia.Input
{
    internal sealed class TappedRoutedEventArgs : RoutedEventArgs
    {
        public TappedRoutedEventArgs()
        {
        }

        internal int Timestamp { get; set; }
    }
}
