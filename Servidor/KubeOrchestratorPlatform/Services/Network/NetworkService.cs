using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network;
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
        public async Task CreateAsync(int userId, string clusterId, CreateServiceDTO dto)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);

            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                var service = BuildServiceObject(dto);

                await client.CoreV1.CreateNamespacedServiceAsync(service, dto.Namespace);

                auditService.LogOperation(userName, clusterName, $"CreateService: {dto.Name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"CreateService: {dto.Name}", false, ex.Message);
                throw;
            }
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
        public Task DeleteAsync(int userId, string clusterId, string namespaceName, string name)
        {
            throw new NotImplementedException();
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