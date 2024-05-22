using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;

namespace MyUtility
{
    class Program
    {
        private static HttpClient client = new HttpClient();

        public static string ServerHost = @"https://localhost:7083";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Client app started...");
            Console.WriteLine(@"Type <exit> to exit");
            Console.WriteLine("Use <help> for help");
            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                if (input == "exit" || input == null)
                    break;
                List<string> arguments = input.Split(' ').ToList();
                switch (arguments[0])
                {
                    case "change-host":
                        ChangeHost(arguments);
                        break;
                    case "shutdown":
                        await ShutDown();
                        break;
                    case "list":
                        await ListAvailable();
                        break;
                    case "create-archive":
                        await CreateArchive(arguments);
                        break;
                    case "status":
                        await CheckStatus(arguments);
                        break;
                    case "download":
                        await Download(arguments);
                        break;
                    case "download-fast":
                        await FastDownload(arguments);
                        break;
                    case "help":
                        Console.WriteLine("Available commands: ");
                        Console.WriteLine("exit - close client");
                        Console.WriteLine("change-host - change host. Now it is " + ServerHost);
                        Console.WriteLine("shutdown - shutdown server");
                        Console.WriteLine("list - get list of amazing files");
                        Console.WriteLine("create-archive <file1> <file2> ... - initialize an archive (returns process ID)");
                        Console.WriteLine("status <process ID> - get status of the process");
                        Console.WriteLine("download <process ID> <path to> - download an archive");
                        Console.WriteLine("download-fast <path> <file1> <file2> ... - download an archive directly");
                        Console.WriteLine("help - this information");
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
            Console.WriteLine("Exiting...");
        }
        //status <processId>
        public static void ChangeHost(List<string> args)
        {
            if(args == null || args.Count < 2)
            {
                Console.WriteLine("No new name entered.");
                return;
            }
            Console.WriteLine("Old host " + ServerHost);
            ServerHost = args[1];
            Console.WriteLine("New host " + ServerHost);
        }
        public static async Task CheckStatus(List<string> args)
        {
            if (args.Count != 2)
            {
                Console.WriteLine("Wrong count of args");
                return;
            }
            string processIdStr = args[1];

            var queryParams = new Dictionary<string, string>
            {
                { "processIdStr", processIdStr.ToString() },
            };
            string fullUrl = CreateURL("/status", queryParams);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            HttpResponseMessage response;
            // получаем ответ
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("No connection");
                Console.WriteLine(ex.Message);
                request.Dispose();
                return;
            }
            string content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine($"Error occured: {response.StatusCode}");
                Console.WriteLine(content);
            }
            response.Dispose();
            request.Dispose();
        }
        //download processId path
        public static async Task Download(List<string> args)
        {
            if(args.Count != 3) 
            {
                Console.WriteLine("Wrong count of args");
                return;
            }
            string processId = args[1];
            string path = args[2];
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Console.WriteLine("invalid path");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("invalid path");
                Console.WriteLine(ex.Message);
                return;
            }
            var queryParams = new Dictionary<string, string>
            {
                { "processIdStr", processId },
            };
            string fullUrl = CreateURL("/download", queryParams);


            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            HttpResponseMessage response;
            // получаем ответ
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex) 
            {
                Console.WriteLine("No connection");
                Console.WriteLine(ex.Message);
                request.Dispose();
                return;
            }
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(path, fileBytes);
          
            response.Dispose();
            request.Dispose();
            Console.WriteLine("Download complete.");
            
        }
        public static async Task ListAvailable()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, ServerHost + "/list");
            HttpResponseMessage response;
            // получаем ответ
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("No connection");
                Console.WriteLine(ex.Message);
                request.Dispose();
                return;
            }
            string content = await response.Content.ReadAsStringAsync();

            try
            {
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                {
                    List<string>? lines = JsonConvert.DeserializeObject<List<string>>(content);
                    if (lines == null)
                    {
                        Console.WriteLine("Unexpected behavior: list is null");
                        response.Dispose();
                        request.Dispose();
                        return;
                    }
                    for (int i = 0; i < lines.Count; i++)
                    {
                        Console.WriteLine($"- {lines[i]}");
                    }
                }
                else
                {
                    Console.WriteLine($"Error occured: {response.StatusCode}");
                    Console.WriteLine(content);
                }
            }
            catch (Exception e) 
            {
                Console.WriteLine("Unexpected behaviour");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                response.Dispose();
                request.Dispose();
            }

        }
        public static async Task CreateArchive(List<string> args)
        {
            if (args.Count == 1)
            {
                Console.WriteLine("Not enough args");
                return;
            }

            string url = ServerHost + "/create-archive";

            args.RemoveAt(0);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            string json = JsonConvert.SerializeObject(args);
            var reqContent = new StringContent(json, Encoding.UTF8, "application/json-patch+json");
            request.Content = reqContent;
            HttpResponseMessage response;
            // получаем ответ
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex) 
            {
                Console.WriteLine("No connection");
                Console.WriteLine(ex.Message);
                request.Dispose();
                return;
            }
            string content = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
            {
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine($"Error occured: {response.StatusCode}");
                Console.WriteLine(content);
            }
            response.Dispose();
            request.Dispose();
        }
        public static async Task FastDownload(List<string> args)
        {
            if (args.Count < 2)
            {
                Console.WriteLine("Not enough args");
                return;
            }

            string url = ServerHost + "/download-fast";

            args.RemoveAt(0);
            string path = args[0];
            args.RemoveAt(0);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            string json = JsonConvert.SerializeObject(args);
            var reqContent = new StringContent(json, Encoding.UTF8, "application/json-patch+json");
            request.Content = reqContent;
            HttpResponseMessage response;
            // получаем ответ
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("No connection");
                Console.WriteLine(ex.Message);
                request.Dispose();
                return;
            }
            //--------------------------------------------------------
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
            File.WriteAllBytes(path, fileBytes);
            Console.WriteLine("Download complete.");
        }

        public static async Task ShutDown()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, ServerHost + "/shutdown");
            HttpResponseMessage response;
            // получаем ответ
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine("No connection");
                Console.WriteLine(ex.Message);
                request.Dispose();
                return;
            }
            request.Dispose();
            Console.WriteLine("Server is shut down");
        }

        public static string CreateURL(string endpoint, Dictionary<string, string> args)
        {
            var queryString = string.Join("&", args.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            return ServerHost + endpoint + "?" + queryString;
        }
    }
}