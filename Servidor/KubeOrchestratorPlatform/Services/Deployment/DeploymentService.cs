using k8s;
using k8s.Models;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs;
using Servidor.KubeOrchestratorPlatform.Models.DTOs.Workloads.Deployments.CreateDeploymentSpecs.ContainerSpecs;
using Servidor.KubeOrchestratorPlatform.Services.Audit;
using Servidor.KubeOrchestratorPlatform.Services.Cluster;
using Servidor.KubeOrchestratorPlatform.Services.Connection;

namespace Servidor.KubeOrchestratorPlatform.Services.Deployment
{
    public class DeploymentService : IDeploymentService
    {
        private readonly IKubernetesUserClientConnection clientConnection;
        private readonly IClusterService clusterService;
        private readonly IAuditService auditService;

        public DeploymentService(
            IKubernetesUserClientConnection connection,
            IClusterService clusterStore,
            IAuditService audit)
        {
            clientConnection = connection;
            clusterService = clusterStore;
            auditService = audit;
        }

        public async Task<IEnumerable<V1Deployment>> GetAllAsync(int userId, string clusterId, string namespaceName)
        {
            var client = clientConnection.GetClient(userId, clusterId);
            var list = await client.AppsV1.ListNamespacedDeploymentAsync(namespaceName);
            return list.Items;
        }

