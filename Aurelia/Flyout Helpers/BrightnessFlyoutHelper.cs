using Aurelia.Controls;
using Aurelia.Core.Display;
using Aurelia.Helpers;

namespace Aurelia
{
    public class BrightnessFlyoutHelper : FlyoutHelperBase
    {
        private BrightnessControl brightnessControl;

        public BrightnessFlyoutHelper()
        {
            Initialize();
        }

        public void Initialize()
        {
            AlwaysHandleDefaultFlyout = true;

            BrightnessManager.Initialize();

            // Restore the user's link/unlink choice, and keep it saved as they toggle it from the
            // lock button in the flyout.
            BrightnessManager.Instance.IsSyncEnabled = AppDataHelper.BrightnessSyncEnabled;
            BrightnessManager.Instance.SyncEnabledChanged += (_, _) =>
                AppDataHelper.BrightnessSyncEnabled = BrightnessManager.Instance.IsSyncEnabled;

            brightnessControl = new BrightnessControl();

            PrimaryContent = brightnessControl;

            OnEnabled();
        }

        public override bool CanHandleNativeOnScreenFlyout(FlyoutTriggerData triggerData)
        {
            if (triggerData.TriggerType == FlyoutTriggerType.Brightness)
                return true;

            return base.CanHandleNativeOnScreenFlyout(triggerData);
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            AppDataHelper.BrightnessModuleEnabled = IsEnabled;

            BrightnessManager.Initialize();
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            BrightnessManager.Suspend();

            AppDataHelper.BrightnessModuleEnabled = IsEnabled;
        }
    }
}
