namespace Servidor.KubeOrchestratorPlatform.Services.Auth
{
    public interface ICurrentUserService
    {
        int UserId { get; }
        string Username { get; }
    }
}