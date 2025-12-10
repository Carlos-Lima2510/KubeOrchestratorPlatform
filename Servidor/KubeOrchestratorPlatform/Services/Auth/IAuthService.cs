using Servidor.KubeOrchestratorPlatform.Models.DTOs.Auth;

namespace Servidor.KubeOrchestratorPlatform.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto request);
        Task<AuthResponseDto> LoginAsync(LoginDto request);
    }
}