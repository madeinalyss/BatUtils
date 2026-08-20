using System.Windows.Controls;
using BatUtils.Services;

namespace BatUtils.Views
{
    public partial class InfoView : UserControl
    {
        public InfoView()
        {
            InitializeComponent();

            PopulateSystemInfo();
        }

        private void PopulateSystemInfo()
        {
            OsValueText.Text = AppInfo.OperatingSystem;

            ArchValueText.Text = AppInfo.Architecture;

            DotNetValueText.Text = AppInfo.RuntimeVersion;

            AppVersionValueText.Text = AppInfo.Version;
        }
    }
}
