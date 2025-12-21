using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Observability;
using Servidor.KubeOrchestratorPlatform.Services.Connection;
using Servidor.KubeOrchestratorPlatform.Services.Metrics;

namespace Servidor.KubeOrchestratorPlatform.Services.Observability
{
    public class NetworkMapService : INetworkMapService
    {
        private readonly IKubernetesUserClientConnection clientConnection;

        public NetworkMapService(IKubernetesUserClientConnection clientConnection)
        {
            this.clientConnection = clientConnection;
        }

        public async Task<NetworkGraphDTO> GetNetworkMapAsync(int userId, string clusterId, string namespaceName)
        {
            var client = clientConnection.GetClient(userId, clusterId);
            var graph = new NetworkGraphDTO();

            var ingressesTask = client.NetworkingV1.ListNamespacedIngressAsync(namespaceName);
            var servicesTask = client.CoreV1.ListNamespacedServiceAsync(namespaceName);
            var podsTask = client.CoreV1.ListNamespacedPodAsync(namespaceName);
            var policiesTask = client.NetworkingV1.ListNamespacedNetworkPolicyAsync(namespaceName);
            var podsMetricsTask = client.GetKubernetesPodsMetricsAsync();

            await Task.WhenAll(ingressesTask, servicesTask, podsTask, policiesTask, podsMetricsTask);

            var ingresses = ingressesTask.Result.Items;
            var services = servicesTask.Result.Items;
            var pods = podsTask.Result.Items;
            var policies = policiesTask.Result.Items;
            var metrics = podsMetricsTask.Status == TaskStatus.RanToCompletion ? podsMetricsTask.Result.Items : new List<PodMetrics>();

            foreach (var ing in ingresses)
            {
                var ingId = $"ing-{ing.Uid()}";
                graph.Nodes.Add(new GraphNodeDTO
                {
                    Id = ingId,
                    Label = ing.Name(),
                    Type = "ingress",
                    Status = "Active",
                    Metadata = new Dictionary<string, string> { { "hosts", string.Join(", ", ing.Spec.Rules?.Select(r => r.Host) ?? Array.Empty<string>()) } }
                });

                foreach (var rule in ing.Spec.Rules ?? new List<V1IngressRule>())
                {
                    foreach (var path in rule.Http?.Paths ?? new List<V1HTTPIngressPath>())
                    {
                        var targetSvcName = path.Backend?.Service?.Name;
                        if (string.IsNullOrEmpty(targetSvcName)) continue;

                        var targetSvc = services.FirstOrDefault(s => s.Name() == targetSvcName);
                        if (targetSvc != null)
                        {
                            bool isBlocked = false;
                            var firstPod = pods.FirstOrDefault(p => IsMatch(p.Metadata.Labels, targetSvc.Spec.Selector));
                            if (firstPod != null) isBlocked = IsConnectionBlocked(null, firstPod, policies, fromIngress: true);

                            graph.Edges.Add(new GraphEdgeDTO
                            {
                                Id = $"edge-{ing.Uid()}-{targetSvc.Uid()}",
                                Source = ingId,
                                Target = $"svc-{targetSvc.Uid()}",
                                Relation = "routes-to",
                                Label = path.Path ?? "/",
                                Metadata = new Dictionary<string, string> { { "isBlocked", isBlocked.ToString().ToLower() } }
                            });
                        }
                    }
                }
            }

            foreach (var svc in services)
            {
                var svcId = $"svc-{svc.Uid()}";
                graph.Nodes.Add(new GraphNodeDTO
                {
                    Id = svcId,
                    Label = svc.Name(),
                    Type = "service",
                    Status = "Active",
                    Metadata = new Dictionary<string, string> { { "clusterIP", svc.Spec.ClusterIP }, { "type", svc.Spec.Type } }
                });

                if (svc.Spec.Selector == null || !svc.Spec.Selector.Any()) continue;

                var matchingPods = pods.Where(p =>
                            p.Metadata.Labels != null &&
                            svc.Spec.Selector != null &&
                            svc.Spec.Selector.All(sel => p.Metadata.Labels.ContainsKey(sel.Key) && p.Metadata.Labels[sel.Key] == sel.Value)
                        );

                foreach (var pod in matchingPods)
                {
                    var podId = $"pod-{pod.Uid()}";
                    var isIsolated = policies.Any(p => IsPodTargetedByPolicy(pod, p));

                    if (!graph.Nodes.Any(n => n.Id == podId))
                    {
                        var podMetric = metrics.FirstOrDefault(m => m.Metadata.Name == pod.Name());
                        var cpuUsage = podMetric?.Containers.Select(c => c.Usage["cpu"]).ToList();
                        var memUsage = podMetric?.Containers.Select(c => c.Usage["memory"]).ToList();

                        graph.Nodes.Add(new GraphNodeDTO
                        {
                            Id = podId,
                            Label = pod.Name(),
                            Type = "pod",
                            Status = pod.Status.Phase,
                            Metadata = new Dictionary<string, string>
                            {
                                { "cpu", FormatCpuUsage(cpuUsage)},
                                { "mem", FormatMemoryUsage(cpuUsage) },
                                { "ip", pod.Status.PodIP },
                                { "isIsolated", isIsolated.ToString().ToLower() }
                            }
                        });
                    }

                    bool blocked = IsConnectionBlocked(svc.Metadata.Labels, pod, policies);

                    graph.Edges.Add(new GraphEdgeDTO
                    {
                        Id = $"edge-{svc.Uid()}-{pod.Uid()}",
                        Source = svcId,
                        Target = podId,
                        Relation = "selects",
                        Label = string.Join(", ", svc.Spec.Ports?.Select(p => p.Port.ToString()) ?? Array.Empty<string>()),
                        Metadata = new Dictionary<string, string> { { "isBlocked", blocked.ToString().ToLower() } }
                    });
                }
            }

            return graph;
        }

