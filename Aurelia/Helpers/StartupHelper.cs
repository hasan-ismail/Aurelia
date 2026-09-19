using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace Aurelia.Helpers
{
    /// <summary>
    /// Controls whether the app launches when the user signs in.
    /// </summary>
    /// <remarks>
    /// There are two mechanisms, because the app ships in two shapes:
    ///
    /// <list type="bullet">
    /// <item>Packaged (MSIX): <see cref="StartupTask"/>, which is what the Store build has always used.
    /// It cannot be used outside a package - the APIs throw.</item>
    /// <item>Unpackaged (the installer and portable builds): the per-user <c>Run</c> registry key.</item>
    /// </list>
    ///
    /// Previously only the packaged path existed and every failure was swallowed, so in an unpackaged
    /// build the toggle did nothing at all and the getter's <c>catch { return true; }</c> reported it
    /// as enabled regardless. That made the setting look like it wasn't being saved.
    /// </remarks>
    internal class StartupHelper
    {
        private const string StartupId = "AureliaStartupId";

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private const string RunValueName = "Aurelia";

        private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

        private static readonly Lazy<bool> isPackaged = new(() =>
        {
            try
            {
                int length = 0;
                return GetCurrentPackageFullName(ref length, null) != APPMODEL_ERROR_NO_PACKAGE;
            }
            catch (EntryPointNotFoundException)
            {
                // The API is missing on very old Windows versions, which can only be unpackaged anyway.
                return false;
            }
        });

        /// <summary>Whether the app is running from inside an MSIX package.</summary>
        public static bool IsPackaged => isPackaged.Value;

        public static async Task<bool> GetRunAtStartupEnabled()
        {
            if (!IsPackaged)
            {
                return GetRunKeyEnabled();
            }

            try
            {
                StartupTask startupTask = await StartupTask.GetAsync(StartupId);

                return startupTask.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;
            }
            catch
            {
                return false;
            }
        }

        public static async void SetRunAtStartupEnabled(bool value)
        {
            if (!IsPackaged)
            {
                SetRunKeyEnabled(value);
                return;
            }

            try
            {
                StartupTask startupTask = await StartupTask.GetAsync(StartupId);

                if (value)
                {
                    await startupTask.RequestEnableAsync();
                }
                else
                {
                    startupTask.Disable();
                }
            }
            catch { }
        }

        private static bool GetRunKeyEnabled()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);

                return key?.GetValue(RunValueName) is string value && value.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void SetRunKeyEnabled(bool value)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                    ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

                if (key == null)
                {
                    return;
                }

                if (value)
                {
                    string path = Environment.ProcessPath;

                    if (!string.IsNullOrEmpty(path))
                    {
                        // Quoted so a path containing spaces (Program Files) still launches.
                        key.SetValue(RunValueName, $"\"{path}\"", RegistryValueKind.String);
                    }
                }
                else
                {
                    key.DeleteValue(RunValueName, throwOnMissingValue: false);
                }
            }
            catch { }
        }
    }
}
