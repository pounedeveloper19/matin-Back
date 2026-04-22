using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NLog.Filters;
using System.IdentityModel.Tokens.Jwt;
using TicketManagement.Infrastructure;

namespace MatinPower.Infrastructure.Filter
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Skip authorization for Swagger endpoints
            var path = context.HttpContext.Request.Path.Value?.ToLower() ?? "";
            if (path.StartsWith("/swagger") || path.StartsWith("/api/swagger"))
            {
                return;
            }

            if (Authorize(context))
            {
                return;
            }
            else
                HandleUnauthorizedRequest(context);
        }

        protected void HandleUnauthorizedRequest(AuthorizationFilterContext actionContext)
        {
            if (actionContext == null)
                throw new ArgumentNullException("actionContext", "Null actionContext");
            actionContext.Result = new JsonResult(new { message = "Unauthorized", StatusCode = StatusCodes.Status401Unauthorized });
        }

        private bool Authorize(AuthorizationFilterContext actionContext)
        {
            try
            {
                var auth = new UseContext(new HttpContextAccessor()).GetTokenHeader();
                if (string.IsNullOrEmpty(auth))
                    return false;
                var handler = new JwtSecurityTokenHandler();
                var tokenRead = handler.ReadToken(auth) as JwtSecurityToken;
                return tokenRead.ValidTo > DateTime.Now;
            }
            catch (Exception ex)
            {
                Utilities.LogException(ex);
                return false;
            }
        }

    }
    public class InternetAccessAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (!Utilities.InternetAccessUsers())
                throw new Exception("denaid");
        }
    }
}
