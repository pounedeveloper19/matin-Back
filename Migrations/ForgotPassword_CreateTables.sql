-- ============================================================
-- دیتابیس: MatinPower_Core
-- ============================================================

CREATE TABLE OtpCode (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Mobile     NVARCHAR(20)  NOT NULL,
    Code       NVARCHAR(6)   NOT NULL,
    ExpiresAt  DATETIME      NOT NULL,
    IsUsed     BIT           NOT NULL DEFAULT 0,
    CreatedAt  DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE INDEX IX_OtpCode_Mobile ON OtpCode (Mobile);

-- لاگ تغییر رمز در جدول AuditTrail موجود در MatinPower_Log ذخیره می‌شود (بدون جدول جدید)
