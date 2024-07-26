using Google.Protobuf;
using Grpc.Net.Client;
using GrpcService;
using GrpcServiceClient;
using GrpcService.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace ClientUploadGrpc
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            while(true)
            {
                MovieUploadClient client = new MovieUploadClient();
                var fileName = "NO20240410-091700-005652.mp4";
                var fileName1 = "a84da817653d4f1a8b09f99ec368a069.mp4";
                var downloadPath = @"C:\Users\Artem\Downloads\unknown";
                var files = new List<string> { @"C:\Users\Artem\Downloads\NO20240410-091700-005652.mp4", @"C:\Users\Artem\Downloads\NO20240316-174907-005181.mp4", @"C:\Users\Artem\Downloads\NO20240325-094824-005349.mp4" };
                Console.WriteLine("Press '1' or '2' for uploading a file, '3' - multiply uploading, '4' - download file from Server, '5' - download file from Database, or 'Enter' to exit");
                var upload = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(upload))
                    break;
                switch (upload)
                {
                    case "1":
                        await client.UploadFile(@"C:\Users\Artem\Downloads\a84da817653d4f1a8b09f99ec368a069.mp4");
                        break;
                    case "2":
                        await client.UploadFile(@"C:\Users\Artem\Downloads\Knox.Goes.Away.2023.1080p.mkv");
                        break;
                    case "3":
                        await client.MultiplyUpload(files);
                        break;
                    case "4":
                        await client.Download(downloadPath, fileName);
                        break;
                    case "5":
                        await client.Download(fileName1);
                        break;
                    default:
                        Console.WriteLine("Unknown operation");
                        break;
                }
            }
            Console.ReadLine();
        }
    }
}
