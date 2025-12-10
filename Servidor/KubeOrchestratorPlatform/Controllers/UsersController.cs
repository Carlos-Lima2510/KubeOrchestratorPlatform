using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Servidor.KubeOrchestratorPlatform.Database.Data;

namespace Servidor.KubeOrchestratorPlatform.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")] 
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext dbContext;

        public UsersController(AppDbContext context)
        {
            dbContext = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var users = await dbContext.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.CreatedAt,
                        u.LastLoginAt
                    })
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await dbContext.Users.FindAsync(id);
                if (user == null) return NotFound(new { Message = "Usuario no encontrado" });

                dbContext.Users.Remove(user);
                await dbContext.SaveChangesAsync();

                return Ok(new { Message = "Usuario eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}