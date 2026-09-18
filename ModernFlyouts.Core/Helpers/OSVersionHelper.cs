using System;

namespace ModernFlyouts.Core.Helpers
{
    /// <summary>
    /// Single source of truth for the Windows build number the app is running on.
    /// </summary>
    /// <remarks>
    /// This used to be derived by taking everything after the last '.' of
    /// <see cref="System.Runtime.InteropServices.RuntimeInformation.OSDescription"/> and calling
    /// <c>int.Parse</c> on it. That is wrong in two ways: when the description carries a revision
    /// (e.g. "Microsoft Windows 10.0.26100.1742") the parsed value is the revision rather than the
    /// build, which silently selects the wrong OSD layout; and any unexpected format throws, which
    /// surfaces as a crash while the flyout handler is initialising.
    ///
    /// <see cref="Environment.OSVersion"/> on .NET (Core) is backed by RtlGetVersion, so it reports the
    /// real build and is not affected by the application compatibility manifest.
    /// </remarks>
    public static class OSVersionHelper
    {
        /// <summary>The Windows build number, e.g. 19045, 22621, 26100.</summary>
        public static int Build { get; } = Environment.OSVersion.Version.Build;

        /// <summary>True on Windows 11 (build 22000) or newer.</summary>
        public static bool IsWindows11OrGreater => Build >= 22000;
    }
}
