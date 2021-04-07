using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MinecraftUpdater {

    // Доступные операции с файлами при обновлении
    enum UpdateOperations
    {
        update,
        remove,
        create_directory
    }

    class JSONResponse
    {
        public object deserializedObject;
        public string message;
    }

    class Settings
    {
        public string username = "User";
        public int ram = 2000;
        public string javaPath = "javaw";
    }

    class FileHashInfo
    {
        public string file;
        public string hash;
    }

    class UpdateTask
    {
        public string file;
        public UpdateOperations operation;
    }

    class SyncOptions
    {
        public string root = "./";
        public string[] syncElements = { };
        public List<string> exceptions = new List<string>();
    }
}
