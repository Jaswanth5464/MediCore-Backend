using MediCore.API.Infrastructure.Database;
using MediCore.API.Infrastructure.Database.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediCore.API.Controllers
{
    [ApiController]
    [Route("api/seed")]
    [AllowAnonymous] // Allow anyone to seed during development
    public class SeedController : ControllerBase
    {
        private readonly MediCoreDbContext _context;

        public SeedController(MediCoreDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            await DbSeeder.SeedAsync(_context);
            return Ok(new { success = true, message = "Database seeded with realistic data" });
        }
    }
}
