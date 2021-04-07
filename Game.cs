using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace MinecraftUpdater
{
    class Game
    {
        private Settings settings; 

        public Game(Settings _settings) {
           settings = _settings;
        }

        public void Run()
        {
            // Основная строка запуска, содержащая список всех требуемых библиотек, а также различные оптимизации лежит в файле .launch
            if (!File.Exists(".launch"))
            {
                MessageBox.Show("Отсутствуют параметры запуска. Обновите клиент.");
                return;
            }

            // Вычисляем уникальный uuid игрока, основываясь на его нике
            byte[] uuidHash = MD5.Create().ComputeHash(BitConverter.GetBytes(settings.username.GetHashCode()));
            string uuid = BitConverter.ToString(uuidHash).Replace("-", "").ToLower();

            // Мне серьёзно стоит говорить что мы тут делаем?
            string root = Directory.GetCurrentDirectory();

            // В эту строчку сваливаем все аргументы, какие только можно. Все они пойдут как параметры запуска java
            string LaunchArguments = "";
            
            // Выделим память
            LaunchArguments += $" -Xmn128M -Xmx{settings.ram}m -Xms{Convert.ToInt32(settings.ram / 1.5)}m";

            // Загрузим все библиотеки и оптимизации из файла .launch
            LaunchArguments += File.ReadAllText(".launch");

            // Информация об игроке и папке с игрой
            LaunchArguments += $" --username {settings.username} --uuid {uuid} --gameDir {root}";
            
            // Пробуем запустить игру. Если не выходит (например, джава не установлена), мы получим соответствующую ошибку
            try
            {
                Process java = Process.Start(settings.javaPath, LaunchArguments);
                Environment.Exit(0);
            }
            catch (Exception error)
            {
                MessageBox.Show($"Не могу запустить процесс {settings.javaPath}: {error.Message}");
            }
        }
    }
}
