using StackExchange.Redis;
using Serilog;
using Servidor.KubeOrchestratorPlatform.Factories.Client;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Parser;
using Servidor.KubeOrchestratorPlatform.Services.Connection.Decorators;
using Servidor.KubeOrchestratorPlatform.Database.Data;
using Servidor.KubeOrchestratorPlatform.Services.Store;
using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Services.Store.Repositories;
using Servidor.KubeOrchestratorPlatform.Services.Store.Cache;

namespace Servidor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs/orchestrator.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Iniciando la aplicación...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(builder.Configuration.GetConnectionString("MySql"),
                                     new MySqlServerVersion(new Version(8, 0, 44))));

                builder.Services.AddScoped<IUserClusterRepository, UserClusterRepository>();
                builder.Services.AddScoped<IUserClusterCache, UserClusterCache>();
                builder.Services.AddScoped<IUserClusterStore, HybridUserClusterStore>();

                builder.Services.AddScoped<IKubernetesUserClientConnection, KubernetesUserClientConnection>();
                builder.Services.Decorate<IKubernetesUserClientConnection, LoggingKubernetesUserClientConnection>();


                builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var redisConn = builder.Configuration.GetConnectionString("Redis")
                        ?? throw new Exception("No se encontró la cadena de conexión de Redis.");

                    return ConnectionMultiplexer.Connect(redisConn);
                });

                builder.Services.AddControllers();
                builder.Services.AddSingleton<IAuditService, AuditService>();
                builder.Services.AddSingleton<IKubernetesClientFactory, KubernetesClientFactory>();
                builder.Services.AddSingleton<IKubeConfigParser, KubeConfigParser>();

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "La aplicación terminó inesperadamente");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
