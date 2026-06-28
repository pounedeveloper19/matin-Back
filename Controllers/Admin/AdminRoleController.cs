using MatinPower.Infrastructure;
using MatinPower.Server.Models;
using MatinPower.Server.Models.Body;
using Microsoft.AspNetCore.Mvc;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    [Route("[controller]")]
    public class AdminRoleController : BaseController
    {
        [HttpGet("List")]
        public ExecutionResult List() =>
            RunExceptionProof(() =>
                Repository<Role>.Query(db =>
                    (object)db.Roles
                        .OrderBy(r => r.Id)
                        .Select(r => new { r.Id, r.Title, r.Description })
                        .ToList()));

        [HttpPost("Create")]
        public ExecutionResult Create([FromBody] RoleFormRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return new ExecutionResult(ResultType.Danger, "خطا", "عنوان نقش الزامی است.", 400);

            return RunExceptionProof(() =>
                Repository<Role>.InsertItem(new Role { Title = request.Title, Description = request.Description }));
        }

        [HttpPut("Update")]
        public ExecutionResult Update([FromBody] RoleFormRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return new ExecutionResult(ResultType.Danger, "خطا", "عنوان نقش الزامی است.", 400);

            var role = Repository<Role>.GetLast(r => r.Id == request.Id);
            if (role == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "نقش یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                role.Title       = request.Title;
                role.Description = request.Description;
                Repository<Role>.UpdateItem(role);
            });
        }

        [HttpDelete("Delete/{id}")]
        public ExecutionResult Delete(int id)
        {
            var role = Repository<Role>.GetLast(r => r.Id == id);
            if (role == null)
                return new ExecutionResult(ResultType.Danger, "خطا", "نقش یافت نشد.", 404);

            return RunExceptionProof(() =>
            {
                Repository<SiteMapRole>.ExecuteCommand(db =>
                {
                    var siteMapRoles = db.SiteMapRoles.Where(sr => sr.RoleId == id).ToList();
                    db.SiteMapRoles.RemoveRange(siteMapRoles);
                    var userRoles = db.UserRoles.Where(ur => ur.RoleId == id).ToList();
                    db.UserRoles.RemoveRange(userRoles);
                    db.SaveChanges();
                });
                Repository<Role>.DeleteItem(role);
            });
        }

        [HttpGet("GetPermissions/{roleId}")]
        public ExecutionResult GetPermissions(int roleId) =>
            RunExceptionProof(() =>
                Repository<SiteMapRole>.Query(db =>
                    (object)db.SiteMapRoles
                        .Where(sr => sr.RoleId == roleId)
                        .Select(sr => sr.SiteMapId)
                        .ToList()));

        [HttpPut("SetPermissions")]
        public ExecutionResult SetPermissions([FromBody] SetPermissionsRequest request) =>
            RunExceptionProof(() =>
            {
                Repository<SiteMapRole>.ExecuteCommand(db =>
                {
                    var existing = db.SiteMapRoles.Where(sr => sr.RoleId == request.RoleId).ToList();
                    db.SiteMapRoles.RemoveRange(existing);
                    foreach (var siteMapId in request.SiteMapIds)
                        db.SiteMapRoles.Add(new SiteMapRole { RoleId = request.RoleId, SiteMapId = siteMapId });
                    db.SaveChanges();
                });
            });
    }
}
