using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Web.Helpers;
using MatinPower.Server.Models;
using MatinPower.Infrastructure.Filter;
using TicketManagement.Infrastructure;

namespace MatinPower.Infrastructure
{
    //[ApiAuthorize]
    public class BaseManageController<T> : BaseController where T : class, new()
    {
        protected virtual Expression<Func<T, bool>> SearchPredicate { get; }
        protected virtual string SortExpression => "Id";
        protected virtual SortDirection SortDirection => SortDirection.Descending;
        protected virtual PaginationFilter PaginationFilter
        {
            get
            {
                int? pageNumber = UrlArgument<int?>("pageNumber");
                int? pageSize = UrlArgument<int?>("pageSize");
                PaginationFilter filter = new PaginationFilter { PageNumber = pageNumber ?? 1, PageSize = pageSize ?? 10 };
                return filter;
            }
        }

        [Route("[controller]/GetHeaderDescription")]
        [HttpGet]
        public virtual string? GetHeaderDescription() => null;

        protected virtual PaginationResult GridDataSource(Expression<Func<T, bool>> predicate, PaginationFilter filter)
        {
            var result = Repository<T>.GetList(filter, predicate, SortExpression, SortDirection);
            return new PaginationResult(result.Item1, filter.PageNumber, filter.PageSize, result.Item2, result.Item3, result.Item4);
        }

        [Route("[controller]/List")]
        [HttpGet]
        public ExecutionResult List()
        {
            return RunExceptionProof(() => GridDataSource(SearchPredicate, PaginationFilter));
        }

        [Route("[controller]/Detail/{id}")]
        [HttpGet]
        public ExecutionResult Detail(int id)
        {
            return RunExceptionProof(() => PrepareDetail(GetItem(id), id));
        }

        protected virtual T GetItem(int id)
        {
            return id == 0 ? new T() : Repository<T>.GetItemById(id);
        }

        private T PrepareDetail(T item, int id)
        {
            return item;
        }

        [Route("[controller]/Insert")]
        [HttpPost]
        public virtual ExecutionResult Insert([FromBody] T item)
        {
            bool isCanceled = false;
            item = PrepareInsertItem(item, isCanceled);
            if (!ModelState.IsValid)
                return ExecutionResult.Failure;
            return RunExceptionProof(() =>
            {

                if (isCanceled)
                    return "0";
                Repository<T>.InsertItem(item);
                this.DataChanged(item, EntityState.Added);
                return item.GetProperty("Id").ToString();
            });

        }

        [Route("[controller]/Update")]
        [HttpPut]
        public virtual ExecutionResult Update([FromBody] T item)
        {
            if (!ModelState.IsValid)
                return ExecutionResult.Failure;
            return RunExceptionProof(() =>
            {
                item = PrepareUpdateItem(item);
                Repository<T>.UpdateItem(item);
                this.DataChanged(item, EntityState.Modified);
                return item.GetProperty("Id").ToString();
            });

        }

        [Route("[controller]/Delete/{id}")]
        [HttpDelete]
        public virtual ExecutionResult Delete(int id)
        {
            return RunExceptionProof(() =>
            {
                T item = Repository<T>.GetItemById(id);
                item = PrepareDeleteItem(item);
                Repository<T>.DeleteItem(item);
                this.DataChanged(item, EntityState.Deleted);
            });
        }

        protected virtual T PrepareInsertItem(T item, bool isCanceled) => isCanceled ? item : item;

        protected virtual T PrepareUpdateItem(T item) => item;

        protected virtual T PrepareDeleteItem(T item) => item;

        protected virtual void DataChanged(T item, EntityState state) { }

    }
}
