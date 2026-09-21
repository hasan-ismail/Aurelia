using Aurelia.AppLifecycle;
using Aurelia.Core.Interop;
using Aurelia.Helpers;
using System;
using System.Reflection;
using System.Threading;

namespace Aurelia
{
    public class Program
    {
        public const string AppName = "Aurelia";
        /// <summary>
        /// Filename of the running executable, without extension. Derived rather than hardcoded so
        /// it cannot go stale if the assembly is renamed.
        /// </summary>
        public static string AppHostName { get; } =
            System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "AureliaFlyouts";

        [STAThread]
        private static void Main(string[] args)
        {
            Thread thread = new(() => {
                AppLifecycleManager.StartApplication(args, () =>
                {
                    // Visual Studio App Center, which release builds used to report analytics and
                    // crashes to, was retired by Microsoft on 31 March 2025. Starting it only cost a
                    // failed network round-trip on every launch, so the SDK has been removed entirely.
                    // PrepareToDie covers the orderly exits; this catches the rest so a
                    // debounced settings write is never silently dropped.
                    AppDomain.CurrentDomain.ProcessExit += (_, _) => Helpers.SettingsStore.Flush();

                    InitializePrivateUseClasses();

                    AppDataMigration.Perform();

                    NativeFlyoutHandler.Instance = new NativeFlyoutHandler();
                    NativeFlyoutHandler.Instance.Initialize();

                    LocalizationHelper.Initialize();

                    var app = new App();
                    app.Run();
                });
            });

            //If you lauch directly from the host bridge it won't be STA.
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        internal static void RunCommand(RunCommandType runCommandType)
        {
            switch (runCommandType)
            {
                case RunCommandType.ShowSettings:
                    {
                        if (FlyoutHandler.HasInitialized)
                        {
                            FlyoutHandler.ShowSettingsWindow();
                        }
                        else
                        {
                            FlyoutHandler.Initialized += (_, __) => FlyoutHandler.ShowSettingsWindow();
                        }
                        break;
                    }
                case RunCommandType.RestoreDefault:
                    {
                        NativeFlyoutHandler.Instance.VerifyNativeFlyoutCreated();
                        FlyoutHandler.SafelyExitApplication();
                        break;
                    }
                case RunCommandType.SafeExit:
                    {
                        FlyoutHandler.SafelyExitApplication();
                        break;
                    }
                case RunCommandType.AppUpdated:
                    {
                        //if (AppLifecycleManager.IsBuildBetaChannel)
                        //{
                        //    MessageBox.Show("App update successfully!", AppName);
                        //}

                        break;
                    }
                default:
                    break;
            }
        }

        public static string AppVersion
        {
            get => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        /// <summary>
        /// The copyright line shown in About. Read from the assembly so it stays in step with the
        /// Copyright property in Directory.Build.props rather than being duplicated here.
        /// </summary>
        public static string Copyright
        {
            get => Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        }

        internal static void InitializePrivateUseClasses()
        {
#if Screenshots
            FlyoutHandler.Initialized += (_, __) => Private.ScreenshotHelper.Initialize();
#endif
        }
    }

    internal enum RunCommandType
    {
        ShowSettings = 0,
        RestoreDefault = 1,
        SafeExit = 2,
        AppUpdated = 3
    }
}
