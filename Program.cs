using Renci.SshNet;

namespace FtpFileAccess
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GetDirectory();
            Console.WriteLine("Hello, World!");
        }

        private static void GetDirectory()
        {
            string SFTP_CLIENT_HOST = "FTP HOST";
            string SFTP_CLIENT_USERNAME = "FTP USER NAME";
            string SFTP_CLIENT_PASSWORD = "FTP PASSWORD";
            string SFTP_CLIENT_ARCHIVE = "Archive";
            string SFTP_CLIENT_EXCEPTION = "Exception";

            var localFolderPath = LocalFolderPath();

            var connectionInfo = new ConnectionInfo(SFTP_CLIENT_HOST, SFTP_CLIENT_USERNAME, new PasswordAuthenticationMethod(SFTP_CLIENT_USERNAME, SFTP_CLIENT_PASSWORD));

            using (var client = new SftpClient(connectionInfo))
            {
                client.Connect();

                if (client.IsConnected)
                {
                    //Get directory and file list
                    var directoryList = client.ListDirectory(client.WorkingDirectory);

                    if (directoryList != null && (!directoryList.Any(d => d.Name == SFTP_CLIENT_ARCHIVE) || !directoryList.Any(d => d.Name == SFTP_CLIENT_EXCEPTION)))
                    {
                        CreateDirectories(client, SFTP_CLIENT_ARCHIVE);
                        CreateDirectories(client, SFTP_CLIENT_EXCEPTION);
                    }

                    if (directoryList != null && directoryList.Count() > 0)
                    {
                        foreach (var directory in directoryList)
                        {
                            if (!directory.IsDirectory)
                            {
                                var filePath = localFolderPath + directory.FullName;
                                if (File.Exists(filePath)) File.Delete(filePath);
                                DownloadFileToClient(client, filePath, directory.FullName);

                                try
                                {
                                    // process file here
                                }
                                catch (Exception e)
                                {
                                    var sftpFilePathException = client.WorkingDirectory + SFTP_CLIENT_EXCEPTION + directory.FullName;
                                    MoveFile(client, filePath, sftpFilePathException, directory.FullName);
                                    Console.WriteLine(e.Message);
                                    throw;
                                }

                                var sftpFilePathArchive = client.WorkingDirectory + SFTP_CLIENT_ARCHIVE + directory.FullName;
                                MoveFile(client, filePath, sftpFilePathArchive, directory.FullName);
                            }
                        }
                    }
                }
            }
        }

        private static void DownloadFileToClient(SftpClient sftpClient, string localPath, string remotePath)
        {
            using (Stream fileStream = File.OpenWrite(localPath))
            {
                sftpClient.DownloadFile(remotePath, fileStream);
            }
        }

        private static void MoveFile(SftpClient client, string localFilePath, string ftpDestPath, string ftpSourcePath)
        {
            using (Stream fileStream = File.OpenRead(localFilePath))
            {
                client.UploadFile(fileStream, ftpDestPath);
                client.DeleteFile(ftpSourcePath);
            }
        }

        private static void CreateDirectories(SftpClient sftpClient, string directory)
        {
            if (!sftpClient.Exists(directory))
            {
                var directories = directory.Trim().Split('/');
                var path = "";
                for (var i = 0; i < directories.Length; i++)
                {
                    path += directories[i] + @"/";
                    if (!string.IsNullOrWhiteSpace(directories[i]))
                    {
                        try
                        {
                            sftpClient.CreateDirectory($"{path}");
                        }
                        catch (Exception)
                        {
                            //directory exists
                        }
                    }
                }
            }
        }

        private static string LocalFolderPath()
        {
            
            var localFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "upload");
            if (!Directory.Exists(localFolderPath))
            {
                Directory.CreateDirectory(localFolderPath);
            }
            return localFolderPath;
        }
    }
}