        private string FormatCpuUsage(List<ResourceQuantity>? usages)
        {
            if (usages == null || !usages.Any()) return "0m";

            decimal totalNanocores = 0;
            foreach (var usage in usages)
            {
                totalNanocores += usage.ToDecimal();
            }

            var millicores = (totalNanocores / 1000000).ToString("F0");
            return $"{millicores}m";
        }

        private string FormatMemoryUsage(List<ResourceQuantity>? usages)
        {
            if (usages == null || !usages.Any()) return "0Mi";

            decimal totalBytes = 0;
            foreach (var usage in usages)
            {
                totalBytes += usage.ToDecimal();
            }

            var mib = (totalBytes / (1024 * 1024)).ToString("F0");
            return $"{mib}Mi";
        }

        private bool IsPodTargetedByPolicy(V1Pod pod, V1NetworkPolicy policy)
        {
            return IsMatch(pod.Metadata.Labels, policy.Spec.PodSelector.MatchLabels);
        }

        private bool IsConnectionBlocked(IDictionary<string, string> sourceLabels, V1Pod targetPod, IList<V1NetworkPolicy> policies, bool fromIngress = false)
        {
            var relevantPolicies = policies.Where(p => IsPodTargetedByPolicy(targetPod, p)).ToList();

            if (!relevantPolicies.Any()) return false;

            foreach (var policy in relevantPolicies)
            {
                if (policy.Spec.Ingress == null) continue;

                foreach (var rule in policy.Spec.Ingress)
                {
                    if (rule.FromProperty == null || !rule.FromProperty.Any()) return false;

                    foreach (var peer in rule.FromProperty)
                    {
                        if (fromIngress && peer.IpBlock != null) return false;

                        if (sourceLabels != null && peer.PodSelector != null)
                        {
                            if (IsMatch(sourceLabels, peer.PodSelector.MatchLabels)) return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool IsMatch(IDictionary<string, string> target, IDictionary<string, string> selector)
        {
            if (selector == null || !selector.Any()) return true;
            if (target == null) return false;
            return selector.All(s => target.ContainsKey(s.Key) && target[s.Key] == s.Value);
        }
    }
}
