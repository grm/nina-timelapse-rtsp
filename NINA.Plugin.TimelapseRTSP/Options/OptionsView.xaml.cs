using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.Plugin.TimelapseRTSP.Options {

    [Export(typeof(ResourceDictionary))]
    public partial class OptionsView : ResourceDictionary {
        public OptionsView() {
            InitializeComponent();
        }
    }
}
