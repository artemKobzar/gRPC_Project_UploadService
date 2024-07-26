using Azure.Core;
using Grpc.Core;
using Google.Protobuf;
using GrpcService;
using GrpcService.Data;
using GrpcService.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;
using static Google.Rpc.Context.AttributeContext.Types;
using System.Diagnostics;

namespace GrpcService.Services
{
    public class MovieUploadService : FileServer.FileServerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<MovieUploadService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public MovieUploadService(ILogger<MovieUploadService> logger, AppDbContext dbContext, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        public override async Task<FileUploadResponse> FileUpload(IAsyncStreamReader<FileUploadRequest> requestStream, ServerCallContext context)
        {
            _logger.LogInformation("FileUpload called");
            if (_webHostEnvironment.WebRootPath == null)
            {
                _logger.LogError("WebRootPath is null");
                throw new ArgumentNullException(nameof(_webHostEnvironment.WebRootPath), "WebRootPath cannot be null");
            }
            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");
            
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            System.IO.FileStream fileStream = null;
            decimal chunkSize = 0;
            decimal loggedChunkSize = 0;
            var count = 0;
            var videoFile = new VideoFile();
            using var memoryStream = new MemoryStream();
            try
            {
                while(await requestStream.MoveNext())
                {
                    if(count++ == 0)
                    {
                        fileStream = new System.IO.FileStream($"{path}/{requestStream.Current.Info.FileName}{requestStream.Current.Info.FileExtention}", FileMode.CreateNew);
                        fileStream.SetLength(requestStream.Current.FileSize);
                        videoFile.Name = requestStream.Current.Info.FileName;
                    }
                    var buffer = requestStream.Current.Data.ToByteArray();

                    await fileStream.WriteAsync(buffer, 0, buffer.Length);
                    await memoryStream.WriteAsync(buffer, 0, buffer.Length);
                    chunkSize += requestStream.Current.ReadedByte;
                 
                    // Log progress every 1 MB or so
                    if(chunkSize - loggedChunkSize >= 1024 * 1024 * 2)
                    {
                        var progress = (double)chunkSize / requestStream.Current.FileSize * 100;
                        Console.WriteLine($"Upload progress: {requestStream.Current.Info.FileName} - {progress:F2}%");
                        loggedChunkSize = chunkSize;
                    }
                }
                if (videoFile != null)
                {
                    videoFile.Data = memoryStream.ToArray();
                    _dbContext.VideoFiles.Add(videoFile);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"File was saved to database: {videoFile.Name}");
                }
                _logger.LogInformation($"File was uploaded successful");
            }
            catch (IOException ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                return new FileUploadResponse { Success = "Error of uploading" };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading file: {ex.Message}");
                return new FileUploadResponse { Success = "Error of uploading" };
            }
            await fileStream.DisposeAsync();
            fileStream.Close();
            return new FileUploadResponse { Success = "File was uploaded" };
        }

        public override async Task FileDownload(FileInfo request, IServerStreamWriter<FileUploadRequest> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("FileDownload called");
            _logger.LogInformation("FileDownload called with fileName: {FileName} and fileExt: {FileExt}", request.FileName, request.FileExtention);
            if (_webHostEnvironment.WebRootPath == null)
            {
                _logger.LogError("ContentRootPath is null");
                throw new ArgumentNullException(nameof(_webHostEnvironment.WebRootPath), "ContentRootPath cannot be null");
            }

            string path = Path.Combine(_webHostEnvironment.WebRootPath, "files");
            
            using FileStream fileStream = new FileStream($"{path}/{request.FileName}{request.FileExtention}", FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[8192];
            FileUploadRequest response = new FileUploadRequest
            {
                FileSize = fileStream.Length,
                Info = new FileInfo
                {
                    FileName = Path.GetFileNameWithoutExtension(fileStream.Name),
                    FileExtention = Path.GetExtension(fileStream.Name)
                },
                ReadedByte = 0
            };

            while ((response.ReadedByte = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                response.Data = ByteString.CopyFrom(buffer);
                await responseStream.WriteAsync(response);
            }
            fileStream.Close();
            _logger.LogInformation($"File was downloaded successful");
        }
        public override async Task FileDownloadFromDb(FileInfo request, IServerStreamWriter<FileUploadRequest> responseStream, ServerCallContext context)
        {
            _logger.LogInformation("FileDownload called");
            _logger.LogInformation("FileDownload called with fileName: {FileName} and fileExt: {FileExt}", request.FileName, request.FileExtention);

            var stopwatch = Stopwatch.StartNew();
            var videoFile = await _dbContext.VideoFiles.AsNoTracking().FirstOrDefaultAsync(file => file.Name == request.FileName);
            stopwatch.Stop();
            _logger.LogInformation("Time taken to read from database: {TimeElapsed}ms", stopwatch.ElapsedMilliseconds);

            if (videoFile == null)
            {
                _logger.LogError("File not found in database: {fileName}", request.FileName);
                throw new FileNotFoundException("File not found in database", request.FileName);
            }
            _logger.LogInformation("Starting to stream file: {fileName}", videoFile.Name);

            byte[] buffer = new byte[8192];
            using MemoryStream fileStream = new MemoryStream(videoFile.Data);

            FileUploadRequest response = new FileUploadRequest
            {
                FileSize = fileStream.Length,
                Info = new FileInfo
                {
                    FileName = Path.GetFileNameWithoutExtension(videoFile.Name),
                    FileExtention = request.FileExtention
                },
                ReadedByte = 0
            };
            while ((response.ReadedByte = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                response.Data = ByteString.CopyFrom(buffer);
                await responseStream.WriteAsync(response);
            }
            fileStream.Close();
            _logger.LogInformation($"File was downloaded successful");
        }
    }
}