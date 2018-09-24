using System.Windows;
using System.Windows.Controls;

namespace JenkinsToolsetWpf.Forms.SettingsPages
{
    public abstract class BaseSettingsPage : UserControl
    {
        protected BaseSettingsPage()
        {
            
        }

        public static readonly DependencyProperty PageIconProperty =
            DependencyProperty.Register("PageIcon", typeof(Image), typeof(BaseSettingsPage));

        public virtual Image PageIcon
        {
            get { return (Image) GetValue(PageIconProperty); }
            set { SetValue(PageIconProperty, value); }
        }

        public abstract void SaveSettings();
    }
}