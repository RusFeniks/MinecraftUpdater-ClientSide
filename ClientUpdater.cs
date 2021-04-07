using Microsoft.WindowsAPICodePack.Taskbar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MinecraftUpdater {

    class ClientUpdater
    {
        // Для подключения к серверам обновлений
        string host;
        WebClient webClient;

        // Список задачек для обновления
        List<UpdateTask> updateTasks;

        // Сюда выводим всё, что происходит с нами, во время апдейта
        TextBox logTextBox;

        public ClientUpdater(string server, string port, TextBox log)
        {
            webClient = new WebClient();
            host = $"http://{server}:{port}/";
            updateTasks = new List<UpdateTask>();
            logTextBox = log;
        }

        // Функция для логирования действий
        private void logPrint(string message)
        {
            logTextBox.AppendText(message + "\n");
            logTextBox.ScrollToEnd();
        }

        // Получить JSON с сервера
        private JSONResponse getJsonFromServer(string path)
        {
            JSONResponse response = new JSONResponse();
            try
            {
                // Получаем файл с веб-сервера, в виде json строки, затем десериализуем его в объект dynamic класса
                response.deserializedObject = JsonConvert.DeserializeObject(webClient.DownloadString(host + path));
                response.message = $"Успех!";
                return response;
            }
            catch (Exception error)
            {
                response.deserializedObject = null;
                response.message = $"Не удалось получить информацию об обновлениях от сервера! \n {error.Message}";
                return response;
            }
        }

        // Получает хеш файла. Если это папки, тогда получает пустую строку.
        private string getHash(string path)
        {
            if (Directory.Exists(path)) { return ""; }
            byte[] hashBytes = SHA256.Create().ComputeHash(File.OpenRead(path));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        // Получает дерево файлов и папок внутри директории
        private List<string> getFilesRecursive(string _folderPath, List<string> exceptions)
        {
            List<string> filesList = new List<string>();

            // Стандартизация путей файлов и папок
            string folderPath = _folderPath.Replace("\\", "/");

            // Обработка исключений, если текущий путь указан в списке игнорируемых, мы уходим
            if(exceptions.Find(exception => exception == folderPath) != null)
            {
                return filesList;
            }

            // Если путём является не папка, а файл, вернем лист с этим файлом внутри
            if (File.Exists(folderPath))
            {
                filesList.Add(folderPath);
                return filesList;
            }

            // Если папки не существует, вернем пустой лист
            if (!Directory.Exists(folderPath))
            {
                return filesList;
            }

            // Добавим текущую папку в список
            filesList.Add(folderPath);

            // Сначала рекурсивно обрабатываем все подпапки, составляя дерево
            foreach (string directory in Directory.GetDirectories(folderPath))
            {
                filesList.AddRange(getFilesRecursive(directory, exceptions));
            }

            // Добавляем все файлы внутри директории в список
            foreach (string _file in Directory.GetFiles(folderPath))
            {
                // Стандартизация путей файлов и папок
                string file = _file.Replace("\\", "/");
                // Если текущего файла нет в списке исключений, мы добавляем его в дерево
                if (exceptions.Find(exception => exception == file) == null)
                {
                    filesList.Add(file);
                }
            }

            return filesList;

        }

        private List<FileHashInfo> clientHashGenerate(SyncOptions syncOptions)
        {
            List<FileHashInfo> _clientHashList = new List<FileHashInfo>();

            // Для всех синхронизируемых каталогов
            foreach (string element in syncOptions.syncElements)
            {
                // Пытаемся получить список файлов в аналогичных каталогах на клиенте
                getFilesRecursive(element, syncOptions.exceptions).ForEach(_file =>
                {
                    // Перебираем все рекурсивно найденные файлы. Если файл не был ранее добавлен в список, добавляем его.
                    if (_clientHashList.Find(repetitive => repetitive.file == _file) == null)
                    {
                        _clientHashList.Add(new FileHashInfo { file = _file, hash = getHash(_file) });
                    }
                });
            }

            return _clientHashList;
        }

        // Генерируем список задач по обновлениям, на основе сравнения хеш-листов клиента и сервера
        private List<UpdateTask> updateTasksGenerate(List<FileHashInfo> clientHash, List<FileHashInfo> serverHash)
        {
            List<UpdateTask> _updateTasks = new List<UpdateTask>();

            // Для каждого файла на клиенте
            clientHash.ForEach(client =>
            {
                // Проверяем, есть ли файл с таким названием на сервере
                FileHashInfo server = serverHash.Find(e => e.file == client.file);
                if (server != null)
                {
                    // Если файл с таким-же названием есть на сервере, сравниваем хеш двух файлов
                    if (server.hash != client.hash)
                    {
                        // В случае, если хеш не совпадает, добавляем файл в список на обновление
                        _updateTasks.Add(new UpdateTask { file = server.file, operation = UpdateOperations.update });
                    }
                    // Удаляем файл из серверного хеш-листа, чтобы он не скачивался как недостающий
                    serverHash.Remove(server);
                }
                else
                {
                    // Если файла нет на сервере, создаем задачу на его удаление с клиента
                    _updateTasks.Add(new UpdateTask { file = client.file, operation = UpdateOperations.remove });
                }
            });

            // Для оставшихся файлов из списка файлов сервера (часть мы удалили на прошлом этапе)
            serverHash.ForEach(server =>
            {
                if (string.IsNullOrEmpty(server.hash))
                {
                    if (!Directory.Exists(server.file))
                    {
                        _updateTasks.Add(new UpdateTask { file = server.file, operation = UpdateOperations.create_directory });
                    }
                }
                else
                {
                    if (!File.Exists(server.file))
                    {
                        _updateTasks.Add(new UpdateTask { file = server.file, operation = UpdateOperations.update });
                    }
                }
            });

            return _updateTasks;
        }

        // Асинхронная функция для проверки обновлений
        public async Task checkUpdates()
        {
            // Очищаем список заданий
            updateTasks.Clear();

            logPrint($"Получение параметров синхронизации");

            // Получаем параметры синхронизации с сервера
            JSONResponse syncOptions_response = await Task.Run(() =>
            {
                return getJsonFromServer(".syncOptions");
            });

            // Если при получении параметров произошла ошибка - сообщаем об этом и уходим
            if (syncOptions_response.deserializedObject == null)
            {
                logPrint(syncOptions_response.message);
                return;
            }

            SyncOptions syncOptions = ((JObject)syncOptions_response.deserializedObject).ToObject<SyncOptions>();

            logPrint($"Получение хеш-листа с сервера");

            // Получаем хеш-лист с сервера
            JSONResponse serverHashList_response = await Task.Run(() =>
            {
                return getJsonFromServer(".hash");
            });

            // Если при получении хеш-листа произошла ошибка - сообщаем об этом и уходим
            if (serverHashList_response.deserializedObject == null)
            {
                logPrint(serverHashList_response.message);
                return;
            }

            List<FileHashInfo> serverHashList = ((JArray)serverHashList_response.deserializedObject).ToObject<List<FileHashInfo>>();

            logPrint($"Генерация хеш-листа на стороне клиента");

            // Генерируем хеш-лист на стороне клиента
            List<FileHashInfo> clientHashList = await Task.Run(() =>
            {
                return clientHashGenerate(syncOptions);
            });

            logPrint($"Построение списка требуемых изменений");

            // Генерируем список задач по обновлениям, на основе сравнения хеш-листов клиента и сервера
            updateTasks = await Task.Run(() =>
            {
                return updateTasksGenerate(clientHashList, serverHashList);
            });

            logPrint($"Доступно обновлений: {updateTasks.Count}");

        }

        // Возвращает true, в случае, если доступны обновления и false, если обновлений нет
        public bool isUpdateAvailable()
        {
            return updateTasks.Count > 0;
        }

        
        public async Task applyUpdate(ProgressBar progressbar, TaskbarManager taskbar)
        {
            taskbar.SetProgressState(TaskbarProgressBarState.Normal);

            double progress = 0;
            double targetProgress = updateTasks.Count;

            foreach(UpdateTask task in updateTasks)
            {
                progress++;
                string file = task.file;

                try
                {
                    switch (task.operation)
                    {
                        case (UpdateOperations.create_directory):
                            if (!Directory.Exists(file))
                            {
                                await Task.Run(() => Directory.CreateDirectory(file));
                            }
                            break;

                        case UpdateOperations.remove:
                            if (File.Exists(file))
                            {
                                await Task.Run(() => File.Delete(file));
                            }
                            else if (Directory.Exists(file))
                            {
                                await Task.Run(() => Directory.Delete(file, true));
                            }
                            break;

                        case UpdateOperations.update:
                            await Task.Run(() => webClient.DownloadFile(new Uri(host + file), file + ".download"));
                            if (File.Exists(file))
                            {
                                await Task.Run(() => File.Delete(file));
                            }
                            File.Move(file + ".download", file);
                            break;
                    }
                    progressbar.Value = progress / targetProgress * 100;
                    taskbar.SetProgressValue(Convert.ToInt32(progress), Convert.ToInt32(targetProgress));
                    logPrint($"[{task.operation}] {task.file}");
                }
                catch (Exception error)
                {
                    logPrint($"Ошибка при выполнении операции: {error.Message}");
                }
            }
            logPrint($"Применение обновлений завершено");
            return;
        }
    }
}
