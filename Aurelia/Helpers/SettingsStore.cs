using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Windows.Storage;

namespace Aurelia.Helpers
{
    /// <summary>
    /// Key/value backing store for the app's settings.
    /// </summary>
    /// <remarks>
    /// The app originally persisted everything through <see cref="ApplicationData.Current"/>, which
    /// only works inside an MSIX package. Unpackaged - which is how the installer and portable
    /// builds run - every one of those calls throws, and because the callers wrapped them in
    /// <c>catch { }</c> the failure was invisible: reads fell back to defaults and writes vanished,
    /// so settings appeared to reset on every launch.
    ///
    /// Packaged builds keep using <see cref="ApplicationData"/> so existing installs keep their
    /// settings. Unpackaged builds get a small JSON file next to the app's other local data.
    ///
    /// Every value is stored as a string, matching what the old code wrote.
    /// </remarks>
    internal static class SettingsStore
    {
        private static readonly object gate = new();

        private static readonly Lazy<string> filePath = new(() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Program.AppName,
            "settings.json"));

        private static Dictionary<string, string> cache;

        private static Timer saveTimer;

        /// <summary>Writes are coalesced: sliders fire a change per pixel of travel.</summary>
        private const int SaveDebounceMilliseconds = 400;

        private static bool UsePackagedStore => StartupHelper.IsPackaged;

        public static bool TryGetValue(string key, out string value)
        {
            if (UsePackagedStore)
            {
                try
                {
                    var values = ApplicationData.Current.LocalSettings.Values;

                    if (values.ContainsKey(key))
                    {
                        value = values[key]?.ToString() ?? string.Empty;
                        return true;
                    }
                }
                catch { }

                value = null;
                return false;
            }

            lock (gate)
            {
                return Load().TryGetValue(key, out value);
            }
        }

        public static void SetValue(string key, string value)
        {
            if (UsePackagedStore)
            {
                try
                {
                    ApplicationData.Current.LocalSettings.Values[key] = value;
                }
                catch { }

                return;
            }

            lock (gate)
            {
                Load()[key] = value ?? string.Empty;
                ScheduleSave();
            }
        }

        public static void Clear()
        {
            if (UsePackagedStore)
            {
                try
                {
                    ApplicationData.Current.LocalSettings.Values.Clear();
                }
                catch { }

                return;
            }

            lock (gate)
            {
                cache = new Dictionary<string, string>(StringComparer.Ordinal);
                ScheduleSave();
            }
        }

        /// <summary>
        /// Writes any pending changes immediately. Called on the way out so a debounced change
        /// isn't lost when the app exits.
        /// </summary>
        public static void Flush()
        {
            if (UsePackagedStore)
            {
                return;
            }

            lock (gate)
            {
                saveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                SaveLocked();
            }
        }

        private static Dictionary<string, string> Load()
        {
            if (cache != null)
            {
                return cache;
            }

            cache = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                string path = filePath.Value;

                if (File.Exists(path))
                {
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));

                    if (loaded != null)
                    {
                        foreach (var pair in loaded)
                        {
                            cache[pair.Key] = pair.Value;
                        }
                    }
                }
            }
            catch
            {
                // A corrupt or unreadable settings file must not stop the app starting; the user
                // gets defaults and the file is rewritten on the next change.
            }

            return cache;
        }

        private static void ScheduleSave()
        {
            saveTimer ??= new Timer(_ =>
            {
                lock (gate)
                {
                    SaveLocked();
                }
            }, null, Timeout.Infinite, Timeout.Infinite);

            saveTimer.Change(SaveDebounceMilliseconds, Timeout.Infinite);
        }

        private static void SaveLocked()
        {
            if (cache == null)
            {
                return;
            }

            try
            {
                string path = filePath.Value;
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                // Write to a temporary file and move it into place, so an interrupted write
                // can't leave a half-written settings file behind.
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(cache, new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temp, path, overwrite: true);
            }
            catch { }
        }
    }
}
