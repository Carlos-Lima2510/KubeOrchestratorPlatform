namespace Servidor.KubeOrchestratorPlatform.Models.DTOs.Pagination
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public string? ContinueToken { get; set; }
        public int RemainingCount { get; set; }
    }
}