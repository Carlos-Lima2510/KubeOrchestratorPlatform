using k8s;
using Microsoft.AspNetCore.Mvc;
using Servidor.KubeOrchestratorPlatform.Kubernetes;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KubeTestController : ControllerBase
    {
        private readonly IKubeConnection kubeConnection;

        public KubeTestController(IKubeConnection kubeConnection)
        {
            this.kubeConnection = kubeConnection;
        }

        [HttpGet("pods")]
        public async Task<IActionResult> GetPods()
        {
            try
            {
                IKubernetes client = kubeConnection.GetClient();

                var pods = await client.CoreV1.ListNamespacedPodAsync(kubeConnection.Namespace);

                return Ok(new
                {
                    Namespace = kubeConnection.Namespace,
                    Count = pods.Items.Count,
                    Pods = pods.Items.Select(p => p.Metadata.Name)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stack = ex.StackTrace });
            }
        }
        // GET: api/<KubeTestController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<KubeTestController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<KubeTestController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<KubeTestController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<KubeTestController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
