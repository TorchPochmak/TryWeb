using System;
using Aspose.Zip;
using System.Text;
using Aspose.Zip.Saving;
using System.IO;
using System.Collections.Generic;
using static Archivator.Dependecies;


namespace Archivator
{

    class Program
    {
        //processId
        //basedirectory
        //zipId
        //file1
        //file2...
        private static int Main(string[] args) 
        {
            if (args.Length < 4)
                return (int)ArchCodes.UserError;

            string s = args[1];
            if(!Directory.Exists(s))
            {
                return (int)ArchCodes.ServerError;
            }
            Console.WriteLine(s);
            ArchCodes Code = ArchCodes.Success;
            //---------------------------------------------------------------------------
            //if exists this .zip???
            string ss = Path.Combine(args[1], ARCHIVES_PATH) + $"/{args[2]}.zip";
            if (File.Exists(ss))
                return (int)ArchCodes.UserError;
            //---------------------------------------------------------------------------s
            for (int i = 3; i < args.Length; ++i)
            {
                args[i] = Path.Combine(s, Path.Combine(AMAZING_COLLECTIONS_ARCHIVE, args[i]));
            }
            using (FileStream zipFile = File.Open(ss, FileMode.Create))
            {
                try
                {
                    List<(string, FileInfo)> files = new List<(string, FileInfo)>() { };
                    for (int i = 3; i < args.Length; ++i)
                    {
                        if (!File.Exists(args[i]))
                        {
                            Code = ArchCodes.UserError;
                            throw new Exception($"File {args[i]} not found");

                        }
                        files.Add((args[i], new FileInfo(args[i])));
                    }
                    using (var archive = new Archive())
                    {
                        for (int i = 0; i < files.Count; ++i)
                        {
                            (string first, FileInfo second) = files[i];
                            archive.CreateEntry(Path.GetFileName(first), second);
                        }
                        archive.Save(zipFile, new ArchiveSaveOptions() { Encoding = Encoding.ASCII });
                    }
                }
                catch (Exception ex) {
                    Code = ArchCodes.ServerError;
                }
            }
            return (int)Code;
        }   
    }
}