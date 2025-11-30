-- =============================================
-- Script: Khởi tạo dữ liệu cho hệ thống phân quyền
-- Mô tả: Tạo các chức năng và quyền mẫu cho hệ thống
-- =============================================

USE [QLBanSach]
GO

-- =============================================
-- 1. Thêm các Chức Năng (ChucNang)
-- =============================================
PRINT 'Thêm các Chức Năng...'

-- Kiểm tra và thêm chức năng nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN001')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN001', N'Quản Lý Nhân Viên')

IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN002')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN002', N'Quản Lý Sách')

IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN003')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN003', N'Quản Lý Đơn Hàng')

IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN004')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN004', N'Quản Lý Khách Hàng')

IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN005')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN005', N'Quản Lý Mã Giảm Giá')

IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN006')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN006', N'Xem Thống Kê')

IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN007')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN007', N'Quản Lý Đánh Giá')

IF NOT EXISTS (SELECT 1 FROM ChucNang WHERE MaChucNang = 'CN008')
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES ('CN008', N'Quản Lý Tác Giả & NXB')

PRINT 'Đã thêm các Chức Năng!'

-- =============================================
-- 2. Thêm các Quyền (Quyen)
-- =============================================
PRINT 'Thêm các Quyền...'

-- Quyền cho Quản Lý Nhân Viên
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q001')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q001', N'Xem Danh Sách Nhân Viên', 'CN001')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q002')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q002', N'Thêm Nhân Viên', 'CN001')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q003')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q003', N'Sửa Nhân Viên', 'CN001')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q004')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q004', N'Xóa Nhân Viên', 'CN001')

-- Quyền cho Quản Lý Sách
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q005')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q005', N'Xem Danh Sách Sách', 'CN002')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q006')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q006', N'Thêm Sách', 'CN002')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q007')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q007', N'Sửa Sách', 'CN002')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q008')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q008', N'Xóa Sách', 'CN002')

-- Quyền cho Quản Lý Đơn Hàng
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q009')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q009', N'Xem Đơn Hàng', 'CN003')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q010')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q010', N'Cập Nhật Trạng Thái Đơn Hàng', 'CN003')

-- Quyền cho Quản Lý Khách Hàng
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q011')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q011', N'Xem Khách Hàng', 'CN004')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q012')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q012', N'Quản Lý Khách Hàng', 'CN004')

-- Quyền cho Quản Lý Mã Giảm Giá
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q013')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q013', N'Xem Mã Giảm Giá', 'CN005')

IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q014')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q014', N'Thêm/Sửa/Xóa Mã Giảm Giá', 'CN005')

-- Quyền cho Thống Kê
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q015')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q015', N'Xem Thống Kê', 'CN006')

-- Quyền cho Đánh Giá
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q016')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q016', N'Quản Lý Đánh Giá', 'CN007')

-- Quyền cho Tác Giả & NXB
IF NOT EXISTS (SELECT 1 FROM Quyen WHERE MaQuyen = 'Q017')
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES ('Q017', N'Quản Lý Tác Giả & NXB', 'CN008')

PRINT 'Đã thêm các Quyền!'

-- =============================================
-- 3. Gán Quyền cho Chức Vụ (Nhiều-Nhiều: ChucVu_Quyen)
-- =============================================
PRINT 'Gán Quyền cho Chức Vụ...'

-- Lấy mã chức vụ Quản Lý và Nhân Viên (giả sử đã tồn tại)
DECLARE @MaQuanLy NVARCHAR(50)
DECLARE @MaNhanVien NVARCHAR(50)

SELECT @MaQuanLy = MaCV FROM ChucVu WHERE TenCV LIKE N'%Quản Lý%'
SELECT @MaNhanVien = MaCV FROM ChucVu WHERE TenCV LIKE N'%Nhân Viên%'

-- Cấp TẤT CẢ quyền cho Quản Lý
IF @MaQuanLy IS NOT NULL
BEGIN
    PRINT 'Cấp quyền cho Quản Lý...'
    
    -- Thêm tất cả quyền cho Quản Lý
    INSERT INTO ChucVu_Quyen (MaCV, MaQuyen)
    SELECT @MaQuanLy, MaQuyen 
    FROM Quyen
    WHERE NOT EXISTS (
        SELECT 1 FROM ChucVu_Quyen 
        WHERE MaCV = @MaQuanLy AND MaQuyen = Quyen.MaQuyen
    )
    
    PRINT 'Đã cấp quyền cho Quản Lý!'
END

-- Cấp một số quyền cơ bản cho Nhân Viên
IF @MaNhanVien IS NOT NULL
BEGIN
    PRINT 'Cấp quyền cho Nhân Viên...'
    
    -- Quyền xem và quản lý sách
    IF NOT EXISTS (SELECT 1 FROM ChucVu_Quyen WHERE MaCV = @MaNhanVien AND MaQuyen = 'Q005')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'Q005')
    
    IF NOT EXISTS (SELECT 1 FROM ChucVu_Quyen WHERE MaCV = @MaNhanVien AND MaQuyen = 'Q006')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'Q006')
    
    IF NOT EXISTS (SELECT 1 FROM ChucVu_Quyen WHERE MaCV = @MaNhanVien AND MaQuyen = 'Q007')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'Q007')
    
    -- Quyền xem và cập nhật đơn hàng
    IF NOT EXISTS (SELECT 1 FROM ChucVu_Quyen WHERE MaCV = @MaNhanVien AND MaQuyen = 'Q009')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'Q009')
    
    IF NOT EXISTS (SELECT 1 FROM ChucVu_Quyen WHERE MaCV = @MaNhanVien AND MaQuyen = 'Q010')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'Q010')
    
    -- Quyền xem mã giảm giá
    IF NOT EXISTS (SELECT 1 FROM ChucVu_Quyen WHERE MaCV = @MaNhanVien AND MaQuyen = 'Q013')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'Q013')
    
    -- Quyền quản lý đánh giá
    IF NOT EXISTS (SELECT 1 FROM ChucVu_Quyen WHERE MaCV = @MaNhanVien AND MaQuyen = 'Q016')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'Q016')
    
    PRINT 'Đã cấp quyền cho Nhân Viên!'
END

-- =============================================
-- 4. Kiểm tra kết quả
-- =============================================
PRINT ''
PRINT '===== KẾT QUẢ KHỞI TẠO ====='
PRINT 'Tổng số Chức Năng: ' + CAST((SELECT COUNT(*) FROM ChucNang) AS VARCHAR(10))
PRINT 'Tổng số Quyền: ' + CAST((SELECT COUNT(*) FROM Quyen) AS VARCHAR(10))
PRINT ''

-- Hiển thị số quyền của từng chức vụ
SELECT 
    cv.TenCV AS [Chức Vụ],
    COUNT(cvq.MaQuyen) AS [Số Quyền]
FROM ChucVu cv
LEFT JOIN ChucVu_Quyen cvq ON cv.MaCV = cvq.MaCV
GROUP BY cv.TenCV

PRINT ''
PRINT 'Hoàn tất khởi tạo hệ thống phân quyền!'
GO
