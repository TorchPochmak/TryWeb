using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using static System.Net.Mime.MediaTypeNames;

#pragma warning disable SYSLIB0011
namespace Archivator
{
    public class FileManager : IDisposable
	{
        public const string IDS_PATH = "Cache/ids.bin";//hashset<int>
		public const string NAMES_IDS_PATH = "Cache/ids_names.bin";//Dictionary<string, int> 

		public const string ZIP_IDS = "Cache/zip_ids.bin";//hashset<int>
		public const string ZIP_HASHES = "Cache/hashes_ids.bin";//Dictionary<IEnumerable<string>, int>

		public const string COLLDIR = "AmazingCollections";
		private HashSet<int> ids;
		private Dictionary<string, int> names_ids;

		private HashSet<int> zip_ids;
		private Dictionary<List<string>, int> zip_hashes;

        //----------------------------------------------------------------
        private static FileManager instance;
		
		public static object TryLoadFile(string path)
		{
			if(!File.Exists(path))
			{
				return null;
			}
			object obj = null;
            using (var file = File.Open(IDS_PATH, FileMode.Open))
            {
                BinaryFormatter bf = new BinaryFormatter();
                //Dangerous, but...I have no time
				obj = bf.Deserialize(file);
            }
			return obj;
        }
		public static void SaveFile(string path, object obj)
		{
            if (!File.Exists(path))
            {
				return;
            }
            using (Stream file = File.Open(path, FileMode.Create))
            {
                BinaryFormatter bf = new BinaryFormatter();
                //Dangerous, but...I have no time
                bf.Serialize(file, obj);
            }
        }
		public static List<string> FileNames()
		{
			return Directory.GetFiles(COLLDIR).ToList();
		}
		
		//------------------------------------------------------------------		
		protected FileManager() { Console.Write("WHAT"); }
		public static FileManager Instance()
		{
			if (instance == null)
			{
				bool init = true;
				instance = new FileManager();

				object obj = TryLoadFile(IDS_PATH);
				if (obj == null)
					init = true;
				else
					init = false;
                instance.ids = obj == null ? new HashSet<int>() : (HashSet<int>)obj;

				obj = TryLoadFile(NAMES_IDS_PATH);
				instance.names_ids = obj == null ? new Dictionary<string, int>() : (Dictionary<string, int>)obj;

				obj = TryLoadFile(ZIP_IDS);
				instance.zip_ids = obj == null ? new HashSet<int>() : (HashSet<int>)obj;

				obj = TryLoadFile(ZIP_HASHES);
				instance.zip_hashes = obj == null ? new Dictionary<List<string>, int>() : (Dictionary<List<string>, int>)obj;

				if(init)
				{
					List<string> names = FileNames();
					Console.Write("OK");
				}
            }
			return instance;
		}

		public FileManager GetInstance() => instance;
		public HashSet<int> GetIds() => instance.ids;
		public Dictionary<string, int> GetNamesIds() => instance.names_ids;
		public HashSet<int> GetZipIds() => instance.zip_ids;
		public Dictionary<List<string>, int> GetZipHashes() => instance.zip_hashes;

        public void Dispose()
		{
			Console.Write("OK");
			SaveFile(IDS_PATH, ids);
            SaveFile(NAMES_IDS_PATH, names_ids);
			SaveFile(ZIP_IDS, zip_ids);
			SaveFile(ZIP_HASHES, zip_hashes);
        }
    }
}