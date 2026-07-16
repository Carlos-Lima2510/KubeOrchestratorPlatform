using System.Net;
using System.Net.Sockets;

namespace Servidor.KubeOrchestratorPlatform.Services.Security
{
    public class NetworkSafetyValidator : INetworkSafetyValidator
    {
        public async Task EnsureSafeClusterEndpointAsync(string serverUrl)
        {
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException("La URL del servidor del cluster no es válida.");

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(uri.Host);
            }
            catch (SocketException)
            {
                return;
            }

            if (addresses.Any(IsBlockedMetadataAddress))
            {
                throw new ArgumentException("La dirección del cluster no es válida: apunta a un rango de red no permitido.");
            }
        }

        private static bool IsBlockedMetadataAddress(IPAddress ip)
        {
            if (ip.IsIPv6LinkLocal) return true;

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                return bytes[0] == 169 && bytes[1] == 254;
            }

            return false;
        }
    }
}
