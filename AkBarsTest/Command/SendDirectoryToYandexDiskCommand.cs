using AkBarsTest.Factory;
using AkBarsTest.ValueObject;
using System;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Configuration;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

namespace AkBarsTest.Command
{
    class SendDirectoryToYandexDiskCommand
    {
        private SendParamsFactory SendParamsFactory;
        List<Task> Tasks;

        public SendDirectoryToYandexDiskCommand()
        {
            SendParamsFactory = new SendParamsFactory();
            Tasks = new List<Task>();
        }

        public void Execute(string sourcePath, string destinationPath)
        {
            var sendParams = SendParamsFactory.Create(sourcePath, destinationPath);
            CreateRemoteFolder(sendParams.DestinationPath, "");
            Recursive(sendParams.SourcePath, sendParams.DestinationPath);
            Task.WhenAll(Tasks).Wait();
        }

        private void CreateRemoteFolder(string folderName, string destinationPath)
        {
            using (var client = GetAuthorizedHttpClient()) {
                var request = CreateCreationRequest(folderName, destinationPath);
                Console.WriteLine($"Creating remote folder {Path.GetFileName(folderName)} inside of {destinationPath}");
                client.SendAsync(request).Wait();
            }
        }

        private static HttpClient GetAuthorizedHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", "OAuth " + ConfigurationManager.AppSettings.Get("OAuth"));
            return client;
        }

        private HttpRequestMessage CreateCreationRequest(string folderName, string destinationPath)
        {
            string targetFolder = CombineAndFormatPath(destinationPath, folderName);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://cloud-api.yandex.net/v1/disk/resources/?path={targetFolder}"),
                Content = new StringContent("")
            };
            return request;
        }
        private string CombineAndFormatPath(string origin, string addition)
        {
            return FormatPath(Path.Combine(origin, addition));
        }

        private string FormatPath(string path)
        {
            return path.Replace('\\', '/');
        }

        private void Recursive(string sourcePath, string destinationPath)
        {
            string folderName = Path.GetFileName(sourcePath);
            CreateRemoteFolder(folderName, destinationPath);
            Tasks.Add(UploadFiles(sourcePath, CombineAndFormatPath(destinationPath, folderName)));
            foreach (var childDirectoryPath in Directory.GetDirectories(sourcePath)) {
                Recursive(childDirectoryPath, Path.Combine(destinationPath, folderName));
            }
        }

        private Task UploadFiles(string localDirectoryPath, string destinationPath)
        {
            string[] fileNames = Directory.GetFiles(localDirectoryPath);
            int length = fileNames.Length;
            Task[] tasks = new Task[fileNames.Length];
            for (int i = 0; i < length; i++) {
                string fileName = fileNames[i];
                tasks[i] = uploadFile(destinationPath, fileName);
            }
            return Task.WhenAll(tasks);
        }

        private async Task<bool> uploadFile(string destinationPath, string fileName)
        {
            var rawFileName = Path.GetFileName(fileName);
            Console.WriteLine($"Uploading {rawFileName} file.");
            try {
                var uploadLink = await GetUploadLink(destinationPath, rawFileName);

                using (var client = new WebClient()) {
                    using (Stream fileStream = File.OpenRead(fileName)) {
                        using (Stream requestStream = client.OpenWrite(new Uri(uploadLink), "PUT")) {
                            fileStream.CopyTo(requestStream);
                        }
                    }
                }
                Console.WriteLine($"{rawFileName} file uploaded ");
            } catch (Exception e) {
                Console.WriteLine($"Error while uploading {rawFileName}");
            }

            return true;
        }

        private async Task<string> GetUploadLink(string destinationPath, string rawFileName)
        {
            var jsonTask = GetUploadResponse(destinationPath, rawFileName);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            string json = await jsonTask;
            var uploadResponse = JsonSerializer.Deserialize<UploadResponse>(json, options);
            return uploadResponse.Href;
        }

        private Task<string> GetUploadResponse(string destinationPath, string rawFileName)
        {
            var targetFileName = CombineAndFormatPath(destinationPath, rawFileName);
            string uri = "https://cloud-api.yandex.net/v1/disk/resources/upload?path=" + targetFileName;
            var _client = GetAuthorizedHttpClient();
            return _client.GetStringAsync(uri);
        }
    }
}
