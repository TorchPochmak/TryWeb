using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Archivator
{
    public static class Dependecies
    {

        public const int ID_BOUNDS = 100000000;
        public const string IDS_PATH = "Cache/ids.json";//hashset<int>
        public const string NAMES_IDS_PATH = "Cache/ids_names.json";//Dictionary<string, int> 

        public const string ZIP_IDS = "Cache/zip_ids.json";//hashset<int>
        public const string ZIP_HASHES = "Cache/hashes_ids.json";//Dictionary<IEnumerable<int>, int>
        public const string ARCHIVATOR_PATH = "Archivator.exe";

        public const string DATABASE_PATH = "Logs/Log.db";

        public const string DATABASE_COMMAND = "CREATE TABLE Log (" + //sql to create a log db
                "CreatedOn TEXT," +
                "Message TEXT," +
                "Level TEXT," +
                "Exception TEXT," +
                "StackTrace TEXT," +
                "Logger TEXT," +
                "Url TEXT);";
        public const string ARCHIVES_PATH = "Archives";
        public const string AMAZING_COLLECTIONS_ARCHIVE = "AmazingCollections";
        public const string AMAZING_COLLECTIONS = "AmazingCollections";
    }
}
