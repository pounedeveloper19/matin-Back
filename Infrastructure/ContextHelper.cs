
using Enumerable = System.Linq.Enumerable;

namespace MatinPower.Infrastructure
{
    public static class ContextHelper
    {
        //public static IEnumerable<SiteMap> GetUserSiteMaps()
        //{
        //    var userId = new UseContext(new HttpContextAccessor()).GetUserId();
        //    if (userId == 0)
        //        return Enumerable.Empty<SiteMap>();

        //    // Get role IDs for user
        //    var roleIds = Repository<UserRole>.GetSelectiveList(i => i.RoleId, i => i.UserId == userId);
        //    if (!roleIds.Any())
        //        return Enumerable.Empty<SiteMap>();

        //    // Get siteMap IDs for those roles - filter in SQL, not in memory
        //    var siteMapIds = Repository<SiteMapRole>.GetSelectiveList(
        //        m => m.SiteMapId,
        //        m => roleIds.Contains(m.RoleId));

        //    // Get siteMaps - filter by IDs in SQL
        //    var result = Repository<SiteMap>.GetSelectiveList(
        //        m => new SiteMap
        //        {
        //            Id = m.Id,
        //            Title = m.Title,
        //            IsAccessible = true,
        //            ControlKey = m.ControlKey,
        //            IsInMenu = m.IsInMenu,
        //            Indexer = m.Indexer,
        //            ParentId = m.ParentId,
        //            PhysicalPath = m.PhysicalPath
        //        },
        //        i => i.IsInMenu && siteMapIds.Contains(i.Id));

        //    return result;
        //}

        //public static T GetApplicationSetting<T>(string key)
        //{
        //    ApplicationSetting? setting = Repository<ApplicationSetting>.GetLast(s => s.FieldName == key);
        //    if (setting is not null && !string.IsNullOrEmpty(setting.Value))
        //    {
        //        return setting.Value.ChangeType<T>();
        //    }
        //    return default!;
        //}
    }
}