        public async Task<V1Deployment?> GetByNameAsync(int userId, string clusterId, string namespaceName, string name)
        {
            var client = clientConnection.GetClient(userId, clusterId);
            try
            {
                var deployment = await client.AppsV1.ReadNamespacedDeploymentAsync(name, namespaceName);

                if (deployment?.Metadata != null)
                {
                    deployment.Metadata.ManagedFields = null;
                }

                return deployment;
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task CreateAsync(int userId, string clusterId, CreateDeploymentDTO dto)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);

            try
            {
                var client = clientConnection.GetClient(userId, clusterId);

                var deployment = BuildDeploymentObject(dto);

                await client.AppsV1.CreateNamespacedDeploymentAsync(deployment, dto.Namespace);

                auditService.LogOperation(userName, clusterName, $"CreateDeployment: {dto.Name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"CreateDeployment: {dto.Name}", false, ex.Message);
                throw;
            }
        }
        private V1Deployment BuildDeploymentObject(CreateDeploymentDTO dto)
        {
            var matchLabels = new Dictionary<string, string> { { "app", dto.Name } };

            var metadataLabels = new Dictionary<string, string>(dto.Labels)
            {
                { "app", dto.Name },
                { "managed-by", "kube-orchestrator" }
            };

            return new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = dto.Name,
                    NamespaceProperty = dto.Namespace,
                    Labels = metadataLabels,
                    Annotations = dto.Annotations
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = dto.Replicas,
                    Selector = new V1LabelSelector { MatchLabels = matchLabels },
                    Strategy = MapStrategy(dto.Strategy),
                    MinReadySeconds = dto.MinReadySeconds,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta { Labels = metadataLabels },
                        Spec = new V1PodSpec
                        {
                            ServiceAccountName = dto.ServiceAccountName,
                            NodeSelector = dto.NodeSelector.Count > 0 ? dto.NodeSelector : null,
                            ImagePullSecrets = dto.ImagePullSecrets.Select(x => new V1LocalObjectReference()).ToList(),

                            InitContainers = dto.InitContainers.Select(MapContainer).ToList(),
                            Containers = dto.Containers.Select(MapContainer).ToList(),

                            Volumes = dto.Volumes.Select(MapVolume).ToList()
                        }
                    }
                }
            };
        }
        private V1DeploymentStrategy MapStrategy(DeploymentStrategyDTO dto)
        {
            var strategy = new V1DeploymentStrategy { Type = dto.Type };

            if (dto.Type == "RollingUpdate" && dto.RollingUpdate != null)
            {
                strategy.RollingUpdate = new V1RollingUpdateDeployment
                {
                    MaxUnavailable = dto.RollingUpdate.MaxUnavailable,
                    MaxSurge = dto.RollingUpdate.MaxSurge
                };
            }
            return strategy;
        }
        private V1Container MapContainer(ContainerDTO dto)
        {
            return new V1Container
            {
                Name = dto.Name,
                Image = dto.Image,
                ImagePullPolicy = dto.ImagePullPolicy,
                Command = dto.Command.Any() ? dto.Command : null,
                Args = dto.Args.Any() ? dto.Args : null,

                Ports = dto.Ports.Select(p => new V1ContainerPort
                {
                    Name = p.Name,
                    ContainerPort = p.ContainerPort,
                    HostPort = p.HostPort,
                    HostIP = p.HostIP,
                    Protocol = p.Protocol
                }).ToList(),

                Env = MapEnvVars(dto.Environment),
                EnvFrom = dto.EnvFrom.Select(MapEnvFrom).ToList(),

                Resources = new V1ResourceRequirements
                {
                    Requests = ConvertToResourceQuantity(dto.Resources.Requests),
                    Limits = ConvertToResourceQuantity(dto.Resources.Limits)
                },

                LivenessProbe = MapProbe(dto.LivenessProbe),
                ReadinessProbe = MapProbe(dto.ReadinessProbe),
                StartupProbe = MapProbe(dto.StartupProbe),

                SecurityContext = dto.SecurityContext != null ? new V1SecurityContext
                {
                    Privileged = dto.SecurityContext.Privileged,
                    ReadOnlyRootFilesystem = dto.SecurityContext.ReadOnlyRootFilesystem,
                    RunAsUser = dto.SecurityContext.RunAsUser != null ? (long)dto.SecurityContext.RunAsUser : null,
                    RunAsGroup = dto.SecurityContext.RunAsGroup != null ? (long)dto.SecurityContext.RunAsGroup : null
                } : null,

                VolumeMounts = dto.VolumeMounts.Select(vm => new V1VolumeMount
                {
                    Name = vm.Name,
                    MountPath = vm.MountPath,
                    ReadOnlyProperty = vm.ReadOnly,
                    SubPath = vm.SubPath
                }).ToList()
            };
        }
        private V1Volume MapVolume(VolumeDTO dto)
        {
            var vol = new V1Volume { Name = dto.Name };

            switch (dto.Type)
            {
                case "EmptyDir":
                    vol.EmptyDir = new V1EmptyDirVolumeSource();
                    break;
                case "PersistentVolumeClaim":
                    vol.PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource { ClaimName = dto.SourceName };
                    break;
                case "ConfigMap":
                    vol.ConfigMap = new V1ConfigMapVolumeSource { Name = dto.SourceName };
                    break;
                case "Secret":
                    vol.Secret = new V1SecretVolumeSource { SecretName = dto.SourceName };
                    break;
            }
            return vol;
        }
        private List<V1EnvVar> MapEnvVars(Dictionary<string, string> envs)
        {
            return envs.Select(kv => new V1EnvVar { Name = kv.Key, Value = kv.Value }).ToList();
        }
        private V1EnvFromSource MapEnvFrom(EnvFromSourceDTO dto)
        {
            return new V1EnvFromSource
            {
                ConfigMapRef = !string.IsNullOrEmpty(dto.ConfigMapRef) ? new V1ConfigMapEnvSource { Name = dto.ConfigMapRef } : null,
                SecretRef = !string.IsNullOrEmpty(dto.SecretRef) ? new V1SecretEnvSource { Name = dto.SecretRef } : null
            };
        }
        private V1Probe? MapProbe(ProbeDTO? dto)
        {
            if (dto == null) return null;

            var probe = new V1Probe
            {
                InitialDelaySeconds = dto.InitialDelaySeconds,
                PeriodSeconds = dto.PeriodSeconds,
                TimeoutSeconds = dto.TimeoutSeconds,
                SuccessThreshold = dto.SuccessThreshold,
                FailureThreshold = dto.FailureThreshold
            };

            if (dto.HttpGet != null)
            {
                probe.HttpGet = new V1HTTPGetAction
                {
                    Path = dto.HttpGet.Path,
                    Port = dto.HttpGet.Port,
                    HttpHeaders = dto.HttpGet.HttpHeaders?
                        .Select(h => new V1HTTPHeader { Name = h.Name, Value = h.Value })
                        .ToList()
                };
            }
            else if (dto.TcpSocket != null)
            {
                probe.TcpSocket = new V1TCPSocketAction
                {
                    Port = dto.TcpSocket.Port
                };
            }
            else if (dto.Exec != null)
            {
                probe.Exec = new V1ExecAction
                {
                    Command = dto.Exec.Command
                };
            }

            return probe;
        }
        private Dictionary<string, ResourceQuantity> ConvertToResourceQuantity(Dictionary<string, string> resources)
        {
            var result = new Dictionary<string, ResourceQuantity>();
            foreach (var kv in resources)
            {
                result[kv.Key] = new ResourceQuantity(kv.Value);
            }
            return result;
        }
        public async Task DeleteAsync(int userId, string clusterId, string namespaceName, string name)
        {
            var (userName, clusterName) = clusterService.GetClusterContext(userId, clusterId);

            try
            {
                var client = clientConnection.GetClient(userId, clusterId);
                await client.AppsV1.DeleteNamespacedDeploymentAsync(name, namespaceName);
                
                auditService.LogOperation(userName, clusterName, $"DeleteDeployment: {name}", true);
            }
            catch (Exception ex)
            {
                auditService.LogOperation(userName, clusterName, $"DeleteDeployment: {name}", false, ex.Message);
                throw;
            }
        }
    }
}