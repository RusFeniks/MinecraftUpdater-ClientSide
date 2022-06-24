using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace MinecraftUpdater
{

    class SyncOptions
    {
        public string root;
        public string[] syncElements;
        public string[] exceptions;
        public OptionalFile[] optional;
    }

    class OptionalFile
    {
        public string link;
        public string install;
    }

    class HashObject
    {
        public string file;
        public string hash;
    }

    enum UpdateOperations
    {
        DOWNLOAD,
        DELETE,
        CREATE_DIR
    }

    class UpdateTask
    {
        public string file;
        public string link;
        public UpdateOperations task;
    }

    class ClientUpdater
    {
        string serverURL;
        string clientPath;

        public ClientUpdater(string _serverURL, string _clientPath)
        {
            serverURL = _serverURL;
            clientPath = _clientPath;
        }

        /// <summary>
        /// Обновить клиент игры
        /// </summary>
        /// <param name="progressBar">Прогрессбар для отображения прогресса обновления</param>
        /// <returns></returns>
        public async Task Update(ProgressBar progressBar)
        {
            List<UpdateTask> updateTasks = new List<UpdateTask>();
            await Task.Run(() =>
            {
                SyncOptions syncOptions = GetSyncOptionsAsync($"{serverURL}/.syncOptions");
                List<HashObject> clientHashList = GetClientHashListAsync(syncOptions.syncElements);
                List<HashObject> serverHashList = GetServerHashListAsync($"{serverURL}/.hash");
                updateTasks.AddRange(CompareHashLists(clientHashList, serverHashList, syncOptions.exceptions));
                updateTasks.AddRange(CheckOptionalFiles(syncOptions.optional));
            });
            await ApplyUpdate(updateTasks, progressBar);
        }

        public async Task<bool> UpdatesCheck()
        {
            List<UpdateTask> updateTasks = new List<UpdateTask>();
            await Task.Run(() =>
            {
                SyncOptions syncOptions = GetSyncOptionsAsync($"{serverURL}/.syncOptions");
                if(syncOptions == null) { return; }
                List<HashObject> clientHashList = GetClientHashListAsync(syncOptions.syncElements);
                List<HashObject> serverHashList = GetServerHashListAsync($"{serverURL}/.hash");
                updateTasks.AddRange(CompareHashLists(clientHashList, serverHashList, syncOptions.exceptions));
                updateTasks.AddRange(CheckOptionalFiles(syncOptions.optional));
            });
            return updateTasks.Count > 0;
        }

        /// <summary>
        /// Получить опции синхронизации с сервера
        /// </summary>
        /// <param name="link">Ссылка на файл опций</param>
        /// <returns></returns>
        private SyncOptions GetSyncOptionsAsync(string link)
        {
            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;

            string syncOptionsString = "";

            try {
                syncOptionsString = webClient.DownloadString(link);
            } catch
            {
                return null;
            }
            
            SyncOptions syncOptions = JsonConvert.DeserializeObject<SyncOptions>(syncOptionsString);

            for(int i = 0; i < syncOptions.syncElements.Length; i++)
            {
                syncOptions.syncElements[i] = Path.Combine(Regex.Split(clientPath + "/" + syncOptions.syncElements[i], "[\\/]+"));
            }

            for (int i = 0; i < syncOptions.exceptions.Length; i++)
            {
                syncOptions.exceptions[i] = Path.Combine(Regex.Split(clientPath + "/" + syncOptions.exceptions[i], "[\\/]+"));
            }

            return syncOptions;
        }

        /// <summary>
        /// Построение hash листа файлов о отслеживаемых папках
        /// </summary>
        /// <param name="syncOptions"></param>
        /// <returns></returns>
        private List<HashObject> GetClientHashListAsync(string[] syncElements)
        {
            List<HashObject> clientHashList = new List<HashObject>();
            for(int i = 0; i < syncElements.Length; i++)
            {
                clientHashList.AddRange(GetHashByObjectsInFolder(syncElements[i]));
            }
            return clientHashList;
        }

        /// <summary>
        /// Рекурсивный обход папок и получение хеша, находящихся в них файлов
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        private List<HashObject> GetHashByObjectsInFolder(string fullPath)
        {
            List<HashObject> hashObjectsList = new List<HashObject>();

            if(Directory.Exists(fullPath))
            {
                hashObjectsList.Add(new HashObject { file = fullPath, hash = "" });
                foreach(string directory in Directory.GetDirectories(fullPath))
                {
                    hashObjectsList.AddRange(GetHashByObjectsInFolder(directory));
                }
                foreach(string file in Directory.GetFiles(fullPath))
                {
                    hashObjectsList.AddRange(GetHashByObjectsInFolder (file));
                }
            }
            else if (File.Exists(fullPath))
            {
                hashObjectsList.Add(new HashObject { file = fullPath, hash = GetFileHash(fullPath) });
            }
            return hashObjectsList;
        }

        /// <summary>
        /// Получить хеш файла
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string GetFileHash( string filePath )
        {
            StreamReader streamReader = new StreamReader(filePath);
            byte[] hashBytes = SHA256.Create().ComputeHash(streamReader.BaseStream);
            streamReader.Close();
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Получить хеш-лист с сервера
        /// </summary>
        /// <param name="hashFileLink"></param>
        /// <returns></returns>
        private List<HashObject> GetServerHashListAsync( string hashFileLink )
        {
            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;

            string serverHashListString = webClient.DownloadString(hashFileLink);
            List<HashObject> serverHashList = JsonConvert.DeserializeObject<List<HashObject>>(serverHashListString);

            serverHashList.ForEach(self =>
            {
                self.file = Path.Combine(Regex.Split(clientPath + "/" + self.file, "[\\/]+"));
            });

            return serverHashList;
        }

        /// <summary>
        /// Получает список модов пользователя из папки userMods.
        /// Эти моды, в дальнейшем, игнорируются при проверке обновлений.
        /// </summary>
        /// <returns></returns>
        private List<string> GetUserModsList()
        {
            List<string> userModsList = new List<string>();
            string userModsFolderPath = Path.Combine(clientPath, "UserMods");
            string modsFolder = Path.Combine(clientPath, "mods");

            if(Directory.Exists(userModsFolderPath))
            {
                string[] userMods = Directory.GetFiles(userModsFolderPath);
                userModsList.AddRange(userMods);

                foreach(string userMod in userMods)
                {
                    string fileName = Path.GetFileName(userMod);
                    userModsList.Add(Path.Combine(modsFolder, fileName));
                }
            }

            return userModsList;
        }

        /// <summary>
        /// Производит сравнение между хеш-листами сервера и клиента, создавая список заданий на обновление.
        /// </summary>
        /// <param name="clientHashList">Хеш-лист клиента</param>
        /// <param name="serverHashList">Хеш-лист сервера</param>
        /// <param name="exceptions">Исключения</param>
        /// <returns></returns>
        private List<UpdateTask> CompareHashLists(List<HashObject> clientHashList, List<HashObject> serverHashList, string[] exceptions)
        {
            List<UpdateTask> updateTasks = new List<UpdateTask>();
            List<string> ignoredFiles = new List<string>();
            ignoredFiles.AddRange(exceptions);
            ignoredFiles.AddRange(GetUserModsList());
            clientHashList.ForEach(clientHash =>
            {
                // Отсеиваем файлы, у которых всё хорошо и совпадает хеш
                int index = serverHashList.FindIndex(serverHash =>
                {
                    return clientHash.file == serverHash.file && clientHash.hash == serverHash.hash;
                });
                if (index > -1)
                {
                    serverHashList.RemoveAt(index);
                }
                else
                {
                    bool findException = false;
                    foreach (string exception in ignoredFiles)
                    {
                        findException = clientHash.file.Contains(exception);
                        if(findException) { break; }
                    }
                    if(!findException)
                    {
                        updateTasks.Add(new UpdateTask { file = clientHash.file, link = clientHash.file, task = UpdateOperations.DELETE });
                    }
                }
            });
            serverHashList.ForEach(serverHash =>
            {
                if(serverHash.hash == "")
                {
                    updateTasks.Add(new UpdateTask { file = serverHash.file, link = serverHash.file, task = UpdateOperations.CREATE_DIR });
                } else
                {
                    updateTasks.Add(new UpdateTask { file = serverHash.file, link = serverHash.file, task = UpdateOperations.DOWNLOAD });
                }
            });
            return updateTasks;
        }

        private List<UpdateTask> CheckOptionalFiles (OptionalFile[] optionals)
        {
            List<UpdateTask> updateTasks = new List<UpdateTask>();

            foreach(OptionalFile optional in optionals)
            {
                string localFilePath = Path.Combine(Regex.Split(clientPath + "/" + optional.install, "[\\/]+"));
                if (!File.Exists(localFilePath))
                {
                    updateTasks.Add(new UpdateTask { file = localFilePath, link = $"{serverURL}/{optional.link}", task = UpdateOperations.DOWNLOAD });
                }
            }

            return updateTasks;
        }

        private void DownloadFileFromServerAsync(string localFilePath, string linkFilePath)
        {
            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;

            string url = linkFilePath.Replace(clientPath, serverURL).Replace('\\', '/');
            Uri uri = new Uri(url);
            byte[] fileData = webClient.DownloadData(uri);
            string dir = Path.GetDirectoryName(localFilePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            FileStream file = File.OpenWrite(localFilePath);
            file.Write(fileData, 0, fileData.Length);
            file.Close();
            return;
        }

        private async Task ApplyUpdate(List<UpdateTask> updateTasks, ProgressBar progressBar)
        {
            progressBar.Maximum = updateTasks.Count;
            progressBar.Value = 0;

            foreach(UpdateTask updateTask in updateTasks)
            {
                await Task.Run(() =>
                {
                    switch (updateTask.task)
                    {
                        case UpdateOperations.DOWNLOAD:
                            DownloadFileFromServerAsync(updateTask.file, updateTask.link);
                            break;

                        case UpdateOperations.CREATE_DIR:
                            Directory.CreateDirectory(updateTask.file);
                            break;

                        case UpdateOperations.DELETE:
                            if (File.Exists(updateTask.file))
                            {
                                File.Delete(updateTask.file);
                            }
                            else if (Directory.Exists(updateTask.file))
                            {
                                try
                                {
                                    Directory.Delete(updateTask.file, true);
                                } catch (Exception error)
                                {
                                    MessageBox.Show($"Невозможно автоматически удалить каталог: {updateTask.file}\nДля корректной работы клиента игры, произведите удаление вручную!\nПричина: {error.Message}");
                                }
                            }
                            break;
                    }
                });
                progressBar.Value += 1;
            }

            await Task.Delay(1000);
            progressBar.Maximum = 1;
            progressBar.Value = 0;
            return;
        }
    }
}
