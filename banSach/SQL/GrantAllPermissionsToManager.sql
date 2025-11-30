-- =============================================
-- Script: Cấp Đầy Đủ Quyền Cho Quản Lý
-- Mô tả: Sau khi bỏ logic bypass quyền cho Quản Lý,
--        phải chạy script này để cấp quyền cho Quản Lý
-- =============================================

USE [QLBanSach]
GO

PRINT '========================================='
PRINT 'CẤP QUYỀN CHO QUẢN LÝ'
PRINT '========================================='
PRINT ''

-- Lấy mã chức vụ Quản Lý
DECLARE @MaQuanLy NVARCHAR(50)
SELECT @MaQuanLy = MaCV FROM ChucVu WHERE TenCV LIKE N'%Quản Lý%'

IF @MaQuanLy IS NULL
BEGIN
    PRINT '❌ KHÔNG TÌM THẤY CHỨC VỤ QUẢN LÝ!'
    PRINT 'Vui lòng kiểm tra bảng ChucVu'
    RETURN
END

PRINT '✅ Tìm thấy chức vụ Quản Lý: ' + @MaQuanLy
PRINT ''

-- Đếm số quyền hiện tại
DECLARE @QuyenHienTai INT
SELECT @QuyenHienTai = COUNT(*) 
FROM ChucVu_Quyen 
WHERE MaCV = @MaQuanLy

PRINT 'Số quyền hiện tại: ' + CAST(@QuyenHienTai AS VARCHAR(10))

-- Đếm tổng số quyền trong hệ thống
DECLARE @TongQuyen INT
SELECT @TongQuyen = COUNT(*) FROM Quyen

PRINT 'Tổng số quyền trong hệ thống: ' + CAST(@TongQuyen AS VARCHAR(10))
PRINT ''

-- Nếu đã có đủ quyền rồi thì không làm gì
IF @QuyenHienTai >= @TongQuyen
BEGIN
    PRINT '✅ Quản Lý đã có đầy đủ quyền rồi!'
    PRINT ''
    
    -- Hiển thị danh sách quyền
    PRINT 'Danh sách quyền:'
    SELECT 
        q.MaQuyen, 
        q.TenQuyen,
        cn.TenChucNang
    FROM ChucVu_Quyen cvq
    JOIN Quyen q ON cvq.MaQuyen = q.MaQuyen
    JOIN ChucNang cn ON q.MaChucNang = cn.MaChucNang
    WHERE cvq.MaCV = @MaQuanLy
    ORDER BY q.MaQuyen
    
    RETURN
END

-- Cấp TẤT CẢ quyền cho Quản Lý
PRINT '⏳ Đang cấp quyền cho Quản Lý...'
PRINT ''

BEGIN TRY
    BEGIN TRANSACTION
    
    -- Cấp các quyền chưa có
    INSERT INTO ChucVu_Quyen (MaCV, MaQuyen)
    SELECT @MaQuanLy, q.MaQuyen 
    FROM Quyen q
    WHERE NOT EXISTS (
        SELECT 1 FROM ChucVu_Quyen 
        WHERE MaCV = @MaQuanLy AND MaQuyen = q.MaQuyen
    )
    
    DECLARE @SoQuyenThemMoi INT = @@ROWCOUNT
    
    COMMIT TRANSACTION
    
    PRINT '✅ Đã cấp ' + CAST(@SoQuyenThemMoi AS VARCHAR(10)) + ' quyền mới cho Quản Lý'
    PRINT ''
    
    -- Kiểm tra lại
    SELECT @QuyenHienTai = COUNT(*) 
    FROM ChucVu_Quyen 
    WHERE MaCV = @MaQuanLy
    
    PRINT '📊 KẾT QUẢ:'
    PRINT '   - Tổng số quyền hiện tại: ' + CAST(@QuyenHienTai AS VARCHAR(10)) + '/' + CAST(@TongQuyen AS VARCHAR(10))
    
    IF @QuyenHienTai = @TongQuyen
    BEGIN
        PRINT '   - ✅ Quản Lý đã có ĐẦY ĐỦ quyền!'
    END
    ELSE
    BEGIN
        PRINT '   - ⚠️ Quản Lý vẫn thiếu ' + CAST((@TongQuyen - @QuyenHienTai) AS VARCHAR(10)) + ' quyền'
    END
    
    PRINT ''
    PRINT '========================================='
    PRINT 'DANH SÁCH QUYỀN CỦA QUẢN LÝ'
    PRINT '========================================='
    
    -- Hiển thị danh sách quyền theo chức năng
    SELECT 
        cn.TenChucNang AS [Chức Năng],
        q.MaQuyen AS [Mã Quyền],
        q.TenQuyen AS [Tên Quyền],
        CASE 
            WHEN cvq.MaQuyen IS NOT NULL THEN N'✅ Có'
            ELSE N'❌ Không'
        END AS [Trạng Thái]
    FROM Quyen q
    JOIN ChucNang cn ON q.MaChucNang = cn.MaChucNang
    LEFT JOIN ChucVu_Quyen cvq ON cvq.MaQuyen = q.MaQuyen AND cvq.MaCV = @MaQuanLy
    ORDER BY cn.MaChucNang, q.MaQuyen
    
    PRINT ''
    PRINT '✅ HOÀN TẤT!'
    PRINT ''
    PRINT '⚠️ LƯU Ý:'
    PRINT '   - Logout và Login lại để load quyền mới'
    PRINT '   - Kiểm tra Session["UserPermissions"] đã được cập nhật'
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    
    PRINT ''
    PRINT '❌ LỖI XẢY RA:'
    PRINT '   Lỗi: ' + ERROR_MESSAGE()
    PRINT '   Line: ' + CAST(ERROR_LINE() AS VARCHAR(10))
    
END CATCH

GO

PRINT ''
PRINT '========================================='
PRINT 'THỐNG KÊ QUYỀN THEO CHỨC VỤ'
PRINT '========================================='

-- Thống kê tổng quan
SELECT 
    cv.TenCV AS [Chức Vụ],
    COUNT(cvq.MaQuyen) AS [Số Quyền],
    (SELECT COUNT(*) FROM Quyen) AS [Tổng Quyền],
    CASE 
        WHEN COUNT(cvq.MaQuyen) = (SELECT COUNT(*) FROM Quyen) THEN N'✅ Đầy đủ'
        ELSE N'⚠️ Thiếu ' + CAST(((SELECT COUNT(*) FROM Quyen) - COUNT(cvq.MaQuyen)) AS VARCHAR(10)) + N' quyền'
    END AS [Trạng Thái]
FROM ChucVu cv
LEFT JOIN ChucVu_Quyen cvq ON cv.MaCV = cvq.MaCV
GROUP BY cv.TenCV, cv.MaCV
ORDER BY cv.TenCV

GO
