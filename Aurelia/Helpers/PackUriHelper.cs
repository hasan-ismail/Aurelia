using System;

namespace Aurelia.Helpers
{
    internal static class PackUriHelper
    {
        private static readonly string AssemblyName =
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        public static Uri GetAbsoluteUri(string path)
        {
            // The assembly name is read at runtime rather than hardcoded. It used to say
            // "Aurelia", which silently became wrong the moment the assembly was renamed - every
            // theme dictionary then failed to load and the app died at startup.
            return new Uri($"pack://application:,,,/{AssemblyName};component/{path}");
        }
    }
}
