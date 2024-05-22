using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using static System.Net.Mime.MediaTypeNames;
using static Archivator.Dependecies;

namespace ServerApp
{
    public class FileManager
    {
        //Four Singleton-Cache collections
        private HashSet<int> ids; //set of file_ids (unique)
        private Dictionary<string, int> names_ids; //dictionary of filename - file_id

        private HashSet<int> zip_ids; //set of list_ids (unique id for every filelist
        private Dictionary<string, int> zip_hashes; //filelist ("fileId1 fileId2 ...") and listId

        private FileManager() { }

        //Singleton...
        private static FileManager instance = null;
        public static FileManager Instance()
        {
            string s = AppDomain.CurrentDomain.BaseDirectory;
            if (instance == null)
            {
                bool init = true;
                instance = new FileManager();

                object obj = TryLoadFile<HashSet<int>>(Path.Combine(s, IDS_PATH));
                if (obj == null)
                    init = true;
                else
                    init = false;

                instance.ids = obj == null ? new HashSet<int>() : (HashSet<int>)obj;

                obj = TryLoadFile<Dictionary<string, int>>(Path.Combine(s, NAMES_IDS_PATH));
                instance.names_ids = obj == null ? new Dictionary<string, int>() : (Dictionary<string, int>)obj;

                obj = TryLoadFile<HashSet<int>>(Path.Combine(s, ZIP_IDS));
                instance.zip_ids = obj == null ? new HashSet<int>() : (HashSet<int>)obj;

                obj = TryLoadFile<Dictionary<string, int>>(Path.Combine(s, ZIP_HASHES));
                instance.zip_hashes = obj == null ? new Dictionary<string, int>() : (Dictionary<string, int>)obj;

                if (init)
                {
                    Random r = new Random();
                    List<string> names = FileNames();
                    int rand_val = 0;
                    for (int i = 0; i < names.Count; ++i)
                    {
                        do
                        {
                            rand_val = GetRandomId(r);
                        }
                        while (instance.ids.Contains(rand_val));
                        instance.ids.Add(rand_val);
                        instance.names_ids[names[i]] = rand_val;
                    }
                }

            }
            return instance;
        }

        //Cache Methods for Json Serialization / Deserialization
        public static object TryLoadFile<T>(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex) { return null; }
        }
        public static void SaveFile(string path, object obj)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                File.WriteAllText(path, json);
            } catch (Exception ex) { }
        }

        public static List<string> FileNames()
        {
            try
            {
                string s = AppDomain.CurrentDomain.BaseDirectory;
                var x = Directory.GetFiles(Path.Combine(s, AMAZING_COLLECTIONS)).ToList();
                for (int i = 0; i < x.Count; ++i)
                {
                    x[i] = Path.GetFileName(x[i]);
                }
                return x;
            }
            catch { return null; }
        }
        
        //рандомный id в диапазоне [bound, bound * 10)
        public static int GetRandomId(Random rand, int bound = ID_BOUNDS)
        { 
             return rand.Next(9 * bound) + bound;
        }
       
        public FileManager GetInstance() => instance;
        public HashSet<int> GetIds() => instance.ids;
        public Dictionary<string, int> GetNamesIds() => instance.names_ids;
        public HashSet<int> GetZipIds() => instance.zip_ids;
        public Dictionary<string, int> GetZipDict() => instance.zip_hashes;

        //saving Cache while ShutDowning...
        public void OnShutDown()
        {
            try
            {
                string s = AppDomain.CurrentDomain.BaseDirectory;
                SaveFile(Path.Combine(s, IDS_PATH), ids);
                SaveFile(Path.Combine(s, NAMES_IDS_PATH), names_ids);
                SaveFile(Path.Combine(s, ZIP_IDS), zip_ids);
                SaveFile(Path.Combine(s, ZIP_HASHES), zip_hashes);
            }
            catch { }
        }
    } 
}