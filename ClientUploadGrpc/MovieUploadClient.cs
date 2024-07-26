using Google.Protobuf;
using Grpc.Net.Client;
using GrpcServiceClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientUploadGrpc
{
    public class MovieUploadClient
    {
        private GrpcChannel channel;
        private readonly FileServer.FileServerClient fileServerClient;
        public MovieUploadClient()
        {
            channel = GrpcChannel.ForAddress("https://localhost:7232", new GrpcChannelOptions
            {
                MaxSendMessageSize = int.MaxValue,
                MaxReceiveMessageSize = int.MaxValue
            });
            fileServerClient = new FileServer.FileServerClient(channel);
        }
        public async Task MultiplyUpload(IEnumerable<string> files)
        {
            var uploadFiles = files.Select(file => Task.Run(() => UploadFile(file)));
            await Task.WhenAll(uploadFiles);
        }
        public async Task UploadFile(string file)
        {
            byte[] buffer = new byte[2048];
            FileStream fileStream = new FileStream(file, FileMode.Open);
            var content = new GrpcServiceClient.FileUploadRequest
            {
                Data = ByteString.CopyFrom(buffer),
                FileSize = fileStream.Length,
                ReadedByte = 0,
                Info = new GrpcServiceClient.FileInfo
                {
                    FileName = Path.GetFileNameWithoutExtension(fileStream.Name),
                    FileExtention = Path.GetExtension(fileStream.Name)
                }
            };
            var upload = fileServerClient.FileUpload();

            while ((content.ReadedByte = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                content.Data = ByteString.CopyFrom(buffer);
                await upload.RequestStream.WriteAsync(content);
            }
            await upload.RequestStream.CompleteAsync();
            Console.WriteLine((await upload.ResponseAsync).Success);

            fileStream.Close();
        }

        public async Task Download(string downloadPath, string file)
        {
            decimal loggedChunkSize = 0;
            byte[] buffer = new byte[8192];
            var fileInfo = new GrpcServiceClient.FileInfo
            {
                FileName = Path.GetFileNameWithoutExtension(file),
                FileExtention = Path.GetExtension(file)
            };
            var request = fileServerClient.FileDownload(fileInfo);
            var cancellation = new CancellationTokenSource();
            int count = 0;
            decimal chunkSize = 0;
            FileStream fileStream = null;

            while (await request.ResponseStream.MoveNext(cancellation.Token))
            {
                if (count++ == 0)
                {
                    fileStream = new FileStream($"{downloadPath}/{request.ResponseStream.Current.Info.FileName}{request.ResponseStream.Current.Info.FileExtention}", FileMode.CreateNew);

                    fileStream.SetLength(request.ResponseStream.Current.FileSize);
                }
                buffer = request.ResponseStream.Current.Data.ToByteArray();

                await fileStream.WriteAsync(buffer, 0, request.ResponseStream.Current.ReadedByte);

                chunkSize += request.ResponseStream.Current.ReadedByte;

                //Log progress every 1 MB or so
                if (chunkSize - loggedChunkSize >= 1024 * 1024 * 2)
                {
                    var progress = (double)chunkSize / request.ResponseStream.Current.FileSize * 100;
                    Console.WriteLine($"Upload progress: {request.ResponseStream.Current.Info.FileName} - {progress:F2}%");
                    loggedChunkSize = chunkSize;
                }
            }
            await fileStream.DisposeAsync();
            fileStream.Close();
            Console.WriteLine($"File was downloaded successful");
        }
        public async Task Download(string file)
        {
            var downloadPath = @"C:\Users\Artem\Downloads\unknown\fromDb";
            decimal loggedChunkSize = 0;
            byte[] buffer = new byte[8192];
            var fileInfo = new GrpcServiceClient.FileInfo
            {
                FileName = Path.GetFileNameWithoutExtension(file),
                FileExtention = Path.GetExtension(file)
            };
            var request = fileServerClient.FileDownloadFromDb(fileInfo);
            var cancellation = new CancellationTokenSource();
            int count = 0;
            decimal chunkSize = 0;
            FileStream fileStream = null;

            while (await request.ResponseStream.MoveNext(cancellation.Token))
            {
                if (count++ == 0)
                {
                    fileStream = new FileStream($"{downloadPath}/{request.ResponseStream.Current.Info.FileName}{request.ResponseStream.Current.Info.FileExtention}", FileMode.CreateNew);

                    fileStream.SetLength(request.ResponseStream.Current.FileSize);
                }
                buffer = request.ResponseStream.Current.Data.ToByteArray();

                await fileStream.WriteAsync(buffer, 0, request.ResponseStream.Current.ReadedByte);

                chunkSize += request.ResponseStream.Current.ReadedByte;

                //Log progress every 1 MB or so
                if (chunkSize - loggedChunkSize >= 1024 * 1024 * 2)
                {
                    var progress = (double)chunkSize / request.ResponseStream.Current.FileSize * 100;
                    Console.WriteLine($"Upload progress: {request.ResponseStream.Current.Info.FileName} - {progress:F2}%");
                    loggedChunkSize = chunkSize;
                }
            }
            await fileStream.DisposeAsync();
            fileStream.Close();
            Console.WriteLine($"File was downloaded successful");
        }
    }
}
