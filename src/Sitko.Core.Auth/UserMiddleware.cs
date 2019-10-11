using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Auth
{
    public class UserMiddleware
    {
        private RequestDelegate _next;

        public UserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (int.TryParse(context.User.Claims.FirstOrDefault(claim => claim.Type == "id")?.Value,
                    out var userId) && userId > 0)
            {
                var userFlags = context.User.Claims.Where(claim => claim.Type == "userFlag")
                    .Select(claim => claim.Value).ToArray();
                context.Features.Set(new UserFeature(new User(userId, userFlags)));
            }
            await _next.Invoke(context);
        }
    }
}
