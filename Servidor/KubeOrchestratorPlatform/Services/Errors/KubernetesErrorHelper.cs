using System.Text.Json;
using k8s.Autorest;

namespace Servidor.KubeOrchestratorPlatform.Services.Errors
{
    public static class KubernetesErrorHelper
    {
        /// <summary>
        /// El cliente de Kubernetes lanza HttpOperationException con un .Message técnico
        /// ("Operation returned an invalid status code 'X'") seguido del cuerpo JSON crudo
        /// que devuelve el API server (formato k8s.Models.V1Status). Esto extrae solo el
        /// campo "message" de ese JSON, que es el texto legible que Kubernetes ya arma
        /// (ej. "Service \"hgh\" is invalid: ... provided port is already allocated").
        /// Si no se puede parsear, cae de vuelta al mensaje original de la excepción.
        /// </summary>
        public static string GetFriendlyMessage(Exception ex)
        {
            if (ex is HttpOperationException httpEx && !string.IsNullOrWhiteSpace(httpEx.Response?.Content))
            {
                try
                {
                    using var doc = JsonDocument.Parse(httpEx.Response.Content);
                    if (doc.RootElement.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                    {
                        return messageProp.GetString() ?? ex.Message;
                    }
                }
                catch (JsonException)
                {
                    // El body no era el JSON de Status esperado; usamos el mensaje original.
                }
            }

            return ex.Message;
        }
    }
}
