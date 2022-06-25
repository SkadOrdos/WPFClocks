using System.Reflection;
using System.Windows;

namespace WClocks
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            Localize();
        }

        private void Localize()
        {
            textAbout.Text = $"{WClocks.MainWindow.APP_NAME} v{Assembly.GetExecutingAssembly().GetName().Version}\n" +
                             $"Copyright \u00a9 2022 Serhii Vishnov";
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
