using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using k8s;

[ApiController]
[Route("api/clusters/{clusterId}/resources")]
public class ResourceController : ControllerBase
{
    private readonly IKubernetesUserClientConnection clientConnection;
    private readonly ICurrentUserService userService;

    public ResourceController(IKubernetesUserClientConnection clientConnection, ICurrentUserService userService)
    {
        this.clientConnection = clientConnection;
        this.userService = userService;
    }

    [HttpGet("{kind}/{name}/yaml")]
    public async Task<IActionResult> GetYaml(string clusterId, string kind, string name, [FromQuery] string @namespace = "default")
    {
        var user = userService.UserId;
        var client = clientConnection.GetClient(user, clusterId);
        object? resource = kind.ToLower() switch
        {
            "ingress" => await client.NetworkingV1.ReadNamespacedIngressAsync(name, @namespace),
            "deployment" => await client.AppsV1.ReadNamespacedDeploymentAsync(name, @namespace),
            "service" => await client.CoreV1.ReadNamespacedServiceAsync(name, @namespace),
            "pod" => await client.CoreV1.ReadNamespacedPodAsync(name, @namespace),
            _ => null
        };

        if (resource == null) return NotFound();

        var serializer = new YamlDotNet.Serialization.SerializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .Build();

        return Ok(serializer.Serialize(resource));
    }
}
