using Microsoft.WindowsAPICodePack.Taskbar;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace MinecraftUpdater {

    public partial class MainWindow : Window {

        Settings settings;
        ClientUpdater clientUpdater;

        string host = "lost-islands.serveminecraft.net";
        string port = "8081";

        public MainWindow() {

            InitializeComponent();

            // Подгружаем настройки
            settings = new Settings();
            loadSettingsFromFile("./updaterConfig.json");
            
            // Заполняем поля формы данными из загруженных настроек
            UserName_TextBox.Text = this.settings.username;
            Ram_TextBox.Text = this.settings.ram.ToString();

            // Создаем новый апдейтер
            clientUpdater = new ClientUpdater(host, port, Log);
            
            // Вызываем проверку на обновления
            prepareUpdate();

        }

        private async void prepareUpdate()
        {
            Log.AppendText("Выполняется проверка на наличие обновлений\n");
            Log.ScrollToEnd();
            
            // Асинхронно проверяем обновления
            await clientUpdater.checkUpdates();

            // По окончании, включаем кнопки лаунчера
            RunGame_Button.IsEnabled = true;
            UpdateClient_Button.IsEnabled = clientUpdater.isUpdateAvailable();
        }

        // Загружает настройки из Json файла
        private void loadSettingsFromFile (string _filePath) {
            if (!File.Exists(_filePath)) { return; }
            settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(_filePath));
        }

        // Сохраняет настройки в Json файл
        private void saveSettingsToFile (Settings _settings, string _filePath) {
            string settingsJson = JsonConvert.SerializeObject(_settings);
            File.WriteAllText(_filePath, settingsJson);
        }

        private void RunGame_Button_Click(object sender, RoutedEventArgs e)
        {
            // Сохраняем настройки и вызываем скрипт запуска игры
            saveSettingsToFile(settings, "./updaterConfig.json");
            new Game(settings).Run();
        }

        private async void UpdateClient_Button_Click(object sender, RoutedEventArgs e)
        {
            // Вызов функции обновления клиента
            await UpdateClient();
        }

        async private Task UpdateClient()
        {
            // Перед обновлением выключаем кнопки
            RunGame_Button.IsEnabled = false;
            UpdateClient_Button.IsEnabled = false;

            // Сбрасываем прогресс-бары (основной и в таскбаре)
            var taskbarInstance = TaskbarManager.Instance;
            UpdateProgress_ProgressBar.Value = 0;

            Log.AppendText($"Обновление клиентской части... \n");
            Log.ScrollToEnd();

            // Асинхронная функция для загрузки файлов. Получает прогресс-бары для вывода процесса операции.
            await clientUpdater.applyUpdate(UpdateProgress_ProgressBar, taskbarInstance);

            Log.AppendText($"Проверка файлов на целостность... \n");
            Log.ScrollToEnd();

            // Проверяем, обновилось-ли всё так, как надо. Поврежденные файлы снова попадут в очередь.
            await clientUpdater.checkUpdates();

            if(!clientUpdater.isUpdateAvailable())
            {
                Log.AppendText($"Клиент обновлен!\n");
                Log.ScrollToEnd();
            }

            // Сбрасываем прогресс-бары (основной и в таскбаре)
            UpdateProgress_ProgressBar.Value = 0;
            taskbarInstance.SetProgressState(TaskbarProgressBarState.NoProgress);

            // Возвращаем кнопкам активность
            RunGame_Button.IsEnabled = true;
            UpdateClient_Button.IsEnabled = clientUpdater.isUpdateAvailable();
        }



        /* При вводе в поля, обновляем информацию в настройках */

        private void UserName_TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.settings.username = UserName_TextBox.Text;
        }

        private void Ram_TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Проверка на то, что мы вводим число
            string oldValue = this.settings.ram.ToString();
            if (!int.TryParse(Ram_TextBox.Text, out this.settings.ram))
            {
                Ram_TextBox.Text = oldValue;
                Ram_TextBox.CaretIndex = Ram_TextBox.Text.Length;
            }
        }
    }
}
