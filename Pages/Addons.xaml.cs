using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MinecraftUpdater.Pages
{
    class AddonInfo
    {
        public string name;
        public string descr;
        public string link;
        public string install;
        public bool isAvailable = false;
        public bool isInstall = false;
    }

    /// <summary>
    /// Логика взаимодействия для Addons.xaml
    /// </summary>
    public partial class Addons : Page
    {
        List<AddonInfo> addonsInfoList;
        private string fileLink = "";

        public Addons(string host)
        {
            InitializeComponent();
            addonsInfoList = new List<AddonInfo>();
            addonsInfoList.Clear();
            fileLink = host;
            PrepareAddonsLists();
        }

        public async void PrepareAddonsLists()
        {
            addonsInfoList.Clear();
            await Task.Run(() =>
            {
                addonsInfoList.AddRange(GetAddonsInfo(fileLink));
            });
            UpdateAddonsList();
            return;
        }

        private void UpdateAddonsList()
        {
            int selectedIndex = AddonsListBox.SelectedIndex;
            AddonsListBox.Items.Clear();
            string gamePath = Properties.Settings.Default.gamePath;
            for(int i = 0; i < addonsInfoList.Count; i++)
            {
                AddonInfo addonInfo = addonsInfoList[i];
                addonInfo.isAvailable = File.Exists(Path.Combine(gamePath, addonInfo.link));
                addonInfo.isInstall = File.Exists(Path.Combine(gamePath, addonInfo.install));

                ListBoxItem item = new ListBoxItem();
                item.Content = addonInfo.name;
                item.IsEnabled = addonInfo.isAvailable || addonInfo.isInstall;
                item.Background = addonInfo.isInstall ? Brushes.DarkSeaGreen : Brushes.Transparent;
                AddonsListBox.Items.Add(item);
            }
            if(AddonsListBox.Items.Count > 0)
            {
                AddonsListBox.SelectedIndex = selectedIndex > -1 ? selectedIndex : 0;
            }
        }

        private List<AddonInfo> GetAddonsInfo(string hostLink)
        {
            string addonsInfoFilePath = Path.Combine(Properties.Settings.Default.gamePath, "addonsInfo.json");
            string addonsInfoJSON;
            List<AddonInfo> result = new List<AddonInfo>();
            string addonsInfoJsonLink = hostLink + "/addonsInfo.json";

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(addonsInfoJsonLink, addonsInfoFilePath);
            } catch (Exception e)
            {
                result.Add(new AddonInfo
                {
                    name = "Ошибка получения",
                    descr = e.Message + "\n" + addonsInfoJsonLink,
                    link = "",
                    install = ""
                });
            }
            
            if (File.Exists(addonsInfoFilePath))
            {
                addonsInfoJSON = File.ReadAllText(addonsInfoFilePath);
                result.AddRange(JsonConvert.DeserializeObject<List<AddonInfo>>(addonsInfoJSON));
            }

            return result;
        }

        private void AddonsListRefresh_Click(object sender, RoutedEventArgs e)
        {
            PrepareAddonsLists();
        }

        private void AddonsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = AddonsListBox.SelectedIndex;
            AddonDescrTextBox.Text = "";
            InstallAddonButton.IsEnabled = false;
            RemoveAddonButton.IsEnabled = false;
            if (index > -1)
            {
                AddonInfo addonInfo = addonsInfoList[index];
                AddonDescrTextBox.Text = addonInfo.name + "\n-------------------\n" + addonInfo.descr;
                InstallAddonButton.IsEnabled = addonInfo.isAvailable && !addonInfo.isInstall;
                RemoveAddonButton.IsEnabled = addonInfo.isInstall;

            }
        }

        private async void InstallAddonButton_Click(object sender, RoutedEventArgs e)
        {
            int index = AddonsListBox.SelectedIndex;
            if(index == -1) { return; }
            string gamePath = Properties.Settings.Default.gamePath;
            AddonInfo addonInfo = addonsInfoList[index];
            if(!File.Exists(Path.Combine(gamePath, addonInfo.link)))
            {
                PrepareAddonsLists();
                return;
            }
            await Task.Run(() =>
            {
                string installPath = Path.Combine(gamePath, addonInfo.install);
                DirectoryInfo folder = Directory.GetParent(installPath);
                if(!folder.Exists)
                {
                    Directory.CreateDirectory(folder.FullName);
                }
                File.Copy(Path.Combine(gamePath, addonInfo.link), installPath, true);
            });
            PrepareAddonsLists();
            return;
        }

        private async void RemoveAddonButton_Click(object sender, RoutedEventArgs e)
        {
            int index = AddonsListBox.SelectedIndex;
            if (index == -1) { return; }
            string gamePath = Properties.Settings.Default.gamePath;
            AddonInfo addonInfo = addonsInfoList[index];
            await Task.Run(() =>
            {
                File.Delete(Path.Combine(gamePath, addonInfo.install));
            });
            PrepareAddonsLists();
            return;
        }

        private void RemoveAddonButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Button removeAddonButton = (Button)sender;
            removeAddonButton.Background = removeAddonButton.IsEnabled 
                ? (Brush)new BrushConverter().ConvertFromString("#fff3ed")
                : (Brush)new BrushConverter().ConvertFromString("#f8f9fa");
        }

        private void InstallAddonButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Button installAddonButton = (Button)sender;
            installAddonButton.Background = installAddonButton.IsEnabled
                ? (Brush)new BrushConverter().ConvertFromString("#deffec")
                : (Brush)new BrushConverter().ConvertFromString("#f8f9fa");
        }
    }
}
