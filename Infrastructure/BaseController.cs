using Microsoft.AspNetCore.Mvc;

namespace MatinPower.Infrastructure
{
    public class BaseController : ControllerBase
    {

        public T UrlArgument<T>(string key)
        {
            if (HttpContext.Request.Query[key].Count < 0)
                return default(T);
            return (T)HttpContext.Request.Query[key].ToString().ChangeType(typeof(T));
        }
        public ExecutionResult RunExceptionProof(Action action, ExecutionResult successResult = null)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                //Utilities.LogException(exception);
                return this.HandleException(exception);
            }
            return successResult ?? ExecutionResult.Success;
        }
        public ExecutionResult RunExceptionProof(Func<object> function, ExecutionResult successResult = null)
        {
            successResult = successResult ?? ExecutionResult.Success;
            try
            {
                successResult.Result = function();
            }
            catch (Exception exception)
            {
               // Utilities.LogException(exception);
                return this.HandleException(exception);
            }
            return successResult ?? ExecutionResult.Success;
        }

        protected ExecutionResult HandleException(Exception exception)
        {
            string message = "";
            string caption;
            string innerMsg = exception.InnerException?.Message ?? exception.Message;

            if (innerMsg.Contains("The DELETE statement conflicted with the REFERENCE constraint"))
            {
                message = "به علت وجود اطلاعات وابسته امکان حذف وجود ندارد";
                caption = "عملیات حذف با شکست مواجه شد";
            }
            else if (innerMsg.Contains("Violation of UNIQUE KEY constraint"))
            {
                message = "به علت وجود اطلاعات تکراری امکان ذخیره وجود ندارد";
                caption = "عملیات ذخیره با شکست مواجه شد";
            }
            else if (innerMsg.Contains("Data manipulation is not valid"))
            {
                message = "به علت وجود موارد استفاده از اطلاعات در سامانه امکان تغییر وجود ندارد";
                caption = "عملیات مجاز نمی باشد";
            }
            else if (innerMsg.Contains("the deadlock victim"))
            {
                message = "به علت وجود ترافیک سرور امکان انجام عملیات موقتا امکان پذیر نمی باشد، لطفا مجددا تلاش نمائید";
                caption = "سرور در دسترس نمی باشد";
            }
            else if (innerMsg.Contains("Timeout expired"))
            {
                message = "به علت وجود ترافیک سرور امکان انجام عملیات موقتا امکان پذیر نمی باشد، لطفا مجددا تلاش نمائید";
                caption = "سرور در دسترس نمی باشد";
            }
            else
            {
                message = "";
                caption = "عملیات با شکست مواجه شد";
            }
            return new ExecutionResult(ResultType.Danger, caption, message, 400);
        }

    }
    public class ExecutionResult
    {
        public ResultType Type { get; set; }
        public int Code { get; set; }
        public string Caption { get; set; }
        public string Message { get; set; }
        public object Result { get; set; }

        public ExecutionResult()
        {

        }

        public ExecutionResult(ResultType type, string caption, string message, int code, object result = null)
        {
            Type = type;
            Code = code;
            Caption = caption;
            Message = message;
            Result = result;

        }
        public static ExecutionResult Success => new ExecutionResult(ResultType.Success, "اجرای موفق", "عملیات با موفقیت اجرا شد.", 200);
        public static ExecutionResult Failure => new ExecutionResult(ResultType.Danger, "خطای ناشناخته", "عملیات با شکست مواجه شد.", 400);
    }

    public enum ResultType
    {
        Success,
        Warning,
        Danger,
        Info
    }
    public class PaginationResult
    {
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalPages { get; }
        public int TotalRecords { get; }
        public int FilteredCount { get; }
        public object Data { get; }
        public PaginationResult(object result, int pageNumber, int pageSize, int totalRecords, int totalPages, int filteredCount)
        {
            this.PageNumber = pageNumber;
            this.PageSize = pageSize;
            this.Data = result;
            this.TotalRecords = totalRecords;
            this.TotalPages = totalPages;
            this.FilteredCount = filteredCount;
        }
    }
}