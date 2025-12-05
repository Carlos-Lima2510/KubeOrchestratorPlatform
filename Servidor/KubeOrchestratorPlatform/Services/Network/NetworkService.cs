using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network;
using Servidor.KubeOrchestratorPlatform.Responses;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Network
{
    public class NetworkService : INetworkService
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IClusterService clusterService;
        private readonly IAuditService auditService;

        public NetworkService(
            IKubernetesUserClientConnection clientConnection,
            IClusterService clusterService,
            IAuditService auditService)
        {
            this.clientConnection = clientConnection;
            this.clusterService = clusterService;
            this.auditService = auditService;
        }
        public async Task<NetworkServiceResponse> CreateAsync(int userId, string clusterId, CreateServiceDTO dto)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);

            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                var service = BuildServiceObject(dto);
                await client.CoreV1.CreateNamespacedServiceAsync(service, dto.Namespace);

                auditService.LogOperation(userName, clusterName, $"CreateService: {dto.Name}", true);

                var warning = await CheckForMatchingPodsAsync(client, dto.Namespace, dto.Selector);

                return new NetworkServiceResponse
                {
                    Name = dto.Name,
                    Message = "Servicio creado exitosamente.",
                    Warning = warning
                };
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"CreateService: {dto.Name}", false, ex.Message);
                throw;
            }
        }

        private async Task<string?> CheckForMatchingPodsAsync(IKubernetes client, string namespaceName, Dictionary<string, string> selector)
        {
            if (selector == null || selector.Count == 0)
            {
                return "El servicio se creó sin selectores (Endpoints manuales requeridos).";
            }

            var selectorString = string.Join(",", selector.Select(kv => $"{kv.Key}={kv.Value}"));

            var pods = await client.CoreV1.ListNamespacedPodAsync(namespaceName, labelSelector: selectorString);

            if (pods.Items.Count == 0)
            {
                return $"El servicio se creó correctamente, pero NINGÚN Pod coincide con el selector '{selectorString}'. El tráfico no llegará a ningún destino por ahora.";
            }

            return null;
        }

        private V1Service BuildServiceObject(CreateServiceDTO dto)
        {
            var labels = new Dictionary<string, string>(dto.Labels)
            {
                { "managed-by", "kube-orchestrator" }
            };

            return new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = dto.Name,
                    NamespaceProperty = dto.Namespace,
                    Labels = labels,
                    Annotations = dto.Annotations
                },
                Spec = new V1ServiceSpec
                {
                    Selector = dto.Selector,

                    Type = dto.Type,

                    Ports = dto.Ports.Select(p => new V1ServicePort
                    {
                        Name = p.Name,
                        Port = p.Port,
                        TargetPort = p.TargetPort.HasValue ? p.TargetPort.Value : null,
                        NodePort = p.NodePort,
                        Protocol = p.Protocol
                    }).ToList(),

                    SessionAffinity = dto.SessionAffinity,
                    InternalTrafficPolicy = dto.InternalTrafficPolicy,
                    ExternalTrafficPolicy = (dto.Type == "NodePort" || dto.Type == "LoadBalancer")
                                            ? dto.ExternalTrafficPolicy : null,

                    IpFamilyPolicy = dto.IpFamilyPolicy
                }
            };
        }
        public async Task DeleteAsync(int userId, string clusterId, string namespaceName, string name)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);

            try
            {
                var client = clientConnection.GetClient(userId, clusterId);
                await client.CoreV1.DeleteNamespacedServiceAsync(name, namespaceName);

                auditService.LogOperation(userName, clusterName, $"DeleteService: {name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"DeleteService: {name}", false, ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<V1Service>> GetAllAsync(int userId, string clusterId, string namespaceName)
        {
            var client = clientConnection.GetClient(userId, clusterId);
            var list = await client.CoreV1.ListNamespacedServiceAsync(namespaceName);
            return list.Items;
        }

        public async Task<V1Service?> GetByNameAsync(int userId, string clusterId, string namespaceName, string name)
        {
            var client = clientConnection.GetClient(userId, clusterId);
            try
            {
                var svc = await client.CoreV1.ReadNamespacedServiceAsync(name, namespaceName);
                if (svc?.Metadata != null) svc.Metadata.ManagedFields = null;
                return svc;
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}