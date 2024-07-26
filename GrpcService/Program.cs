using GrpcService.Data;
using GrpcService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

            // Add services to the container.
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxConcurrentConnections = 100;
                options.Limits.MaxConcurrentUpgradedConnections = 100;
            });
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Server=ArtemKobzar;Database=GrpcSQL;Trusted_Connection=True;TrustServerCertificate=true;"));
            builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);
            builder.Services.AddTransient<MovieUploadService>();

            builder.Services.AddGrpc(opt =>
            {
                opt.EnableDetailedErrors = true;
                opt.MaxReceiveMessageSize = int.MaxValue;
                opt.MaxSendMessageSize = int.MaxValue;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseStaticFiles();
            app.MapGrpcService<GreeterService>();
            app.MapGrpcService<MovieService>();
            app.MapGrpcService<MovieUploadService>();

            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            app.Run();
        }
    }
}