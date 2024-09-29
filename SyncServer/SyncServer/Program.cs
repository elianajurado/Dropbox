using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SyncServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string serverIp = "127.0.0.1";
            int serverPort = 8080;

            TcpListener listener = new TcpListener(IPAddress.Parse(serverIp), serverPort);
            listener.Start();
            Console.WriteLine($"Server running on {serverIp}:{serverPort}");

            while (true)
            {
                Console.WriteLine("Waiting for a client...");
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Client connected.");

                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        static async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    // Read the file name length first
                    byte[] fileNameLengthBytes = new byte[4];
                    await stream.ReadAsync(fileNameLengthBytes, 0, fileNameLengthBytes.Length);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBytes, 0);

                    // Read the actual file name
                    byte[] fileNameBytes = new byte[fileNameLength];
                    await stream.ReadAsync(fileNameBytes, 0, fileNameBytes.Length);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes);

                    // Store file data in memory to process it
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await memoryStream.WriteAsync(buffer, 0, bytesRead);
                        }
                        byte[] fileData = memoryStream.ToArray();

                        string destinationFolder = @"C:\Users\ElianaJurado\Desktop\SyncedFiles";
                        Directory.CreateDirectory(destinationFolder);
                        string destinationFilePath = Path.Combine(destinationFolder, fileName);

                        if (File.Exists(destinationFilePath))
                        {
                            byte[] existingFileHash = ComputeFileHash(destinationFilePath);
                            byte[] incomingFileHash = CalculateHash(fileData);

                            if (AreHashesEqual(existingFileHash, incomingFileHash))
                            {
                                Console.WriteLine($"File '{fileName}' is identical to the one on the server. Overwriting...");
                                File.WriteAllBytes(destinationFilePath, fileData);
                            }
                            else
                            {
                                // File is different, versioning it
                                string versionedFilePath = GetVersionedFilePath(destinationFilePath);
                                File.Move(destinationFilePath, versionedFilePath);
                                Console.WriteLine($"Renamed existing file to {Path.GetFileName(versionedFilePath)}. Saving new version...");

                                File.WriteAllBytes(destinationFilePath, fileData);
                            }
                        }
                        else
                        {
                            File.WriteAllBytes(destinationFilePath, fileData);
                            Console.WriteLine($"Received and saved new file: {fileName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client connection closed.");
            }
        }

        static string GetVersionedFilePath(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            int version = 1;

            while (File.Exists(filePath))
            {
                filePath = Path.Combine(directory, $"{fileName}_v{version}{extension}");
                version++;
            }

            return filePath;
        }

        static byte[] ComputeFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        static byte[] CalculateHash(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        static bool AreHashesEqual(byte[] hash1, byte[] hash2)
        {
            if (hash1.Length != hash2.Length) return false;

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i]) return false;
            }

            return true;
        }
    }
}
