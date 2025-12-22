using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Graph;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Ingress;
using Servidor.KubeOrchestratorPlatform.Services.Network;
using Servidor.KubeOrchestratorPlatform.Models.Enums;
using System.Security.Cryptography.X509Certificates;

namespace Servidor.KubeOrchestratorPlatform.Services.Graph
{
    public class GraphService : IGraphService
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IIngressService ingressService;
        private readonly INetworkService networkService;

        public GraphService(
            IIngressService ingressService,
            INetworkService networkService,
            IKubernetesUserClientConnection clientConnection)
        {
            this.ingressService = ingressService;
            this.networkService = networkService;
            this.clientConnection = clientConnection;
        }

        public async Task<GraphDTO> GetFullTopologyAsync(int userId, string clusterId, string namespaceName)
        {
            var graph = new GraphDTO();
            var client = clientConnection.GetClient(userId, clusterId);

            var ingresses = await ingressService.GetAllAsync(userId, clusterId, namespaceName);
            var services = await networkService.GetAllAsync(userId, clusterId, namespaceName);
            var pods = await client.CoreV1.ListNamespacedPodAsync(namespaceName);

            foreach (var ingress in ingresses)
            {
                var node = BuildIngressNode(ingress);
                await EnrichTlsInfo(node, ingress, client, namespaceName);

                graph.Nodes.Add(node);

                foreach (var rule in ingress.Spec.Rules ?? new List<V1IngressRule>())
                {
                    foreach (var path in rule.Http?.Paths ?? new List<V1HTTPIngressPath>())
                    {
                        ProcessIngressPath(graph, node.Id, path, services);
                    }
                }
            }

            foreach (var svc in services)
            {
                var svcNodeId = $"service-{svc.Metadata.Uid}";
                if (graph.Nodes.Any(n => n.Id == svcNodeId)) continue;

                var matchingPods = FilterPodsByService(svc, pods.Items);
                var healthStatus = CalculateServiceHealth(matchingPods);

                graph.Nodes.Add(new GraphNodeDTO
                {
                    Id = svcNodeId,
                    Type = ResourceTypeEnum.Service,
                    Label = svc.Metadata.Name,
                    Status = healthStatus,
                    Metadata = new GraphNodeMetadata { Ip = svc.Spec.ClusterIP }
                });

                foreach (var pod in matchingPods)
                {
                    var podNodeId = $"pod-{pod.Metadata.Uid}";
                    AddPodNode(graph, pod, podNodeId);
                    graph.Edges.Add(new GraphEdgeDTO
                    {
                        Id = $"edge-{svc.Metadata.Uid}-{pod.Metadata.Uid}",
                        SourceId = svcNodeId,
                        TargetId = podNodeId,
                        Status = healthStatus
                    });
                }
            }

            return graph;
        }

        private void AddPodNode(GraphDTO graph, V1Pod pod, string podNodeId)
        {
            string status = pod.Status.Phase switch
            {
                "Running" => "Allowed", 
                "Pending" => "Degraded", 
                "Failed" => "Broken",
                _ => "Broken"
            };

            if (pod.Status.ContainerStatuses != null)
            {
                foreach (var container in pod.Status.ContainerStatuses)
                {
                    if (container.State.Waiting != null || (container.State.Terminated != null && container.State.Terminated.ExitCode != 0))
                    {
                        status = "Broken";
                        break;
                    }
                }
            }

            graph.Nodes.Add(new GraphNodeDTO
            {
                Id = podNodeId,
                Type = ResourceTypeEnum.Pod,
                Label = pod.Metadata.Name,
                Status = status,
                Metadata = new GraphNodeMetadata
                {
                    Ip = pod.Status.PodIP ?? "-",
                    NodeName = pod.Spec.NodeName ?? "Unassigned",
                    Ports = pod.Spec.Containers
                        .SelectMany(c => c.Ports ?? new List<V1ContainerPort>())
                        .Select(p => p.ContainerPort.ToString())
                        .ToList()
                }
            });
        }

        private string CalculateServiceHealth(IEnumerable<V1Pod> matchingPods)
        {
            if (!matchingPods.Any()) return "Mismatch";
            if (matchingPods.All(p => p.Status.Phase == "Running")) return "Allowed";
            if (matchingPods.Any(p => p.Status.Phase == "Failed" || p.Status.Phase == "Unknown")) return "Broken";
            return "Degraded";
        }

        private IEnumerable<V1Pod> FilterPodsByService(V1Service svc, IList<V1Pod> allPods)
        {
            var selector = svc.Spec.Selector;
            if (selector == null || !selector.Any()) return new List<V1Pod>();

            return allPods.Where(pod => selector.All(s =>
                pod.Metadata.Labels != null &&
                pod.Metadata.Labels.ContainsKey(s.Key) &&
                pod.Metadata.Labels[s.Key] == s.Value)).ToList();
        }

        private void ProcessIngressPath(GraphDTO graph, string ingressId, V1HTTPIngressPath path, IEnumerable<V1Service> services)
        {
            var svcName = path.Backend?.Service?.Name;
            var targetSvc = services.FirstOrDefault(s => s.Metadata.Name == svcName);

            if (targetSvc != null)
            {
                graph.Edges.Add(new GraphEdgeDTO
                {
                    Id = $"edge-{ingressId}-{targetSvc.Metadata.Uid}",
                    SourceId = ingressId,
                    TargetId = $"service-{targetSvc.Metadata.Uid}",
                    Status = "Allowed"
                });
            }
            else
            {
                var errorNodeId = $"error-404-{svcName}";
                graph.Nodes.Add(new GraphNodeDTO { Id = errorNodeId, Type = ResourceTypeEnum.Error404, Label = "Ruta No Encontrada", Status = "404 Error" });
                graph.Edges.Add(new GraphEdgeDTO { SourceId = ingressId, TargetId = errorNodeId, Status = "404 Error" });
            }
        }

        private async Task EnrichTlsInfo(GraphNodeDTO node, V1Ingress ingress, IKubernetes client, string namespaceName)
        {
            var tlsSecretName = ingress.Spec.Tls?.FirstOrDefault()?.SecretName;
            if (string.IsNullOrEmpty(tlsSecretName)) return;

            try
            {
                var secret = await client.CoreV1.ReadNamespacedSecretAsync(tlsSecretName, namespaceName);
                if (secret.Data.TryGetValue("tls.crt", out byte[]? certData))
                {
                    var cert = new X509Certificate2(certData);
                    var metadata = node.Metadata!;
                    metadata.Issuer = cert.Issuer;
                    metadata.ExpirationDate = cert.NotAfter;

                    if (cert.NotAfter < DateTime.UtcNow)
                        node.Status = "Expired";
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private GraphNodeDTO BuildIngressNode(V1Ingress ingress)
        {
            var publicIp = ingress.Status?.LoadBalancer?.Ingress?.FirstOrDefault()?.Ip ?? "-";

            return new GraphNodeDTO
            {
                Id = $"ingress-{ingress.Metadata.Uid}",
                Type = ResourceTypeEnum.Ingress,
                Label = ingress.Metadata.Name,
                Status = "Allowed",
                Metadata = new GraphNodeMetadata
                {
                    Ip = publicIp
                }
            };
        }
    }
}