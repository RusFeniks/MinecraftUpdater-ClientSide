using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MinecraftUpdater
{

    public partial class MainWindow : Window
    {
        const string version = "2.3.0";

        const string HOST = "lost-lands.ru";
        const int SERVER_PORT = 8081;


        bool lockPlayButton = false;

        Page NewsPage;
        Pages.Settings SettingsPage;
        Pages.Addons AddonsPage;

        public MainWindow()
        {
            InitializeComponent();

            SetDefaultValuesToSettings();
            
            LauncherAutoUpdate();

            FillFieldsFromSavedData();

            NewsPage = new Pages.Log(HOST);
            SettingsPage = new Pages.Settings(this);
            AddonsPage = new Pages.Addons($"http://{HOST}:{SERVER_PORT}");

            CheckClientVersion();
            SetPlayAbility();
            ContentFrame.Content = NewsPage;
        }

        private void LauncherAutoUpdate()
        {

            if (!Properties.Settings.Default.autoUpdateLauncher)
            {
                return;
            }

            string updaterPath = Path.Combine(Path.GetTempPath(), "LostLandsUpdater.exe");
            string currentLauncherPath = Path.Combine(System.Reflection.Assembly.GetEntryAssembly().Location);

            if (File.Exists(updaterPath))
            {
                File.Delete(updaterPath);
            }

            try
            {
                WebClient webClient = new WebClient();
                string currentVersion = webClient.DownloadString($"http://{HOST}/updater/.version");

                if (currentVersion != version)
                {
                    webClient.DownloadFile($"http://{HOST}/updater/lostlandsupdater.exe", updaterPath);
                    Process.Start(updaterPath, currentLauncherPath.Replace(" ", "<s>"));
                    Environment.Exit(0);
                }

            } catch(Exception e)
            {
                MessageBox.Show("Ошибка при получении обновления лаунчера: " + e.Message);
                return;
            }
        }

        private void setPropetriesIfDoesntExist(string name, dynamic value)
        {
            if(Properties.Settings.Default[name] == null)
            {
                Properties.Settings.Default[name] = value;
                Properties.Settings.Default.Save();
                return;
            }
            Properties.Settings.Default[name] = Properties.Settings.Default[name].ToString().Length > 0 ?
                Properties.Settings.Default[name] : value;
            Properties.Settings.Default.Save();
        }

        private void SetDefaultValuesToSettings()
        {
            setPropetriesIfDoesntExist("gamePath", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".lostlands"));
            setPropetriesIfDoesntExist("javaPath", "javaw");
            setPropetriesIfDoesntExist("username", "User");
            setPropetriesIfDoesntExist("password", "");
            setPropetriesIfDoesntExist("memory", "2000");
            setPropetriesIfDoesntExist("clientVersion", "0");
            setPropetriesIfDoesntExist("autoUpdateLauncher", true);
            setPropetriesIfDoesntExist("customStartParams", "");
            return;
        }

        /// <summary>
        /// Проверяет соответствие версии локального клиента и версии клиента на сервере
        /// </summary>
        public async void CheckClientVersion()
        {
            UpdateProgressBar.IsIndeterminate = true;
            string localClientVersion = Properties.Settings.Default.clientVersion;

            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;
            Uri uri = new Uri($"http://{HOST}:{SERVER_PORT}/.client-version.json");
            
            try
            {
                string serverClientVersion = await webClient.DownloadStringTaskAsync(uri);
                UpdateButton.IsEnabled = localClientVersion != serverClientVersion;
            } catch (Exception error) {}

            if(!UpdateButton.IsEnabled)
            {
                string gamePath = Properties.Settings.Default.gamePath;
                ClientUpdater clientUpdater = new ClientUpdater($"http://{HOST}:{SERVER_PORT}", gamePath);
                UpdateButton.IsEnabled = await clientUpdater.UpdatesCheck();
            }

            UpdateButton.Background = UpdateButton.IsEnabled ? (Brush)new BrushConverter().ConvertFromString("#ffc107") : (Brush)new BrushConverter().ConvertFromString("#f8f9fa");
            UpdateProgressBar.IsIndeterminate = false;
            UpdateProgressBar.Value = UpdateButton.IsEnabled ? 0 : UpdateProgressBar.Maximum;
            SettingsPage.UpdateContent();
            AddonsPage.PrepareAddonsLists();
        }

        /// <summary>
        /// Автозаполнение полей, сохраненными данными
        /// </summary>
        private void FillFieldsFromSavedData()
        {
            LoginTextBox.Text = Properties.Settings.Default.username;
            PasswordTextBox.Password = Properties.Settings.Default.password;
        }

        /// <summary>
        /// Проверяет правильную заполненность поля "Логин"
        /// </summary>
        /// <returns></returns>
        private bool ValidateLoginField()
        {
            bool isValid = LoginTextBox.Text.Length > 0;
            LoginTextBox.BorderBrush = isValid ? Brushes.Transparent : Brushes.DarkRed;
            return isValid;
        }

        /// <summary>
        /// Проверяет правильную заполненность поля "Пароль"
        /// </summary>
        /// <returns></returns>
        private bool ValidatePasswordField()
        {
            bool isValid = PasswordTextBox.Password.Length > 0;
            PasswordTextBox.BorderBrush = isValid ? Brushes.Transparent : Brushes.DarkRed;
            return isValid;
        }

        /// <summary>
        /// Устанавливает кнопке "Играть" доступность, в зависимости от доступности клиента
        /// наличия обновлений и заполненности полей Логин и Пароль
        /// </summary>
        private void SetPlayAbility()
        {
            PlayButton.IsEnabled = ValidateLoginField() && ValidatePasswordField() && !lockPlayButton;
        }

        private void LoginTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.username = ((TextBox)sender).Text;
            Properties.Settings.Default.Save();
            SetPlayAbility();
        }

        private void PasswordTextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.password = ((PasswordBox)sender).Password;
            Properties.Settings.Default.Save();
            SetPlayAbility();
        }

        private void NewsPageButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Content = NewsPage;
        }

        private void AddonsPageButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Content = AddonsPage;
            AddonsPage.PrepareAddonsLists();
        }

        private void SettingsPageButton_Click(object sender, RoutedEventArgs e)
        {
            ContentFrame.Content = SettingsPage;
            SettingsPage.UpdateContent();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            string gamePath = Properties.Settings.Default.gamePath;
            string javaPath = Properties.Settings.Default.javaPath;
            int RAM = Convert.ToInt32(Properties.Settings.Default.memory);

            string login = Properties.Settings.Default.username;
            string password = Properties.Settings.Default.password;
            string customStartParams = Properties.Settings.Default.customStartParams;

            GameManager gameManager = new GameManager();
            gameManager.Run(gamePath, javaPath, RAM, login, password, customStartParams);
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.IsEnabled = false;
            PlayButton.IsEnabled = false;
            lockPlayButton = true;

            string gamePath = Properties.Settings.Default.gamePath;
            if(!Directory.Exists(gamePath))
            {
                Directory.CreateDirectory(gamePath);
            }
            ClientUpdater clientUpdater = new ClientUpdater($"http://{HOST}:{SERVER_PORT}", gamePath);
            await clientUpdater.Update(UpdateProgressBar);

            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;
            Uri uri = new Uri($"http://{HOST}:{SERVER_PORT}/.client-version.json");
            string serverClientVersion = await webClient.DownloadStringTaskAsync(uri);
            Properties.Settings.Default.clientVersion = serverClientVersion;
            Properties.Settings.Default.Save();

            lockPlayButton = false;
            CheckClientVersion();
            SetPlayAbility();

        }

        private void PasswordHelpLink_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start($"https://{HOST}/register");
        }
    }
}