using System.Windows;
using System.Windows.Controls;

namespace JenkinsToolsetWpf.Forms.SettingsPages
{
    public class BaseSettingsPage : UserControl
    {
        public BaseSettingsPage()
        {

        }

        public static readonly DependencyProperty PageIconProperty =
            DependencyProperty.Register("PageIcon", typeof(Image), typeof(BaseSettingsPage));

        public virtual Image PageIcon
        {
            get { return (Image) GetValue(PageIconProperty); }
            set { SetValue(PageIconProperty, value); }
        }

        public virtual void SaveSettings()
        { }
    }
}