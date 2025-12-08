using StackExchange.Redis;
using DotNetEnv;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Factories.Client;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Parser;
using Servidor.KubeOrchestratorPlatform.Services.Connection.Decorators;
using Servidor.KubeOrchestratorPlatform.Database.Data;
using Servidor.KubeOrchestratorPlatform.Repositories;
using Servidor.KubeOrchestratorPlatform.Cache;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Services.Security;
using Servidor.KubeOrchestratorPlatform.Services.Namespace;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Cluster.Configuration;
using Servidor.KubeOrchestratorPlatform.Services.Deployment;
using Servidor.KubeOrchestratorPlatform.Services.Network;
using Servidor.KubeOrchestratorPlatform.Services.Pods;
using Servidor.KubeOrchestratorPlatform.Services.Metrics;

namespace Servidor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Env.Load();

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

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontend",
                        policy =>
                        {
                            policy.WithOrigins("http://localhost:3000")
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                        });
                });

                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(builder.Configuration.GetConnectionString("MySql"),
                                     new MySqlServerVersion(new Version(8, 0, 44))));

                builder.Services.AddScoped<IUserClusterRepository, UserClusterRepository>();
                builder.Services.AddScoped<IUserClusterCache, UserClusterCache>();
                builder.Services.AddScoped<IClusterService, ClusterService>();
                builder.Services.AddScoped<IClusterConfigurationService, ClusterConfigurationService>();
                builder.Services.AddScoped<INamespaceService, NamespaceService>();
                builder.Services.AddScoped<ICurrentUserService, DevCurrentUserService>();
                builder.Services.AddScoped<IDeploymentService, DeploymentService>();
                builder.Services.AddScoped<INetworkService, NetworkService>();
                builder.Services.AddScoped<IPodService, PodService>();
                builder.Services.AddScoped<IMetricsService, MetricsService>();

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
                builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseCors("AllowFrontend");
                app.UseAuthorization();
                app.MapControllers();

                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<AppDbContext>();

                        if (context.Database.GetPendingMigrations().Any())
                        {
                            Log.Information("Aplicando migraciones de base de datos pendientes...");
                            context.Database.Migrate(); 
                            Log.Information("Migraciones aplicadas correctamente.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Ocurrió un error al intentar aplicar las migraciones automáticamente.");
                    }
                }

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
