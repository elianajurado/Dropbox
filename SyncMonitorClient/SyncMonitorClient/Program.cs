using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SyncMonitorClient
{
    class Program
    {
        //load configuration settings from appsettings.json
        public static IConfiguration LoadConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            return configurationBuilder.Build();
        }

        static void Main(string[] args)
        {
            IConfiguration configuration = LoadConfiguration();
            string folderPath = configuration["DirectoryToMonitor"];
            string serverIp = configuration["SyncServerIp"];
            int serverPort = int.Parse(configuration["SyncServerPort"]);

            var fileSyncClient = new FileSyncClient(folderPath, serverIp, serverPort);
            fileSyncClient.StartMonitoring();

            Console.WriteLine("Monitoring started. Press any key to stop...");
            Console.ReadKey();
        }
    }

    public class FileSyncClient
    {
        private FileSystemWatcher fileWatcher;
        private readonly string folderToMonitor;
        private readonly string serverIp;
        private readonly int serverPort;
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public FileSyncClient(string folderPath, string serverIp, int serverPort)
        {
            this.folderToMonitor = folderPath;
            this.serverIp = serverIp;
            this.serverPort = serverPort;

            fileWatcher = new FileSystemWatcher(folderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            fileWatcher.Created += OnFileCreatedOrChanged;
            fileWatcher.Changed += OnFileCreatedOrChanged;
            fileWatcher.Deleted += OnFileDeleted;
            fileWatcher.Renamed += OnFileRenamed;
        }

        // start monitoring for file changes
        public void StartMonitoring()
        {
            fileWatcher.EnableRaisingEvents = true;
            logger.Info($"Monitoring folder: {folderToMonitor}");
        }
        private void OnFileCreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                logger.Info($"{e.ChangeType} event for file: {e.FullPath}");
                _ = UploadFileAsync(e.FullPath); // Start the async file upload
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            logger.Info($"File deleted: {e.FullPath}");
            NotificationDelete(e.FullPath); 
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            logger.Info($"File renamed from {e.OldFullPath} to {e.FullPath}");
            if (File.Exists(e.FullPath))
            {
                _ = UploadFileAsync(e.FullPath); 
            }
        }

        private async Task UploadFileAsync(string filePath)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIp, serverPort))
                using (NetworkStream networkStream = client.GetStream())
                {
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(filePath));
                    byte[] fileNameLength = BitConverter.GetBytes(fileNameBytes.Length);
                    await networkStream.WriteAsync(fileNameLength, 0, fileNameLength.Length);
                    await networkStream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);

                    // Send the file data in chunks
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await networkStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                    logger.Info($"File {filePath} sent to server successfully.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error uploading file {filePath}");
            }
        }

        // notify the server when a file is deleted
        private void NotificationDelete(string filePath)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIp, serverPort))
                using (NetworkStream stream = client.GetStream())
                {
                    string deletionInfo = $"DELETE:{filePath}";
                    byte[] deletionBytes = Encoding.UTF8.GetBytes(deletionInfo);
                    stream.Write(deletionBytes, 0, deletionBytes.Length);
                    logger.Info($"Server notified of file deletion: {filePath}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error notifying server of deletion: {filePath}");
            }
        }
    }
}
