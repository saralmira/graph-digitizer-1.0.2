using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace GraphDigitizer.Views
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : Window
    {
        public Help()
        {
            this.InitializeComponent();
            switch (App.Lang)
            {
                case "zh-CN":
                    this.LoadPage(Properties.Resources.HelpGeneral_zh);
                    break;
                default:
                    this.LoadPage(Properties.Resources.HelpGeneral);
                    break;
            }
        }

        private void OnGeneralTabGotFocus(object sender, RoutedEventArgs e)
        {
            switch (App.Lang)
            {
                case "zh-CN":
                    this.LoadPage(Properties.Resources.HelpGeneral_zh);
                    break;
                default:
                    this.LoadPage(Properties.Resources.HelpGeneral);
                    break;
            }
        }

        private void OnKeysTabGotFocus(object sender, RoutedEventArgs e)
        {
            switch (App.Lang)
            {
                case "zh-CN":
                    this.LoadPage(Properties.Resources.HelpKeys_zh);
                    break;
                default:
                    this.LoadPage(Properties.Resources.HelpKeys);
                    break;
            }
        }

        private void OnAboutTabGotFocus(object sender, RoutedEventArgs e)
        {
            this.LoadPage(Properties.Resources.HelpAbout);
        }

        private void LoadPage(string rtf)
        {
            var tr = new TextRange(this.ContentEdit.Document.ContentStart, this.ContentEdit.Document.ContentEnd);
            tr.Load(new System.IO.MemoryStream(Encoding.Default.GetBytes(rtf)), DataFormats.Rtf);
        }
    }
}
