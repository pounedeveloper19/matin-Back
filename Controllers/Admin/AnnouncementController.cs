using MatinPower.Infrastructure;
using MatinPower.Infrastructure.Filter;
using MatinPower.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TicketManagement.Infrastructure;

namespace MatinPower.Server.Controllers.Admin
{
    public class AnnouncementController : BaseManageController<Announcement>
    {
        protected override PaginationResult GridDataSource(Expression<Func<Announcement, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<Announcement>.GetSelectiveListWithPaging(i => new
            {
                i.Id,
                i.Title,
                PublishDate = PersianDateConverter.ToPersianDate(i.PublishDate, "yyyy/MM/dd"),
                FinishDate = PersianDateConverter.ToPersianDate(i.FinishDate, "yyyy/MM/dd"),
            }, filter, predicate,
               sortExpression: "PublishDate",
               sortDirection: System.Web.Helpers.SortDirection.Descending);

            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        protected override Expression<Func<Announcement, bool>> SearchPredicate
        {
            get
            {
                Expression<Func<Announcement, bool>> predicate = a => true;
                string? title = UrlArgument<string?>("Search_Title");
                if (!string.IsNullOrEmpty(title))
                    predicate = predicate.AppendCondition(a => a.Title.Contains(title), false);
                return predicate;
            }
        }
    }
}
