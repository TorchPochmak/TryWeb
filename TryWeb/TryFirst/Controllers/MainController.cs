using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Archivator;
using static Archivator.Dependecies;
using Microsoft.AspNetCore.StaticFiles;

namespace ServerApp.Controllers
{

    [ApiController]
    public class MainController : ControllerBase
    {
        //listId - unique id for every list of files, also name of zip in Archives
        //listId - (processId, ArchCode)
        public static Dictionary<int, (int, ArchCodes)> ListIdDict = new Dictionary<int, (int, ArchCodes)>();//list_id - key4

        //processId - unique id for every process (mainly for client)
        //processId - (listId, ArchCode)
        public static Dictionary<int, (int, ArchCodes)> ProcessesIdDict = new Dictionary<int, (int, ArchCodes)>();//process_id - key

        //Логгер
        private readonly ILogger<MainController> _logger;

        private readonly IHostApplicationLifetime _lifetime;

        //it's okay
        private string GetContentType(string path)
        {
            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            if (!provider.TryGetContentType(path, out contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        public MainController(ILogger<MainController> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        //Get method.
        //Empty Body
        //0 Parameters
        //Returns a list of available Files in AmazingCollection
        [HttpGet("/list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult FileList()
        {
            try
            {
                var x = FileManager.FileNames();
                _logger.LogInformation("Ok");
                return Ok(x);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //OnExited Archivator process..
        private void ExitProcess(object? sender, EventArgs e)
        {
            var process = (Process)sender;
            try
            {   
                //args structure: processId, appPath, listId, file1, file2....
                List<string> lst = process.StartInfo.Arguments.Split(' ').ToList();

                int processId = int.Parse(lst[0]);
                int listId = int.Parse(lst[2]);

                //only filenames left in list
                lst.RemoveRange(0, 3);

                //Formatting from {file1, file2} into {listId1, listId2}
                string listKey = "";
                for(int i = 0; i < lst.Count; i++)
                {
                    if (i != 0) listKey += " ";
                   listKey += FileManager.Instance().GetNamesIds()[lst[i]];
                }

                //Updating ArchCodes - from ArchCodes.Calculating to ExitCode
                ProcessesIdDict[processId] = (listId, (ArchCodes)process.ExitCode);
                ListIdDict[listId] = (processId, (ArchCodes)process.ExitCode);


                var info = ProcessesIdDict[processId];
                if ((ArchCodes)process.ExitCode == ArchCodes.Success)
                {
                    FileManager.Instance().GetZipIds().Add(listId);
                    FileManager.Instance().GetZipDict()[listKey] = listId;
                }
                _logger.LogInformation("Process done");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            process.Dispose();
        }

        //Немного нарушу абстракцию для download-fast
        private (int, string) CreateArchiveConcrete(List<string> filenames)
        {
            try
            {
                //Validating
                var allNames = FileManager.FileNames();
                for (int i = 0; i < filenames.Count; ++i)
                {
                    if (!allNames.Contains(filenames[i]))
                    {
                        string msg = "File not found " + filenames[i];
                        _logger.LogError(msg);
                        return (StatusCodes.Status404NotFound, msg);
                    }
                }
                //--------------------------------------
                List<int> ids = new List<int>(); //{file1, file2} -> {fileId1, fileId2}
                foreach (string filename in filenames)
                {
                    var dictNames = FileManager.Instance().GetNamesIds();
                    ids.Add(dictNames[filename]);
                }
                ids.Sort();
                //---------------------------------------

                var zipIds = FileManager.Instance().GetZipIds();
                var zipDict = FileManager.Instance().GetZipDict();
                string idsKey = string.Join(" ", ids);//{fileId1, fileId2} -> "fileId1 fileId2" <-> listId

                //for new archive
                int listId = 0;
                int processId = 0;

                if (zipDict.ContainsKey(idsKey))
                {
                    //Already exists!
                    listId = zipDict[idsKey];

                    //Already Calculating/Done - just not downloaded
                    if (ListIdDict.ContainsKey(listId))
                    {
                        processId = ListIdDict[listId].Item1;
                        string msg = $"Archive is already in use. Check status {processId} for more info";
                        _logger.LogInformation(msg);
                        return (StatusCodes.Status200OK, msg);
                    }
                    //Exists in Cache!
                    _logger.LogInformation("Ok");
                    return (StatusCodes.Status200OK, "Cache" + listId.ToString());
                }
                else
                {
                    //Creating new unique listId
                    Random r = new Random();
                    listId = FileManager.GetRandomId(r);
                    zipIds.Add(listId);
                    zipDict[idsKey] = listId;
                    do
                    {
                        processId = FileManager.GetRandomId(r);
                    }
                    while (ProcessesIdDict.ContainsKey(processId));
                    //------------------------------------------------
                    string args = processId.ToString() + " " +
                                  AppDomain.CurrentDomain.BaseDirectory + " " +
                                  listId.ToString();
                    for (int i = 0; i < ids.Count; ++i)
                    {
                        args += " " + filenames[i].ToString();
                    }
                    //-------------------------------------------------
                    //args = {"processId path listId(name) file1 file2..."}

                    var process = new Process();
                    try
                    {
                        process.StartInfo.FileName = ARCHIVATOR_PATH;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.Arguments = args;
                        process.EnableRaisingEvents = true;
                        process.Exited += new EventHandler(ExitProcess);

                        //Updating...
                        ListIdDict[listId] = (processId, ArchCodes.Calculating);
                        ProcessesIdDict[processId] = (listId, ArchCodes.Calculating);
                        process.Start();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        process.Dispose();//?..
                        return (StatusCodes.Status500InternalServerError, ex.Message);
                    }
                    
                }
                //----------------------------------------
                _logger.LogInformation("Ok");
                return (StatusCodes.Status200OK, processId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return (StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //Post method.
        //Body - {file1, file2, file3...} (in json)
        //0 Parameters
        //Creates a process of archivating files
        [HttpPost("/create-archive")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateArchive([FromBody] List<string> filenames)
        {
            (int code, string result_message) = CreateArchiveConcrete(filenames);
            return StatusCode(code, result_message);
        }

        private (int, string) CheckStatusConcrete(string processIdStr)
        {
            try
            {
                if (processIdStr.StartsWith("Cache"))
                {
                    int id = 0;
                    bool succ = Int32.TryParse(processIdStr.Substring(5), out id);
                    if (!succ)
                    {
                        string msg = "Not Found process Id";
                        _logger.LogError(msg);
                        return (StatusCodes.Status404NotFound, msg);
                    }
                    //Check in Cache .zip
                    var filepath = System.IO.File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(ARCHIVES_PATH, $"{id}.zip")));
                    if (filepath)
                    {
                        string msg = "Success";
                        _logger.LogInformation(msg);
                        return (StatusCodes.Status200OK, msg);
                    }
                    else
                    {
                        string msg = "Not Found file " + filepath;
                        _logger.LogError(msg);
                        return (StatusCodes.Status404NotFound, msg);
                    }
                }

                int processId = 0;
                bool suc = int.TryParse(processIdStr, out processId);
                if (!suc)
                {
                    string msg = "Not Found processId";
                    _logger.LogError(msg);
                    return (StatusCodes.Status404NotFound, msg);
                }

                if (ProcessesIdDict.ContainsKey(processId))
                {
                    var code = ProcessesIdDict[processId].Item2;
                    var listId = ProcessesIdDict[processId].Item1;
                    string msg = "";
                    switch (code)
                    {
                        case ArchCodes.Success:
                            msg = "Success";
                            _logger.LogInformation(msg);
                            return (StatusCodes.Status200OK, msg);

                        case ArchCodes.ServerError:
                            msg = "ServerError: Archivator error";
                            _logger.LogCritical(msg);
                            return (StatusCodes.Status500InternalServerError, msg);

                        case ArchCodes.UserError:
                            msg = "BadRequest: User Error";
                            _logger.LogError(msg);
                            return (StatusCodes.Status400BadRequest,msg);

                        case ArchCodes.Calculating:
                            msg = "Process in progress...";
                            _logger.LogInformation(msg);
                            return (StatusCodes.Status200OK, msg);
                        default:
                            msg = "ServerError: Unknown process status " + code.ToString();
                            _logger.LogCritical(msg);
                            return (StatusCodes.Status500InternalServerError, msg);
                    }
                }
                else
                {
                    string msg = "Not Found processId";
                    _logger.LogError(msg);
                    return (StatusCodes.Status404NotFound,msg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
                return (StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //Get method
        //Empty Body
        //1 parameter - processIdStr
        //only digits (e.g 429428942, 298290342) - recently created new processes
        //Cache******* (e.g Cache234829924, Cache32748382) - Already in Cache -> Instantly Success
        //returns ArchCode (Calculating, ServerError, UserError or Success)
        [HttpGet("/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CheckStatus(string processIdStr)
        {
            (int code, string result_message) = CheckStatusConcrete(processIdStr);
            return StatusCode(code, result_message);
        }

        //zip path
        //returns (Ok/NotFound, msg), out path
        private (bool, string) GetFilePath(string processId, out string path)
        {
            path = "";
            int id = 0;
            if (processId.StartsWith("Cache"))
            {
                //Cache type
                bool suc = int.TryParse(processId.Substring(5), out id);
                if (!suc)
                {
                    string msg = "Not Found processId";
                    _logger.LogError(msg);
                    return (false, msg);
                }
            }
            else
            {
                //Simple type
                bool succ = Int32.TryParse(processId, out id);
                if (!succ || !ProcessesIdDict.ContainsKey(id))
                {
                    string msg = "Not Found processId";
                    _logger.LogError(msg);
                    return (false, msg); 
                }
                id = ProcessesIdDict[id].Item1;//now id is listId
            }
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(ARCHIVES_PATH, $"{id}.zip"));
            if (System.IO.File.Exists(path))
            {
                return (true, "");
            }
            else
            {
                string msg = "File Not Found " + path;
                _logger.LogError(msg);
                return (false, msg);
            }
        }

        //Get method
        //Empty body
        //1 parameter - processIdStr
        //returns File.
        [HttpGet("/download")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Download(string processIdStr)
        {
            string path = "";
            (bool a, string result) = GetFilePath(processIdStr, out path);
            if(a != true)
            {
                return NotFound(result);
            }
            //----------------------------------
            try
            {
                //useless name
                string name = "Archive";

                if (!processIdStr.Contains("Cache"))
                { 
                    ListIdDict.Remove(ProcessesIdDict[int.Parse(processIdStr)].Item1);
                    ProcessesIdDict.Remove(int.Parse(processIdStr));
                }

                if (!System.IO.File.Exists(path))
                {
                    //hm..
                    string msg = "File not found " + path;
                    _logger.LogError(msg);
                    return NotFound(msg);
                }
                //Maybe I am wrong here...
                var memory = new MemoryStream();
                try
                {
                    var stream = new FileStream(path, FileMode.Open);
                        try { await stream.CopyToAsync(memory); }
                        catch 
                        { 
                            _logger.LogCritical("FileStream troubles");
                            memory.Dispose();//?;
                            stream.Dispose();
                            return StatusCode(StatusCodes.Status500InternalServerError);
                        }
                    memory.Position = 0;
                    _logger.LogInformation("Success download");
                    return File(memory, GetContentType(path), name);
                }
                catch 
                { 
                    _logger.LogCritical("MemoryStream troubles...");
                    memory.Dispose();//?..
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        //Post method
        //Empty body
        //1 parameter - processIdStr
        //returns File. Directly from FileList
        [HttpPost("/download-fast")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Download_Real([FromBody] List<string> filenames)
        {
            (int code, string processId) = CreateArchiveConcrete(filenames);
            if (code != 200 && code != 204)
                return StatusCode(code, processId);
            while (true)
            {
                (code, string result) = CheckStatusConcrete(processId);
                if(code == 200 || code == 204)
                {
                    if (result == "Success")
                        break;
                }
                else
                {
                    return StatusCode(code, result);
                }
                Thread.Sleep(50);//stupid solution, i have no time, sorry :(
            }
            return await Download(processId);
        }


        //Only right method to save Cache
        [HttpDelete("/shutdown")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult ShutDown()
        {
            _lifetime.StopApplication();
            _logger.LogInformation("Shutting down...");
            return NoContent();
        }
    }
}
