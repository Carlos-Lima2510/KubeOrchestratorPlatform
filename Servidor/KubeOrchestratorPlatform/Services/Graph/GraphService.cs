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

                    if (!graph.Nodes.Any(n => n.Id == podNodeId))
                    {
                        podMetrics.TryGetValue(pod.Metadata.Name, out var newPodMetric);
                        AddPodNode(graph, pod, podNodeId, newPodMetric?.Cpu, newPodMetric?.Mem);
                    }

                    bool isBlocked = EvaluateNetworkPolicy(pod, networkPolicies.Items);

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

            foreach (var pod in pods.Items)
            {
                var podNodeId = $"pod-{pod.Metadata.Uid}";
                if (graph.Nodes.Any(n => n.Id == podNodeId)) continue;

                podMetrics.TryGetValue(pod.Metadata.Name, out var orphanPodMetric);
                AddPodNode(graph, pod, podNodeId, orphanPodMetric?.Cpu, orphanPodMetric?.Mem);
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
                "Succeeded" => "Allowed",
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

                if (status == "Allowed" && pod.Status.Phase != "Succeeded" && pod.Status.ContainerStatuses.Any(c => !c.Ready))
                {
                    status = "Degraded";
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
            var pods = matchingPods.ToList();
            if (!pods.Any()) return "Mismatch";

            bool allRunningAndReady = pods.All(p =>
                p.Status.Phase == "Running" &&
                (p.Status.ContainerStatuses == null || p.Status.ContainerStatuses.All(c => c.Ready)));
            if (allRunningAndReady) return "Allowed";

            if (pods.Any(p => p.Status.Phase == "Failed" || p.Status.Phase == "Unknown")) return "Broken";
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

            if (targetSvc != null && !ServiceExposesPort(targetSvc, path.Backend?.Service?.Port))
            {
                var backendPort = path.Backend?.Service?.Port;
                var portLabel = backendPort?.Number?.ToString() ?? backendPort?.Name ?? "?";
                var errorNodeId = $"error-404-{svcName}-port-{portLabel}";

                graph.Nodes.Add(new GraphNodeDTO { Id = errorNodeId, Type = ResourceTypeEnum.Error404, Label = $"Puerto {portLabel} No Encontrado en {svcName}", Status = "404 Error" });
                graph.Edges.Add(new GraphEdgeDTO { SourceId = ingressId, TargetId = errorNodeId, Status = "404 Error" });
                return;
            }

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

        private bool ServiceExposesPort(V1Service svc, V1ServiceBackendPort? backendPort)
        {
            if (backendPort == null) return true;

            var ports = svc.Spec?.Ports;
            if (ports == null || !ports.Any()) return false;

            if (backendPort.Number.HasValue)
            {
                return ports.Any(p => p.Port == backendPort.Number.Value);
            }

            if (!string.IsNullOrEmpty(backendPort.Name))
            {
                return ports.Any(p => p.Name == backendPort.Name);
            }

            return true;
        }

        private async Task EnrichTlsSecurity(GraphNodeDTO node, V1Ingress ingress, IKubernetes client, string namespaceName)
        {
            var tlsBlocks = ingress.Spec.Tls;
            if (tlsBlocks == null || !tlsBlocks.Any()) return;

            node.Metadata ??= new GraphNodeMetadata();

            string? worstStatus = null;
            string? worstIssuer = null;
            DateTime? worstExpiration = null;

            foreach (var tlsBlock in tlsBlocks)
            {
                if (string.IsNullOrEmpty(tlsBlock.SecretName)) continue;

                var (blockStatus, issuerText, expirationDate) = await EvaluateTlsBlock(tlsBlock, client, namespaceName);

                if (worstStatus == null || TlsStatusSeverity(blockStatus) > TlsStatusSeverity(worstStatus))
                {
                    worstStatus = blockStatus;
                    worstIssuer = issuerText;
                    worstExpiration = expirationDate;
                }
            }

            if (worstStatus != null)
            {
                node.Status = worstStatus;
                node.Metadata.Issuer = worstIssuer;
                node.Metadata.ExpirationDate = worstExpiration;
            }
        }

        private int TlsStatusSeverity(string status) => status switch
        {
            "Incompatible" => 2,
            "Expired" => 1,
            _ => 0
        };

        private async Task<(string status, string? issuer, DateTime? expiration)> EvaluateTlsBlock(V1IngressTLS tlsBlock, IKubernetes client, string namespaceName)
        {
            try
            {
                var secret = await client.CoreV1.ReadNamespacedSecretAsync(tlsBlock.SecretName, namespaceName);

                if (!secret.Data.TryGetValue("tls.crt", out byte[]? certData))
                {
                    return ("Allowed", null, null);
                }

                try
                {
                    var cert = new X509Certificate2(certData);
                    var fechaDeExpiracion = cert.NotAfter;

                    if (fechaDeExpiracion < DateTime.UtcNow)
                    {
                        return ("Expired", cert.Issuer, fechaDeExpiracion);
                    }

                    var declaredHosts = tlsBlock.Hosts ?? new List<string>();
                    var certHosts = ExtractCertHostnames(cert);

                    bool hostMatches = declaredHosts.Count == 0 || certHosts.Count == 0 ||
                        declaredHosts.Any(h => certHosts.Any(ch => HostnamePatternMatches(ch, h)));

                    if (!hostMatches)
                    {
                        return ("Incompatible", $"Certificado no cubre el host declarado ({string.Join(", ", declaredHosts)}); cubre: {string.Join(", ", certHosts)}", fechaDeExpiracion);
                    }

                    return ("Allowed", cert.Issuer, fechaDeExpiracion);
                }
                catch (Exception)
                {
                    return ("Incompatible", "Error: Formato no soportado", null);
                }
            }
            catch (Exception)
            {
                return ("Incompatible", "Error: Sin acceso a Secretos", null);
            }
        }

        private List<string> ExtractCertHostnames(X509Certificate2 cert)
        {
            var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var sanExtension = cert.Extensions["2.5.29.17"];
            if (sanExtension != null)
            {
                var formatted = sanExtension.Format(false);
                foreach (var part in formatted.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = part.Trim();
                    var idx = trimmed.IndexOf('=');
                    if (idx < 0) continue;

                    var key = trimmed.Substring(0, idx).Trim();
                    var value = trimmed.Substring(idx + 1).Trim();
                    if (key.Contains("DNS", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                    {
                        hosts.Add(value);
                    }
                }
            }

            if (hosts.Count == 0)
            {
                var cn = cert.GetNameInfo(X509NameType.DnsName, false);
                if (!string.IsNullOrEmpty(cn)) hosts.Add(cn);
            }

            return hosts.ToList();
        }

        private bool HostnamePatternMatches(string certPattern, string requestedHost)
        {
            if (string.Equals(certPattern, requestedHost, StringComparison.OrdinalIgnoreCase)) return true;

            if (certPattern.StartsWith("*."))
            {
                var suffix = certPattern.Substring(1);
                if (requestedHost.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = requestedHost.Substring(0, requestedHost.Length - suffix.Length);
                    return prefix.Length > 0 && !prefix.Contains('.');
                }
            }

            return false;
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