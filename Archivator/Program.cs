using System;
using Aspose.Zip;
using System.Text;
using Aspose.Zip.Saving;
using System.IO;
using System.Collections.Generic;

namespace Archivator
{

    class Program
    {
       
        private static StatusCode ValidateDestination(string dest)
        {
            bool directoryExists = Directory.Exists(Path.GetDirectoryName(dest));
            bool fileExists = File.Exists(dest);

            if (!directoryExists || fileExists)
                return StatusCode.UserError;
            return StatusCode.Success;
        }
        //args:
        //destination
        //file1
        //file2
        //file3...
        private static int Main(string[] args) 
        {
            string dest = @"c:\check\";
            args = new string[]{ "fir", "second.txt", "first.txt"};
            for(int i = 0; i < args.Length; ++i) 
            {
                args[i] = dest + args[i];
            }


            StatusCode Code = StatusCode.Success;
            //---------------------------------------------------------------------------
            if (args.Length < 2) 
                return (int)StatusCode.UserError;

            if ((Code = ValidateDestination(args[0])) != StatusCode.Success)
                return (int)Code;

            //---------------------------------------------------------------------------s
            var rand  = new Random();
            int x = rand.Next(9000) + 1000;
            using (FileStream zipFile = File.Open($@"c:\check\{x}.zip", FileMode.Create))
            {
                try
                {
                    List<(string, FileInfo)> files = new List<(string, FileInfo)>() { };
                    for (int i = 1; i < args.Length; ++i)
                    {
                        if (!File.Exists(args[i]))
                        {
                            Code = StatusCode.UserError;
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
                            Thread.Sleep(1000 * 15);
                        }
                        archive.Save(zipFile, new ArchiveSaveOptions() { Encoding = Encoding.ASCII });
                    }
                }
                catch (Exception ex) {
                    
                    Console.Write(ex.Message);
                    Code = StatusCode.ServerError;
                }
            }
            Console.Write("DONE");
            return (int)Code;
        }   
    }
}