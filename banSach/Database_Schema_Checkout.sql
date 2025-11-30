-- SQL Script for Creating Address and Discount Tables
-- Execute this script on your database to create the required tables

-- Create DiaChi (Address) table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DiaChi]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DiaChi](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [MaTinhThanh] [nvarchar](100) NULL,
        [TenTinhThanh] [nvarchar](200) NULL,
        [MaQuanHuyen] [nvarchar](100) NULL,
        [TenQuanHuyen] [nvarchar](200) NULL,
        [MaPhuongXa] [nvarchar](100) NULL,
        [TenPhuongXa] [nvarchar](200) NULL,
        [PhiVanChuyen] [decimal](18, 2) NULL,
        [NgayTao] [datetime] NULL,
        CONSTRAINT [PK_DiaChi] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY]
END
GO

-- Create MaGiamGia (Discount Code) table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MaGiamGia]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MaGiamGia](
        [MaCode] [nvarchar](50) NOT NULL,
        [TenChuongTrinh] [nvarchar](200) NULL,
        [PhanTramGiam] [int] NULL,
        [SoTienGiam] [decimal](18, 2) NULL,
        [NgayBatDau] [datetime] NULL,
        [NgayKetThuc] [datetime] NULL,
        [SoLuongMa] [int] NULL,
        [DaSuDung] [int] NULL,
        [TrangThai] [bit] NULL,
        CONSTRAINT [PK_MaGiamGia] PRIMARY KEY CLUSTERED ([MaCode] ASC)
    ) ON [PRIMARY]
END
GO

-- Insert sample discount codes
IF NOT EXISTS (SELECT * FROM [dbo].[MaGiamGia] WHERE MaCode = 'GIAM10')
BEGIN
    INSERT INTO [dbo].[MaGiamGia] (MaCode, TenChuongTrinh, PhanTramGiam, SoTienGiam, NgayBatDau, NgayKetThuc, SoLuongMa, DaSuDung, TrangThai)
    VALUES ('GIAM10', N'Giảm 10% cho đơn hàng', 10, NULL, GETDATE(), DATEADD(MONTH, 3, GETDATE()), 100, 0, 1)
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[MaGiamGia] WHERE MaCode = 'GIAM50K')
BEGIN
    INSERT INTO [dbo].[MaGiamGia] (MaCode, TenChuongTrinh, PhanTramGiam, SoTienGiam, NgayBatDau, NgayKetThuc, SoLuongMa, DaSuDung, TrangThai)
    VALUES ('GIAM50K', N'Giảm 50,000đ cho đơn hàng', NULL, 50000, GETDATE(), DATEADD(MONTH, 3, GETDATE()), 50, 0, 1)
END
GO

IF NOT EXISTS (SELECT * FROM [dbo].[MaGiamGia] WHERE MaCode = 'FREESHIP')
BEGIN
    INSERT INTO [dbo].[MaGiamGia] (MaCode, TenChuongTrinh, PhanTramGiam, SoTienGiam, NgayBatDau, NgayKetThuc, SoLuongMa, DaSuDung, TrangThai)
    VALUES ('FREESHIP', N'Miễn phí vận chuyển', NULL, NULL, GETDATE(), DATEADD(MONTH, 3, GETDATE()), 200, 0, 1)
END
GO

-- Add default shipping fees for provinces (sample data)
-- You can customize this based on your business logic
IF NOT EXISTS (SELECT * FROM [dbo].[DiaChi])
BEGIN
    -- Default shipping fee for major cities
    INSERT INTO [dbo].[DiaChi] (MaTinhThanh, TenTinhThanh, PhiVanChuyen, NgayTao)
    VALUES 
        ('01', N'Hà Nội', 30000, GETDATE()),
        ('79', N'Hồ Chí Minh', 30000, GETDATE()),
        ('48', N'Đà Nẵng', 35000, GETDATE()),
        ('92', N'Cần Thơ', 40000, GETDATE());
END
GO

PRINT 'Database tables created successfully!'
GO
