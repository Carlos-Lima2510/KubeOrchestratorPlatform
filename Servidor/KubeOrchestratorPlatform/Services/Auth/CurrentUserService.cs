using System.Security.Claims;

namespace Servidor.KubeOrchestratorPlatform.Services.Auth
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public int UserId
        {
            get
            {
                var idClaim = httpContextAccessor.HttpContext?.User?.FindFirst("id");
                if (idClaim != null && int.TryParse(idClaim.Value, out int userId))
                {
                    return userId;
                }
                
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }
        }

        public string Username => httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
    }
}