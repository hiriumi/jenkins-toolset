using JenkinsToolsetWpf.Properties;

namespace JenkinsToolsetWpf.Forms.SettingsPages
{
    /// <summary>
    ///     Interaction logic for ExternalTools.xaml
    /// </summary>
    public partial class ExternalTools : BaseSettingsPage
    {
        public ExternalTools()
        {
            InitializeComponent();
        }

        public override void SaveSettings()
        {
            Settings.Default.DiffExePath = diffToolPath.DialogueTextResult;
            Settings.Default.CompareToolSwitches = txtSwitches.Text;
            Settings.Default.TextEditorExePath = ctlBrowseForTextEditor.DialogueTextResult;
        }
    }
}