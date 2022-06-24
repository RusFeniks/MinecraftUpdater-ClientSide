using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;
using System.Diagnostics;

namespace MinecraftUpdater.Pages
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        MainWindow mainWindow;

        public Settings(MainWindow _mainWindow)
        {
            InitializeComponent();
            mainWindow = _mainWindow;
        }

        public void UpdateContent()
        {
            RamTextBox.Text = Properties.Settings.Default.memory;
            GamePathLabel.Text = Properties.Settings.Default.gamePath;
            JavaPathTextBox.Text = Properties.Settings.Default.javaPath;
            LauncherAutoUpdateCheckBox.IsChecked = Properties.Settings.Default.autoUpdateLauncher;
            CustomStartParamsBox.Text = Properties.Settings.Default.customStartParams;
        }

        private void RamTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox ramTextBox = (TextBox)sender;
            if(int.TryParse(ramTextBox.Text, out _))
            {
                Properties.Settings.Default.memory = ramTextBox.Text;
                Properties.Settings.Default.Save();
                ramTextBox.BorderBrush = Brushes.Transparent;
            }
            else
            {
                ramTextBox.BorderBrush = Brushes.DarkRed;
            }
        }

        private void CustomStartParamsBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox customStartParamsTextBox = (TextBox)sender;
            Properties.Settings.Default.customStartParams = customStartParamsTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void SelectGamePathButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog selectGamePathFolderDialog = new FolderBrowserDialog();
            if (selectGamePathFolderDialog.ShowDialog() == DialogResult.OK)
            {
                string path = selectGamePathFolderDialog.SelectedPath;
                if (Directory.GetFiles(selectGamePathFolderDialog.SelectedPath).Length > 0)
                {
                    if(MessageBox.Show("В выбранной папке уже есть файлы. При установке обновлений, лаунчер может повредить их. Вы уверены, что хотите выбрать именно эту папку?", "Эта папка не пуста!", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                SetNewGamePath(path);
            }
        }

        private void ResetGamePathButton_Click(object sender, RoutedEventArgs e)
        {
            SetNewGamePath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".lostlands"));
        }

        private void SetNewGamePath(string path)
        {
            string oldPath = Properties.Settings.Default.gamePath;
            Properties.Settings.Default.gamePath = path;
            Properties.Settings.Default.Save();
            UpdateContent();
            if(oldPath != path)
            {
                mainWindow.CheckClientVersion();
            }
        }

        private void SelectJavaPathButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog selectGamePathFolderDialog = new FolderBrowserDialog();
            if (selectGamePathFolderDialog.ShowDialog() == DialogResult.OK)
            {
                string pathToJava = Path.Combine(selectGamePathFolderDialog.SelectedPath, "bin", "javaw.exe");
                if(!File.Exists(pathToJava))
                {
                    MessageBox.Show($"Не могу найти файл: {pathToJava}\nВыберите папку с установленной Java8");
                    return;
                }
                Properties.Settings.Default.javaPath = pathToJava;
                Properties.Settings.Default.Save();
            }
            UpdateContent();
        }

        private void ResetJavaPathButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.javaPath = "javaw";
            Properties.Settings.Default.Save();
            UpdateContent();
        }

        private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.CheckClientVersion();
        }

        private void LauncherAutoUpdateCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.autoUpdateLauncher = (bool)LauncherAutoUpdateCheckBox.IsChecked;
            Properties.Settings.Default.Save();
            UpdateContent();
        }

        private void OpenUserModsFolder_Click(object sender, RoutedEventArgs e)
        {
            string userModsFolder = Path.Combine(Properties.Settings.Default.gamePath, "UserMods");
            if(!Directory.Exists(userModsFolder))
            {
                Directory.CreateDirectory(userModsFolder);
            }

            Process.Start(userModsFolder);
        }
    }
}
