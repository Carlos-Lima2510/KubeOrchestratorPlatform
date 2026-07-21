using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Graph;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Ingress;
using Servidor.KubeOrchestratorPlatform.Services.Network;
using Servidor.KubeOrchestratorPlatform.Models.Enums;
using Servidor.KubeOrchestratorPlatform.Services.Metrics;
using System.Security.Cryptography.X509Certificates;

namespace Servidor.KubeOrchestratorPlatform.Services.Graph
{
    public class GraphService : IGraphService
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IIngressService ingressService;
        private readonly INetworkService networkService;
        private readonly IMetricsService metricsService;

        public GraphService(
            IIngressService ingressService,
            INetworkService networkService,
            IKubernetesUserClientConnection clientConnection,
            IMetricsService metricsService)
        {
            this.ingressService = ingressService;
            this.networkService = networkService;
            this.clientConnection = clientConnection;
            this.metricsService = metricsService;
        }

        public async Task<GraphDTO> GetFullTopologyAsync(int userId, string clusterId, string namespaceName)
        {
            var graph = new GraphDTO();
            var client = clientConnection.GetClient(userId, clusterId);

            var ingresses = await ingressService.GetAllAsync(userId, clusterId, namespaceName);
            var services = await networkService.GetAllAsync(userId, clusterId, namespaceName);
            var pods = await client.CoreV1.ListNamespacedPodAsync(namespaceName);
            var networkPolicies = await client.NetworkingV1.ListNamespacedNetworkPolicyAsync(namespaceName);
            var podMetrics = await GetPodMetricsSafeAsync(userId, clusterId, namespaceName);

            foreach (var ingress in ingresses)
            {
                var node = BuildIngressNode(ingress);
                await EnrichTlsSecurity(node, ingress, client, namespaceName);

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

                    podMetrics.TryGetValue(pod.Metadata.Name, out var podMetric);

                    bool isBlocked = EvaluateNetworkPolicy(pod, networkPolicies.Items);

                    AddPodNode(graph, pod, podNodeId, podMetric?.Cpu, podMetric?.Mem);

                    graph.Edges.Add(new GraphEdgeDTO
                    {
                        Id = $"edge-{svc.Metadata.Uid}-{pod.Metadata.Uid}",
                        SourceId = svcNodeId,
                        TargetId = podNodeId,
                        Status = isBlocked ? "Blocked" : healthStatus,
                        Metadata = new { isBlocked = isBlocked.ToString().ToLower() }
                    });
                }
            }

            return graph;
        }

        private async Task<IDictionary<string, PodMetricsDTO>> GetPodMetricsSafeAsync(int userId, string clusterId, string namespaceName)
        {
            try
            {
                return await metricsService.GetPodMetricsAsync(userId, clusterId, namespaceName);
            }
            catch (Exception)
            {
                return new Dictionary<string, PodMetricsDTO>();
            }
        }

        private bool EvaluateNetworkPolicy(V1Pod pod, IList<V1NetworkPolicy> policies)
        {
            if (!policies.Any()) return false;

            var applicablePolicies = policies
                .Where(p => SelectorMatches(p.Spec.PodSelector, pod.Metadata.Labels))
                .Where(p => p.Spec.PolicyTypes == null || p.Spec.PolicyTypes.Contains("Ingress"))
                .ToList();

            if (!applicablePolicies.Any()) return false;

            bool hasUnrestrictedRule = applicablePolicies.Any(p =>
                p.Spec.Ingress != null &&
                p.Spec.Ingress.Any(rule => rule.FromProperty == null || !rule.FromProperty.Any())
            );

            return !hasUnrestrictedRule;
        }

        private bool SelectorMatches(V1LabelSelector podSelector, IDictionary<string, string> labels)
        {
            if (podSelector.MatchLabels == null) return true;
            return podSelector.MatchLabels.All(l => labels != null && labels.ContainsKey(l.Key) && labels[l.Key] == l.Value);
        }

        private void AddPodNode(GraphDTO graph, V1Pod pod, string podNodeId, string? cpu, string? mem)
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
                    Cpu = cpu ?? "0m",
                    Mem = mem ?? "0Mi",
                    Ports = pod.Spec.Containers
                        .SelectMany(c => c.Ports ?? new List<V1ContainerPort>())
                        .Select(p => p.ContainerPort.ToString())
                        .ToList()
                }
            });
        }

        private string CalculateServiceHealth(IEnumerable<V1Pod> matchingPods)
        {
            if (!matchingPods.Any()) return "Mismatch"; // Servicio huérfano
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

        private async Task EnrichTlsSecurity(GraphNodeDTO node, V1Ingress ingress, IKubernetes client, string namespaceName)
        {
            var tlsSecretName = ingress.Spec.Tls?.FirstOrDefault()?.SecretName;
            if (string.IsNullOrEmpty(tlsSecretName)) return;

            try
            {
                node.Metadata ??= new GraphNodeMetadata();

                var secret = await client.CoreV1.ReadNamespacedSecretAsync(tlsSecretName, namespaceName);

                if (secret.Data.TryGetValue("tls.crt", out byte[]? certData))
                {
                    try
                    {
                        var cert = new X509Certificate2(certData);
                        var fechaDeExpiracion = cert.NotAfter;

                        node.Metadata.Issuer = cert.Issuer;
                        node.Metadata.ExpirationDate = fechaDeExpiracion;

                        if (fechaDeExpiracion < DateTime.UtcNow)
                        {
                            node.Status = "Expired";
                        }
                        else
                        {
                            node.Status = "Allowed";
                        }
                    }
                    catch (Exception ex)
                    {
                        node.Status = "Incompatible";
                        node.Metadata.Issuer = "Error: Formato no soportado";
                    }
                }
            }
            catch (Exception ex)
            {
                node.Status = "Incompatible";
                node.Metadata.Issuer = "Error: Sin acceso a Secretos";
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