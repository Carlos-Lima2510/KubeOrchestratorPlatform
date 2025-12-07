using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Pods;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Pods
{
    public class PodService : IPodService
    {
        private readonly IKubernetesUserClientConnection connection;

        public PodService(IKubernetesUserClientConnection connection)
        {
            this.connection = connection;
        }

        public async Task<IEnumerable<PodDTO>> GetByDeploymentAsync(int userId, string clusterId, string namespaceName, string deploymentName)
        {
            try
            {
                var client = connection.GetClient(userId, clusterId);

                V1Deployment deployment;
                try
                {
                    deployment = await client.AppsV1.ReadNamespacedDeploymentAsync(deploymentName, namespaceName);
                }
                catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new List<PodDTO>();
                }

                if (deployment?.Spec?.Selector?.MatchLabels == null || !deployment.Spec.Selector.MatchLabels.Any())
                {
                    return new List<PodDTO>();
                }

                var selector = string.Join(",", deployment.Spec.Selector.MatchLabels.Select(kv => $"{kv.Key}={kv.Value}"));

                var pods = await client.CoreV1.ListNamespacedPodAsync(namespaceName, labelSelector: selector);

                return pods.Items.Select(MapToDTO);
            }
            catch (Exception)
            {
                return new List<PodDTO>();
            }
        }

        private PodDTO MapToDTO(V1Pod pod)
        {
            var status = pod.Status.Phase;
            string message = "";
            int restarts = 0;

            if (pod.Status.ContainerStatuses != null)
            {
                foreach (var container in pod.Status.ContainerStatuses)
                {
                    restarts += container.RestartCount;

                    if (container.State.Waiting != null)
                    {
                        status = container.State.Waiting.Reason;
                        message = container.State.Waiting.Message;
                    }
                    else if (container.State.Terminated != null && container.State.Terminated.ExitCode != 0)
                    {
                        status = "Error";
                        message = $"Exit Code: {container.State.Terminated.ExitCode} ({container.State.Terminated.Reason})";
                    }
                }
            }

            var age = DateTime.UtcNow - pod.Metadata.CreationTimestamp?.ToUniversalTime();
            string ageStr = age.HasValue ? $"{(int)age.Value.TotalMinutes}m" : "0m";
            if (age.HasValue && age.Value.TotalHours > 1) ageStr = $"{(int)age.Value.TotalHours}h";
            if (age.HasValue && age.Value.TotalDays > 1) ageStr = $"{(int)age.Value.TotalDays}d";

            return new PodDTO
            {
                Name = pod.Metadata.Name,
                Namespace = pod.Metadata.NamespaceProperty,
                Status = status,
                Message = message,
                Restarts = restarts,
                Node = pod.Spec.NodeName ?? "Pending",
                Ip = pod.Status.PodIP ?? "-",
                Age = ageStr
            };
        }
    }
}