using CommunityToolkit.Mvvm.ComponentModel;
using Aurelia.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Windows;
using static Aurelia.Core.Interop.NativeMethods;

namespace Aurelia.Core.Display
{
    public class BrightnessManager : ObservableObject
    {
        private BrightnessWatcher brightnessWatcher;
        private byte[] validWMIBrightnessLevels;

        public bool HasInitialized { get; private set; } = false;

        public BrightnessController DefaultBrightnessController { get; private set; }

        public static BrightnessManager Instance { get; private set; }

        public ObservableCollection<BrightnessController> BrightnessControllers { get; } = new();

        /// <summary>
        /// True while a sync is propagating, so the resulting changes on the other controllers
        /// don't bounce back and start their own propagation.
        /// </summary>
        private bool isSyncing;

        private bool isSyncEnabled = true;

        /// <summary>
        /// Moves every display's brightness together. Defaults on, which is what you want on a
        /// dual-screen laptop where the two panels are really one surface; turn it off to set each
        /// display independently.
        /// </summary>
        public bool IsSyncEnabled
        {
            get => isSyncEnabled;
            set
            {
                if (SetProperty(ref isSyncEnabled, value))
                {
                    SyncEnabledChanged?.Invoke(this, EventArgs.Empty);

                    if (value)
                    {
                        // Re-linking should bring the others to the display the user last touched,
                        // rather than leaving them wherever they drifted while unlinked.
                        SyncFrom(lastChanged ?? BrightnessControllers.FirstOrDefault());
                    }
                }
            }
        }

        /// <summary>Raised when <see cref="IsSyncEnabled"/> changes, so callers can persist it.</summary>
        public event EventHandler SyncEnabledChanged;

        /// <summary>Only meaningful when there is more than one display to link.</summary>
        public bool CanSync => BrightnessControllers.Count > 1;

        private BrightnessController lastChanged;

        private void AttachSync(BrightnessController controller)
        {
            controller.PropertyChanged += Controller_PropertyChanged;
        }

        private void DetachSync(BrightnessController controller)
        {
            controller.PropertyChanged -= Controller_PropertyChanged;
        }

        private void Controller_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(BrightnessController.Brightness) || sender is not BrightnessController source)
            {
                return;
            }

            lastChanged = source;

