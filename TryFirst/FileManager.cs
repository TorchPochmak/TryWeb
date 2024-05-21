using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using static System.Net.Mime.MediaTypeNames;

#pragma warning disable SYSLIB0011
namespace TryFirst
{
    public class FileManager
    {
        public const int ID_BOUNDS = 100000000;
        public const string IDS_PATH = "Cache/ids.json";//hashset<int>
        public const string NAMES_IDS_PATH = "Cache/ids_names.json";//Dictionary<string, int> 

        public const string ZIP_IDS = "Cache/zip_ids.json";//hashset<int>
        public const string ZIP_HASHES = "Cache/hashes_ids.json";//Dictionary<IEnumerable<int>, int>

        public const string COLLDIR = "AmazingCollections";

        private HashSet<int> ids;
        private Dictionary<string, int> names_ids;

        private HashSet<int> zip_ids;
        private Dictionary<List<int>, int> zip_hashes;
        
        private FileManager() { }

        //Singleton...
        private static FileManager instance = null;

        //Cache Methods for Json Serialization / Deserialization
        public static object TryLoadFile<T>(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }
        public static void SaveFile(string path, object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            File.WriteAllText(path, json);
        }
        public static List<string> FileNames()
        {
            string s = AppDomain.CurrentDomain.BaseDirectory;
            var x = Directory.GetFiles(s + "/" + COLLDIR).ToList();
            for(int i = 0; i < x.Count; ++i)
            {
                x[i] = Path.GetFileName(x[i]);
            }
            return x;
        }
        
        public static FileManager Instance()
        {
            if (instance == null)
            {
                bool init = true;
                instance = new FileManager();

                object obj = TryLoadFile<HashSet<int>>(IDS_PATH);
                if (obj == null)
                    init = true;
                else
                    init = false;
                instance.ids = obj == null ? new HashSet<int>() : (HashSet<int>)obj;

                obj = TryLoadFile<Dictionary<string, int>>(NAMES_IDS_PATH);
                instance.names_ids = obj == null ? new Dictionary<string, int>() : (Dictionary<string, int>)obj;

                obj = TryLoadFile<HashSet<int>>(ZIP_IDS);
                instance.zip_ids = obj == null ? new HashSet<int>() : (HashSet<int>)obj;

                obj = TryLoadFile<Dictionary<List<int>, int>>(ZIP_HASHES);
                instance.zip_hashes = obj == null ? new Dictionary<List<int>, int>() : (Dictionary<List<int>, int>)obj;
                
                if (init)
                {
                    Random r = new Random();
                    List<string> names = FileNames();
                    int rand_val = 0;
                    for(int i = 0; i < names.Count; ++i)
                    {
                        do
                        {
                            rand_val = r.Next(9 * ID_BOUNDS) + ID_BOUNDS;
                        }
                        while (instance.ids.Contains(rand_val));
                        instance.ids.Add(rand_val);
                        instance.names_ids.Add(names[i], rand_val);
                    }
                }

            }
            return instance;
        }

        public FileManager GetInstance() => instance;
        public HashSet<int> GetIds() => instance.ids;
        public Dictionary<string, int> GetNamesIds() => instance.names_ids;
        public HashSet<int> GetZipIds() => instance.zip_ids;
        public Dictionary<List<int>, int> GetZipDict() => instance.zip_hashes;

        public void OnShutDown()
        {
            SaveFile(IDS_PATH, ids);
            SaveFile(NAMES_IDS_PATH, names_ids);
            SaveFile(ZIP_IDS, zip_ids);
            SaveFile(ZIP_HASHES, zip_hashes);
        }
    }
}