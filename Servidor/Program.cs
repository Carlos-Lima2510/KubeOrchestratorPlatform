using StackExchange.Redis;
using DotNetEnv;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
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
using Servidor.KubeOrchestratorPlatform.Services.Ingress;
using Servidor.KubeOrchestratorPlatform.Services.Pods;
using Servidor.KubeOrchestratorPlatform.Services.Metrics;
using Servidor.Hubs;
using Servidor.KubeOrchestratorPlatform.Services.Core;
using Microsoft.AspNetCore.DataProtection;
using Servidor.KubeOrchestratorPlatform.Services.Graph;
using Servidor.KubeOrchestratorPlatform.Services.Resources;

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
                .CreateLogger();

            try
            {
                Log.Information("Iniciando la aplicación...");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = builder.Configuration["Jwt:Issuer"],
                            ValidAudience = builder.Configuration["Jwt:Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];

                                var path = context.HttpContext.Request.Path;
                                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                                {
                                    context.Token = accessToken;
                                }
                                return Task.CompletedTask;
                            }
                        };
                    });

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontend",
                        policy =>
                        {
                            policy.WithOrigins("http://localhost:3000")
                                  .AllowAnyHeader()
                                  .AllowAnyMethod()
                                  .AllowCredentials();
                        });
                });

                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseMySql(builder.Configuration.GetConnectionString("MySql"),
                                     new MySqlServerVersion(new Version(8, 0, 44)),
                                     mySqlOptions => mySqlOptions.EnableRetryOnFailure(
                                     maxRetryCount: 5,
                                     maxRetryDelay: TimeSpan.FromSeconds(10),
                                     errorNumbersToAdd: null)

                    ));

                builder.Services.AddHttpContextAccessor();

                builder.Services.AddMemoryCache();

                builder.Services.AddSignalR();

                builder.Services.AddScoped<IUserClusterRepository, UserClusterRepository>();
                builder.Services.AddScoped<IUserClusterCache, UserClusterCache>();
                builder.Services.AddScoped<IClusterService, ClusterService>();
                builder.Services.AddScoped<IClusterConfigurationService, ClusterConfigurationService>();
                builder.Services.AddScoped<INamespaceService, NamespaceService>();
                builder.Services.AddScoped<IConfigMapService, ConfigMapService>();
                builder.Services.AddScoped<ISecretService, SecretService>();
                builder.Services.AddScoped<IIngressService, IngressService>();

                builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
                builder.Services.AddScoped<IAuthService, AuthService>();

                builder.Services.AddScoped<IDeploymentService, DeploymentService>();
                builder.Services.AddScoped<INetworkService, NetworkService>();
                builder.Services.AddScoped<IPodService, PodService>();
                builder.Services.AddScoped<IMetricsService, MetricsService>();
                builder.Services.AddScoped<IGraphService, GraphService>();
                builder.Services.AddScoped<IResourceService, ResourceService>();

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

                builder.Services.AddSwaggerGen(c =>
                {
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                },
                                Scheme = "oauth2",
                                Name = "Bearer",
                                In = ParameterLocation.Header,
                            },
                            new List<string>()
                        }
                    });
                });

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseCors("AllowFrontend");

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();

                app.MapHub<ClusterHub>("/hubs/cluster");
                app.MapHub<TerminalHub>("/hubs/terminal");

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