            if (isSyncEnabled && !isSyncing)
            {
                SyncFrom(source);
            }
        }

        /// <summary>
        /// Copies <paramref name="source"/>'s level onto every other display, as a proportion of
        /// each one's own range - panels don't necessarily share the same scale.
        /// </summary>
        private void SyncFrom(BrightnessController source)
        {
            if (source == null || isSyncing)
            {
                return;
            }

            double sourceRange = source.Maximum - source.Minimum;

            if (sourceRange <= 0)
            {
                return;
            }

            double fraction = (source.Brightness - source.Minimum) / sourceRange;

            isSyncing = true;

            try
            {
                foreach (BrightnessController other in BrightnessControllers)
                {
                    if (ReferenceEquals(other, source))
                    {
                        continue;
                    }

                    double target = other.Minimum + (fraction * (other.Maximum - other.Minimum));

                    if (Math.Abs(other.Brightness - target) > 0.5)
                    {
                        other.Brightness = target;
                    }
                }
            }
            finally
            {
                isSyncing = false;
            }
        }

        private BrightnessManager()
        {
            // Hook the collection once rather than every Add/Remove site, so newly detected
            // displays are wired into brightness sync automatically.
            BrightnessControllers.CollectionChanged += (_, e) =>
            {
                if (e.OldItems != null)
                {
                    foreach (BrightnessController c in e.OldItems)
                    {
                        DetachSync(c);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (BrightnessController c in e.NewItems)
                    {
                        AttachSync(c);
                    }
                }

                OnPropertyChanged(nameof(CanSync));
            };

            validWMIBrightnessLevels = GetValidWMIBrightnessLevels();

            if (AreWMIMethodsSupported())
            {
                brightnessWatcher = new();
                brightnessWatcher.Changed += BrightnessWatcher_Changed;
                brightnessWatcher.Start();
            }

            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
            //BrightnessControllers.Add(new MockBrightnessController());
        }

        public static void Initialize()
        {
            Instance ??= new();

            Instance.InitializeImpl();
        }

        private void InitializeImpl()
        {
            if (!HasInitialized)
            {
                HasInitialized = true;

                foreach (var displayMonitor in DisplayManager.Instance.DisplayMonitors)
                {
                    MakeBrightnessControllersForDisplayMonitor(displayMonitor);
                }
            }
        }

        public static void Suspend()
        {
            if (Instance == null)
                return;

            foreach (BrightnessController brightnessController in Instance.BrightnessControllers)
            {
                if (brightnessController is not BuiltInDisplayBrightnessController)
                    brightnessController.Dispose();
            }
            Instance.BrightnessControllers.Clear();

            if (Instance.brightnessWatcher != null)
            {
                Instance.brightnessWatcher.Stop();
            }

            Instance.HasInitialized = false;
        }

        private void MakeDefaultBrightnessController(DisplayMonitor displayMonitor)
        {
            if (AreWMIMethodsSupported())
            {
                DefaultBrightnessController ??= new BuiltInDisplayBrightnessController(Instance.validWMIBrightnessLevels);
                DefaultBrightnessController.AssociatedDisplayMonitor = displayMonitor;
                if (!BrightnessControllers.Contains(DefaultBrightnessController))
                {
                    BrightnessControllers.Add(DefaultBrightnessController);
                }
            }
        }

        /// <summary>
        /// Checks that a monitor really honours brightness writes, by nudging it one step and
        /// reading the value back.
        /// </summary>
        /// <remarks>
        /// <c>SetMonitorBrightness</c>'s return value cannot be trusted. Some panels - the second
        /// display on an ASUS Zenbook Duo among them - report success for every write and then
        /// ignore it. That produced a slider which moved, reported no error, and changed nothing.
        ///
        /// The probe moves brightness by a single step and restores it, which is imperceptible,
        /// and on a panel that ignores writes nothing changes at all.
        /// </remarks>
        private static bool CanActuallySetBrightness(IntPtr hPhysicalMonitor, uint minValue, uint currentValue, uint maxValue)
        {
            if (maxValue <= minValue)
            {
                return false;
            }

            uint probeValue = currentValue >= maxValue ? currentValue - 1 : currentValue + 1;

            if (!SetMonitorBrightness(hPhysicalMonitor, probeValue))
            {
                return false;
            }

            uint min = 0, readBack = 0, max = 0;
            bool read = GetMonitorBrightness(hPhysicalMonitor, ref min, ref readBack, ref max);

            // Put it back however the probe went.
            SetMonitorBrightness(hPhysicalMonitor, currentValue);

            return read && readBack == probeValue;
        }

        private static BrightnessController[] CreateBrightnessControllersForDisplayMonitor(DisplayMonitor displayMonitor)
        {
            List<BrightnessController> brightnessControllers = new();

            uint physicalMonitorsCount = 0;
            if (!GetNumberOfPhysicalMonitorsFromHMONITOR(displayMonitor.HMonitor, ref physicalMonitorsCount))
            {
                return null;
            }

            var physicalMonitors = new PHYSICAL_MONITOR[physicalMonitorsCount];
            if (!GetPhysicalMonitorsFromHMONITOR(displayMonitor.HMonitor, physicalMonitorsCount, physicalMonitors))
            {
                return null;
            }

            foreach (var physicalMonitor in physicalMonitors)
            {
                uint minValue = 0, currentValue = 0, maxValue = 0;
                if (!GetMonitorBrightness(physicalMonitor.hPhysicalMonitor, ref minValue, ref currentValue, ref maxValue))
                {
                    DestroyPhysicalMonitor(physicalMonitor.hPhysicalMonitor);
                    continue;
                }

                if (!CanActuallySetBrightness(physicalMonitor.hPhysicalMonitor, minValue, currentValue, maxValue))
                {
                    DestroyPhysicalMonitor(physicalMonitor.hPhysicalMonitor);
                    continue;
                }

                MonitorInfo info = new()
                {
                    Handle = physicalMonitor.hPhysicalMonitor,
                    MinValue = minValue,
                    CurrentValue = currentValue,
                    MaxValue = maxValue,
                };

                brightnessControllers.Add(new ExternalDisplayBrightnessController(info, displayMonitor));
            }

            if (brightnessControllers.Count == 0)
                return null;

            return brightnessControllers.ToArray();
        }

        internal static void DisposeBrightnessControllerForDisplayMonitor(DisplayMonitor displayMonitor)
        {
            if (Instance == null || !Instance.HasInitialized)
                return;

            var brightnessControllers = Instance.BrightnessControllers.Where(
                x => x is ExternalDisplayBrightnessController ex && ex.AssociatedDisplayMonitor == displayMonitor).ToList();

            if (brightnessControllers != null)
            {
                foreach (var brightnessController in brightnessControllers)
                {
                    brightnessController.Dispose();
                    Instance.BrightnessControllers.Remove(brightnessController);
                }
            }
        }

        internal static void MakeBrightnessControllersForDisplayMonitor(DisplayMonitor displayMonitor)
        {
            if (Instance == null || !Instance.HasInitialized)
                return;

            if (displayMonitor.IsInBuilt)
            {
                Instance.MakeDefaultBrightnessController(displayMonitor);
                return;
            }

            DisposeBrightnessControllerForDisplayMonitor(displayMonitor);

            var brightnessControllers = CreateBrightnessControllersForDisplayMonitor(displayMonitor);

            if (brightnessControllers != null)
            {
                foreach (var brightnessController in brightnessControllers)
                {
                    Instance.BrightnessControllers.Add(brightnessController);
                }
            }
        }

        private bool AreWMIMethodsSupported()
        {
            return validWMIBrightnessLevels.Length > 0;
        }

        private byte[] GetValidWMIBrightnessLevels()
        {
            byte[] brightnessLevels = Array.Empty<byte>();

            try
            {
                var s = new ManagementScope("root\\WMI");
                var q = new SelectQuery("WmiMonitorBrightness");
                using var mos = new ManagementObjectSearcher(s, q);
                using var moc = mos.Get();

                foreach (ManagementObject managementObject in moc)
                {
                    brightnessLevels = (byte[])managementObject.GetPropertyValue("Level");
                    break;
                }
            }
            catch { }

            return brightnessLevels;
        }

        private void BrightnessWatcher_Changed(object sender, BrightnessChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (DefaultBrightnessController != null)
                    DefaultBrightnessController.UpdateBrightness(e.NewValue);
            });
        }
    }
}
