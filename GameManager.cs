using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;

namespace MinecraftUpdater
{
    class GameManager
    {

        public GameManager() {}

        public void Run(string gamePath, string javaPath, int RAM, string login, string password, string customParams)
        {
            if (Regex.IsMatch(gamePath, "[А-я ]+"))
            {
                MessageBox.Show("Путь до клиента игры содержит пробелы, либо символы, отличные от латинницы.\n" +
                    "Это может вызвать ошибки при запуске игры. Настоятельно рекомендуется изменить путь в настройках лаунчера.", "Ошибка пути");
                return;
            }

            string launchFilePath = Path.Combine(gamePath, ".init");

            // Основная строка запуска, содержащая список всех требуемых библиотек, а также различные оптимизации лежит в файле .launch
            if (!File.Exists(launchFilePath))
            {
                MessageBox.Show("Отсутствует файл с параметрами запуска.\n" +
                    "Убедитесь, что путь до клиента указан верно и клиент имеет последние обновления.", "Ошибка запуска");
                return;
            }
            
            byte[] uuidHash = MD5.Create().ComputeHash(BitConverter.GetBytes(login.GetHashCode()));
            string uuid = BitConverter.ToString(uuidHash).Replace("-", "").ToLower();


            byte[] passwordHash = MD5.Create().ComputeHash(BitConverter.GetBytes(password.GetHashCode()));
            File.WriteAllText(Path.Combine(gamePath, ".sl_password"), BitConverter.ToString(passwordHash).Replace("-", "").ToLower());

            string launchString = File.ReadAllText(launchFilePath);
            launchString = launchString.Replace("%login%", login);
            launchString = launchString.Replace("%ram%", RAM.ToString());
            launchString = launchString.Replace("%javaPath%", javaPath);
            launchString = launchString.Replace("%gamePath%", gamePath);
            launchString = launchString.Replace("%uuid%", uuid);
            launchString = launchString.Replace("%customParams%", customParams);


            try
            {
                Directory.SetCurrentDirectory(Path.Combine(gamePath));
                Process java = Process.Start(javaPath, launchString);
                Environment.Exit(0);
            }
            catch (Exception error)
            {
                MessageBox.Show($"Не могу запустить процесс {javaPath}: {error.Message}\nУбедитесь что у вас установлена Java 8");
            }

            return;
        }
    }
}
