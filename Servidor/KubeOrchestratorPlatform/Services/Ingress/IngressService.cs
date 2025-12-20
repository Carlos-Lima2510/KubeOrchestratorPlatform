using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Network.Ingress;
using Servidor.KubeOrchestratorPlatform.Responses;

namespace Servidor.KubeOrchestratorPlatform.Services.Ingress
{
    public class IngressService : IIngressService
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IClusterService clusterService;
        private readonly IAuditService auditService;

        public IngressService(
            IKubernetesUserClientConnection clientConnection,
            IClusterService clusterService,
            IAuditService auditService)
        {
            this.clientConnection = clientConnection;
            this.clusterService = clusterService;
            this.auditService = auditService;
        }

        public async Task<IEnumerable<V1Ingress>> GetAllAsync(int userId, string clusterId, string namespaceName)
        {
            var client = clientConnection.GetClient(userId, clusterId);
            var list = await client.NetworkingV1.ListNamespacedIngressAsync(namespaceName);
            return list.Items;
        }

        public async Task<IngressServiceResponse> CreateAsync(int userId, string clusterId, CreateIngressDTO dto)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);
            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                var ingress = BuildIngressObject(dto);

                await client.NetworkingV1.CreateNamespacedIngressAsync(ingress, dto.Namespace);
                auditService.LogOperation(userName, clusterName, $"CreateIngress: {dto.Name}", true);

                var warnings = await CheckForMissingServicesAsync(client, dto.Namespace, dto);

                return new IngressServiceResponse
                {
                    Name = dto.Name,
                    Message = "Ingress creado correctamente.",
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"CreateIngress: {dto.Name}", false, ex.Message);
                throw;
            }
        }

        private async Task<List<string>> CheckForMissingServicesAsync(IKubernetes client, string namespaceName, CreateIngressDTO dto)
        {
            var warnings = new List<string>();
            var servicesToCheck = new HashSet<string>();

            if (dto.DefaultBackend?.Service != null)
            {
                servicesToCheck.Add(dto.DefaultBackend.Service.Name);
            }

            foreach (var rule in dto.Rules)
            {
                foreach (var path in rule.Paths)
                {
                    if (path.Backend.Service != null)
                    {
                        servicesToCheck.Add(path.Backend.Service.Name);
                    }
                }
            }

            foreach (var serviceName in servicesToCheck)
            {
                try
                {
                    await client.CoreV1.ReadNamespacedServiceAsync(serviceName, namespaceName);
                }
                catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    warnings.Add($"El Ingress apunta al servicio '{serviceName}', pero este NO EXISTE en el namespace '{namespaceName}'. Dará error 503.");
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return warnings;
        }

        private V1Ingress BuildIngressObject(CreateIngressDTO dto)
        {
            return new V1Ingress
            {
                Metadata = new V1ObjectMeta
                {
                    Name = dto.Name,
                    NamespaceProperty = dto.Namespace,
                    Annotations = dto.Annotations,
                    Labels = new Dictionary<string, string> { { "managed-by", "kube-orchestrator" } }
                },
                Spec = new V1IngressSpec
                {
                    IngressClassName = dto.IngressClassName,
                    DefaultBackend = dto.DefaultBackend != null ? MapBackend(dto.DefaultBackend) : null,
                    Tls = dto.Tls.Any() ? dto.Tls.Select(t => new V1IngressTLS { Hosts = t.Hosts, SecretName = t.SecretName }).ToList() : null,
                    Rules = dto.Rules.Select(r => new V1IngressRule
                    {
                        Host = r.Host,
                        Http = new V1HTTPIngressRuleValue
                        {
                            Paths = r.Paths.Select(p => new V1HTTPIngressPath
                            {
                                Path = p.Path,
                                PathType = p.PathType.ToString(),
                                Backend = MapBackend(p.Backend)
                            }).ToList()
                        }
                    }).ToList()
                }
            };
        }

        private V1IngressBackend MapBackend(IngressBackendDTO dto)
        {
            var backend = new V1IngressBackend();

            if (dto.Service != null)
            {
                backend.Service = new V1IngressServiceBackend
                {
                    Name = dto.Service.Name,
                    Port = new V1ServiceBackendPort
                    {
                        Number = dto.Service.Port.Number,
                        Name = dto.Service.Port.Name
                    }
                };
            }
            else if (dto.Resource != null)
            {
                backend.Resource = new V1TypedLocalObjectReference
                {
                    ApiGroup = dto.Resource.ApiGroup,
                    Kind = dto.Resource.Kind,
                    Name = dto.Resource.Name
                };
            }
            else
            {
                throw new ArgumentException("El backend debe ser un Service o un Resource.");
            }

            return backend;
        }

        public async Task DeleteAsync(int userId, string clusterId, string namespaceName, string name)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);
            try
            {
                var client = clientConnection.GetClient(userId, clusterId);
                await client.NetworkingV1.DeleteNamespacedIngressAsync(name, namespaceName);
                auditService.LogOperation(userName, clusterName, $"DeleteIngress: {name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"DeleteIngress: {name}", false, ex.Message);
                throw;
            }
        }
    }
}
