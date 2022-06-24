using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MinecraftUpdater.Pages
{
    /// <summary>
    /// Логика взаимодействия для Log.xaml
    /// </summary>
    public partial class Log : Page
    {
        public Log(string host)
        {
            InitializeComponent();
            LoadChangelogFromServer($"http://{host}/updater/changelog.txt");
        }

        public async void LoadChangelogFromServer(string link)
        {
            Uri uri = new Uri(link);
            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;
            string text = "";
            try
            {
                text = await webClient.DownloadStringTaskAsync(uri);
            }
            catch (Exception error)
            {
                text = $"Невозможно загрузить информацию о обновлениях проекта\n{error.Message}";
            }
            LogTextBox.Text = text;
        }
    }
}
