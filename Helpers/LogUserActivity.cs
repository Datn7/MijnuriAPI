using Microsoft.AspNetCore.Mvc.Filters;
using MijnuriAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MijnuriAPI.Helpers
{
    public class LogUserActivity : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            //get user id from token nameid
            var userId = int.Parse(resultContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);

            //get idatingrepo service
            var repo = resultContext.HttpContext.RequestServices.GetService<IDatingRepository>();

            //get user
            var user = await repo.GetUser(userId);

            //set last active to now
            user.LastActive = DateTime.Now;

            //save changes
            await repo.SaveAll();
        }
    }
}
