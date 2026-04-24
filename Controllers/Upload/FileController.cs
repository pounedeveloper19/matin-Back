using MatinPower.Infrastructure;
using MatinPower.Server.Models.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MatinPower.Server.Controllers.Upload;

public class FileController : BaseController
{
    private readonly FileDbContext _fileDb;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    public FileController(FileDbContext fileDb)
    {
        _fileDb = fileDb;
    }

    [HttpPost]
    [Route("[controller]/Upload")]
    public async Task<ExecutionResult> Upload(
        IFormFile file,
        [FromQuery] string? entityType = null,
        [FromQuery] int? entityId = null)
    {
        if (file == null || file.Length == 0)
            return new ExecutionResult(ResultType.Danger, "فایل انتخاب نشده", "لطفاً یک فایل انتخاب کنید.", 400);

        if (file.Length > MaxFileSizeBytes)
            return new ExecutionResult(ResultType.Danger, "فایل بزرگ است", "حداکثر حجم فایل ۱۰ مگابایت است.", 400);

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return new ExecutionResult(ResultType.Danger, "فرمت مجاز نیست", "فرمت‌های مجاز: PDF، Word، Excel، تصویر", 400);

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var data = ms.ToArray();

        int? userId = null;
        var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
        if (int.TryParse(userIdClaim, out var uid)) userId = uid;

        var fileId = Guid.NewGuid();
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        var record = new FileStore
        {
            Id = fileId,
            OriginalName = file.FileName,
            MimeType = file.ContentType,
            Extension = ext,
            SizeBytes = file.Length,
            Data = data,
            UploadedByUserId = userId,
            EntityType = entityType,
            EntityId = entityId,
            UploadedAt = DateTime.Now,
        };

        _fileDb.FileStore.Add(record);
        await _fileDb.SaveChangesAsync();

        return new ExecutionResult(ResultType.Success, "آپلود موفق", null, 200, new
        {
            fileId = fileId,
            originalName = file.FileName,
            sizeBytes = file.Length,
            mimeType = file.ContentType,
        });
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("[controller]/Download/{fileId:guid}")]
    public async Task<IActionResult> Download(Guid fileId)
    {
        var record = await _fileDb.FileStore.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);
        if (record == null || record.Data == null || record.Data.Length == 0)
            return NotFound();

        var mimeType = record.MimeType ?? "application/octet-stream";
        return File(record.Data, mimeType, record.OriginalName);
    }

    [HttpGet]
    [Route("[controller]/Info/{fileId:guid}")]
    public async Task<ExecutionResult> Info(Guid fileId)
    {
        var record = await _fileDb.FileStore.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);
        if (record == null)
            return new ExecutionResult(ResultType.Danger, "فایل یافت نشد", null, 404);

        return new ExecutionResult(ResultType.Success, null, null, 200, new
        {
            fileId = record.Id,
            originalName = record.OriginalName,
            sizeBytes = record.SizeBytes,
            mimeType = record.MimeType,
            uploadedAt = record.UploadedAt,
            entityType = record.EntityType,
            entityId = record.EntityId,
        });
    }

    [HttpDelete]
    [Route("[controller]/Delete/{fileId:guid}")]
    public async Task<ExecutionResult> Delete(Guid fileId)
    {
        var record = await _fileDb.FileStore.FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted);
        if (record == null)
            return new ExecutionResult(ResultType.Danger, "فایل یافت نشد", null, 404);

        record.IsDeleted = true;
        record.DeletedAt = DateTime.Now;
        await _fileDb.SaveChangesAsync();

        return ExecutionResult.Success;
    }
}
