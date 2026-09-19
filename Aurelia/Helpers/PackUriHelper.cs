using System;

namespace Aurelia.Helpers
{
    internal static class PackUriHelper
    {
        public static Uri GetAbsoluteUri(string path)
        {
            return new Uri($"pack://application:,,,/Aurelia;component/{path}");
        }
    }
}
