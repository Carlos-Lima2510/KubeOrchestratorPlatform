using k8s;
using Servidor.KubeOrchestratorPlatform.Models.DTOs;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Metrics
{
    public class MetricsService : IMetricsService
    {
        private readonly IKubernetesUserClientConnection clientConnection;

        public MetricsService(IKubernetesUserClientConnection clientConnection)
        {
            this.clientConnection = clientConnection;
        }

        public async Task<IEnumerable<ClusterMetricsDTO>> GetClusterMetricsAsync(int userId, string clusterId)
        {
            var client = clientConnection.GetClient(userId, clusterId);

            try
            {
                var nodes = await client.CoreV1.ListNodeAsync();
                var nodeMetricsList = await client.GetKubernetesNodesMetricsAsync();

                double totalCpuCapacity = 0;
                double totalMemCapacity = 0;
                double totalCpuUsage = 0;
                double totalMemUsage = 0;

                foreach (var node in nodes.Items)
                {
                    totalCpuCapacity += (double)node.Status.Capacity["cpu"].ToDecimal();
                    totalMemCapacity += (double)node.Status.Capacity["memory"].ToDecimal();
                }

                foreach (var metric in nodeMetricsList.Items)
                {
                    totalCpuUsage += (double)metric.Usage["cpu"].ToDecimal();
                    totalMemUsage += (double)metric.Usage["memory"].ToDecimal(); 
                }

                double cpuPercentage = totalCpuCapacity > 0 ? (totalCpuUsage / totalCpuCapacity) * 100 : 0;
                double memPercentage = totalMemCapacity > 0 ? (totalMemUsage / totalMemCapacity) * 100 : 0;

                return new List<ClusterMetricsDTO>
                {
                    new ClusterMetricsDTO
                    {
                        Timestamp = DateTime.Now.ToString("HH:mm:ss"),
                        CpuUsage = Math.Round(cpuPercentage, 2),
                        MemoryUsage = Math.Round(memPercentage, 2)
                    }
                };
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException("METRICS_SERVER_MISSING");
            }
        }

        public async Task<IDictionary<string, PodMetricsDTO>> GetPodMetricsAsync(int userId, string clusterId, string namespaceName)
        {
            var client = clientConnection.GetClient(userId, clusterId);

            try
            {
                var podMetricsList = await client.GetKubernetesPodsMetricsByNamespaceAsync(namespaceName);

                return podMetricsList.Items.ToDictionary(
                    pod => pod.Metadata.Name,
                    pod =>
                    {
                        double cpuCores = pod.Containers.Sum(c => (double)c.Usage["cpu"].ToDecimal());
                        double memBytes = pod.Containers.Sum(c => (double)c.Usage["memory"].ToDecimal());

                        return new PodMetricsDTO
                        {
                            Cpu = $"{Math.Round(cpuCores * 1000)}m",
                            Mem = $"{Math.Round(memBytes / (1024.0 * 1024.0))}Mi"
                        };
                    });
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new Dictionary<string, PodMetricsDTO>();
            }
        }
    }
}