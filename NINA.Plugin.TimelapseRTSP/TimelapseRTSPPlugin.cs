using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Plugin.TimelapseRTSP.Options;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace NINA.Plugin.TimelapseRTSP {

    [Export(typeof(IPluginManifest))]
    public class TimelapseRTSPPlugin : PluginBase {
        private readonly IPluginOptionsAccessor optionsAccessor;

        [ImportingConstructor]
        public TimelapseRTSPPlugin(IProfileService profileService) {
            if (TimelapseRTSPOptions.Instance == null) {
                optionsAccessor = new PluginOptionsAccessor(profileService, System.Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"));
                TimelapseRTSPOptions.Instance = new TimelapseRTSPOptions(optionsAccessor);
            }
        }

        public override Task Initialize() {
            return Task.CompletedTask;
        }

        public override Task Teardown() {
            return Task.CompletedTask;
        }
    }
}
