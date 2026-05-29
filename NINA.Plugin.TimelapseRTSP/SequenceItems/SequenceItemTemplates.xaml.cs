using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.Plugin.TimelapseRTSP.SequenceItems {

    [Export(typeof(ResourceDictionary))]
    public partial class SequenceItemTemplates : ResourceDictionary {
        public SequenceItemTemplates() {
            InitializeComponent();
        }
    }
}
