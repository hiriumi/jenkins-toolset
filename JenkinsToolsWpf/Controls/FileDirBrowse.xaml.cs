using System.Windows;
using System.Windows.Forms;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace JenkinsToolsetWpf.Controls
{
    /// <summary>
    ///     Interaction logic for FileDirBrowse.xaml
    /// </summary>
    public partial class FileDirBrowse : UserControl
    {
        public enum BrowseDialogueType
        {
            File,
            Directory
        }

        public static readonly DependencyProperty BrowseDialogueTypeProperty =
            DependencyProperty.Register("BrowseDialogueType",
                typeof(BrowseDialogueType), typeof(FileDirBrowse));

        public static readonly DependencyProperty DialogueTextResultProperty =
            DependencyProperty.Register("DialogueTextResult",
                typeof(string), typeof(FileDirBrowse));

        public static readonly DependencyProperty FileFilterProperty = DependencyProperty.Register("FileFilter",
            typeof(string), typeof(FileDirBrowse));

        public static readonly DependencyProperty DefaultExtProperty = DependencyProperty.Register("DefaultExt",
            typeof(string), typeof(FileDirBrowse));

        public static readonly DependencyProperty DialogueTitleProperty = DependencyProperty.Register(
            "DialogueTitle", typeof(string), typeof(FileDirBrowse));


        public static readonly DependencyProperty DefaultDirPathProperty = DependencyProperty.Register(
            "DefaultDirPath", typeof(string), typeof(FileDirBrowse), new PropertyMetadata(default(string)));

        public FileDirBrowse()
        {
            InitializeComponent();
        }

        public string DefaultDirPath
        {
            get { return (string) GetValue(DefaultDirPathProperty); }
            set { SetValue(DefaultDirPathProperty, value); }
        }

        public BrowseDialogueType BrowseType
        {
            get { return (BrowseDialogueType) GetValue(BrowseDialogueTypeProperty); }
            set { SetValue(BrowseDialogueTypeProperty, value); }
        }

        public string DialogueTextResult
        {
            get { return (string) GetValue(DialogueTextResultProperty); }
            set { SetValue(DialogueTextResultProperty, value); }
        }

        public string FileFilter
        {
            get { return (string) GetValue(FileFilterProperty); }
            set { SetValue(FileFilterProperty, value); }
        }

        public string DefaultExt
        {
            get { return (string) GetValue(DefaultExtProperty); }
            set { SetValue(DefaultExtProperty, value); }
        }

        public string DialogueTitle
        {
            get { return (string) GetValue(DialogueTitleProperty); }
            set { SetValue(DialogueTitleProperty, value); }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            switch (BrowseType)
            {
                case BrowseDialogueType.File:
                    var fileBrowser = new OpenFileDialog();
                    fileBrowser.DefaultExt = DefaultExt;
                    fileBrowser.Filter = FileFilter;
                    fileBrowser.Title = DialogueTitle;
                    if ((bool) fileBrowser.ShowDialog())
                    {
                        DialogueTextResult = fileBrowser.FileName;
                    }
                    break;

                case BrowseDialogueType.Directory:
                    var dirBrowser = new FolderBrowserDialog();
                    dirBrowser.Description = DialogueTitle;
                    dirBrowser.ShowNewFolderButton = true;
                    dirBrowser.SelectedPath = DefaultDirPath;
                    if (dirBrowser.ShowDialog() == DialogResult.OK)
                    {
                        DialogueTextResult = dirBrowser.SelectedPath;
                    }
                    break;
            }
        }

        private void txtFilePath_OnGotMouseCapture(object sender, MouseEventArgs e)
        {
            txtFilePath.Focus();
            txtFilePath.SelectAll();
        }
    }
}