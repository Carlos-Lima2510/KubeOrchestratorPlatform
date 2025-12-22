using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Services.Auth;
using Servidor.KubeOrchestratorPlatform.Services.Graph;

[ApiController]
[Route("api/clusters/{clusterId}/graph")]
[Authorize]
public class GraphController : ControllerBase
{
    private readonly IGraphService _graphService;
    private readonly ICurrentUserService _currentUser;

    public GraphController(IGraphService graphService, ICurrentUserService currentUser)
    {
        _graphService = graphService;
        _currentUser = currentUser;
    }

    [HttpGet("{namespaceName}")]
    public async Task<IActionResult> GetGraph(string clusterId, string namespaceName)
    {
        var result = await _graphService.GetFullTopologyAsync(_currentUser.UserId, clusterId, namespaceName);
        return Ok(result);
    }
}