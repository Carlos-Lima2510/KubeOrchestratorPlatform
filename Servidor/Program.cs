using StackExchange.Redis;
using Servidor.KubeOrchestratorPlatform.Factories.Client;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Parser;
using Servidor.KubeOrchestratorPlatform.Services.Store;

namespace Servidor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var redisConn = builder.Configuration.GetConnectionString("Redis")
                    ?? throw new Exception("No se encontró la cadena de conexión de Redis.");

                return ConnectionMultiplexer.Connect(redisConn);
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddSingleton<IKubernetesClientFactory, KubernetesClientFactory>();
            builder.Services.AddSingleton<IKubeConfigParser, KubeConfigParser>();
            builder.Services.AddSingleton<IUserClusterStore, RedisUserClusterStore>();
            builder.Services.AddSingleton<IKubernetesUserClientConnection, KubernetesUserClientConnection>();


            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
