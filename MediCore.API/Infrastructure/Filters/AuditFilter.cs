using Microsoft.AspNetCore.Mvc.Filters;
using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Auth.Models;
using System.Security.Claims;

namespace MediCore.API.Infrastructure.Filters
{
    public class AuditFilter : IAsyncActionFilter
    {
        private readonly MediCoreDbContext _context;

        public AuditFilter(MediCoreDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            var method = context.HttpContext.Request.Method;
            if (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH")
            {
                if (resultContext.Exception == null)
                {
                    var userIdStr = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    int? userId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);

                    var path = context.HttpContext.Request.Path.ToString();
                    var action = $"{method} {path}";
                    
                    var auditLog = new AuditLog
                    {
                        UserId = userId,
                        Action = action,
                        Module = GetModuleFromPath(path),
                        Details = $"Executed {method} on {path}",
                        IPAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                    };

                    _context.AuditLogs.Add(auditLog);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private string GetModuleFromPath(string path)
        {
            var parts = path.Split('/');
            if (parts.Length > 2) return parts[2];
            return "General";
        }
    }
}
