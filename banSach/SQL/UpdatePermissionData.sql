-- =============================================
-- Script: Cập nhật lại hệ thống phân quyền với mã quyền dựa trên tên
-- Mô tả: Cập nhật các chức năng và quyền với mã có ý nghĩa
-- =============================================

USE [QLBanSach]
GO

BEGIN TRANSACTION

BEGIN TRY
    -- =============================================
    -- 1. XÓA DỮ LIỆU CŨ (Xóa theo thứ tự để tránh lỗi ràng buộc)
    -- =============================================
    PRINT '1. Xóa dữ liệu cũ...'
    
    -- Xóa bảng quan hệ nhiều-nhiều trước
    DELETE FROM ChucVu_Quyen
    PRINT '   - Đã xóa ChucVu_Quyen'
    
    -- Xóa quyền
    DELETE FROM Quyen
    PRINT '   - Đã xóa Quyen'
    
    -- Xóa chức năng
    DELETE FROM ChucNang
    PRINT '   - Đã xóa ChucNang'
    
    PRINT 'Hoàn thành xóa dữ liệu cũ!'
    PRINT ''

    -- =============================================
    -- 2. THÊM CÁC CHỨC NĂNG MỚI
    -- =============================================
    PRINT '2. Thêm các Chức Năng mới...'
    
    INSERT INTO ChucNang (MaChucNang, TenChucNang) VALUES 
        ('NHANVIEN', N'Quản Lý Nhân Viên'),
        ('CHUCVU', N'Quản Lý Chức Vụ'),
        ('SACH', N'Quản Lý Sách'),
        ('LOAISACH', N'Quản Lý Loại Sách'),
        ('TACGIA', N'Quản Lý Tác Giả'),
        ('NXB', N'Quản Lý Nhà Xuất Bản'),
        ('DONHANG', N'Quản Lý Đơn Hàng'),
        ('KHACHHANG', N'Quản Lý Khách Hàng'),
        ('MAGIAMGIA', N'Quản Lý Mã Giảm Giá'),
        ('DANHGIA', N'Quản Lý Đánh Giá'),
        ('THONGKE', N'Xem Thống Kê'),
        ('PHANQUYEN', N'Quản Lý Phân Quyền')
    
    PRINT 'Đã thêm ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' Chức Năng!'
    PRINT ''

    -- =============================================
    -- 3. THÊM CÁC QUYỀN MỚI (MÃ DỰA TRÊN TÊN)
    -- =============================================
    PRINT '3. Thêm các Quyền mới...'
    
    -- ===== QUẢN LÝ NHÂN VIÊN =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('NV_VIEW', N'Xem Danh Sách Nhân Viên', 'NHANVIEN'),
        ('NV_CREATE', N'Thêm Nhân Viên', 'NHANVIEN'),
        ('NV_EDIT', N'Sửa Nhân Viên', 'NHANVIEN'),
        ('NV_DELETE', N'Xóa Nhân Viên', 'NHANVIEN'),
        ('NV_DETAIL', N'Xem Chi Tiết Nhân Viên', 'NHANVIEN')
    
    -- ===== QUẢN LÝ CHỨC VỤ =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('CV_VIEW', N'Xem Danh Sách Chức Vụ', 'CHUCVU'),
        ('CV_CREATE', N'Thêm Chức Vụ', 'CHUCVU'),
        ('CV_EDIT', N'Sửa Chức Vụ', 'CHUCVU'),
        ('CV_DELETE', N'Xóa Chức Vụ', 'CHUCVU'),
        ('CV_DETAIL', N'Xem Chi Tiết Chức Vụ', 'CHUCVU')
    
    -- ===== QUẢN LÝ SÁCH =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('SACH_VIEW', N'Xem Danh Sách Sách', 'SACH'),
        ('SACH_CREATE', N'Thêm Sách', 'SACH'),
        ('SACH_EDIT', N'Sửa Sách', 'SACH'),
        ('SACH_DELETE', N'Xóa Sách', 'SACH'),
        ('SACH_DETAIL', N'Xem Chi Tiết Sách', 'SACH')
    
    -- ===== QUẢN LÝ LOẠI SÁCH =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('LS_VIEW', N'Xem Danh Sách Loại Sách', 'LOAISACH'),
        ('LS_CREATE', N'Thêm Loại Sách', 'LOAISACH'),
        ('LS_EDIT', N'Sửa Loại Sách', 'LOAISACH'),
        ('LS_DELETE', N'Xóa Loại Sách', 'LOAISACH'),
        ('LS_DETAIL', N'Xem Chi Tiết Loại Sách', 'LOAISACH')
    
    -- ===== QUẢN LÝ TÁC GIẢ =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('TG_VIEW', N'Xem Danh Sách Tác Giả', 'TACGIA'),
        ('TG_CREATE', N'Thêm Tác Giả', 'TACGIA'),
        ('TG_EDIT', N'Sửa Tác Giả', 'TACGIA'),
        ('TG_DELETE', N'Xóa Tác Giả', 'TACGIA'),
        ('TG_DETAIL', N'Xem Chi Tiết Tác Giả', 'TACGIA')
    
    -- ===== QUẢN LÝ NHÀ XUẤT BẢN =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('NXB_VIEW', N'Xem Danh Sách Nhà Xuất Bản', 'NXB'),
        ('NXB_CREATE', N'Thêm Nhà Xuất Bản', 'NXB'),
        ('NXB_EDIT', N'Sửa Nhà Xuất Bản', 'NXB'),
        ('NXB_DELETE', N'Xóa Nhà Xuất Bản', 'NXB'),
        ('NXB_DETAIL', N'Xem Chi Tiết Nhà Xuất Bản', 'NXB')
    
    -- ===== QUẢN LÝ ĐỢN HÀNG =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('DH_VIEW', N'Xem Danh Sách Đơn Hàng', 'DONHANG'),
        ('DH_DETAIL', N'Xem Chi Tiết Đơn Hàng', 'DONHANG'),
        ('DH_UPDATE_STATUS', N'Cập Nhật Trạng Thái Đơn Hàng', 'DONHANG'),
        ('DH_DELETE', N'Xóa Đơn Hàng', 'DONHANG'),
        ('DH_EXPORT', N'Xuất Báo Cáo Đơn Hàng', 'DONHANG')
    
    -- ===== QUẢN LÝ KHÁCH HÀNG =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('KH_VIEW', N'Xem Danh Sách Khách Hàng', 'KHACHHANG'),
        ('KH_CREATE', N'Thêm Khách Hàng', 'KHACHHANG'),
        ('KH_EDIT', N'Sửa Khách Hàng', 'KHACHHANG'),
        ('KH_DELETE', N'Xóa Khách Hàng', 'KHACHHANG'),
        ('KH_DETAIL', N'Xem Chi Tiết Khách Hàng', 'KHACHHANG')
    
    -- ===== QUẢN LÝ MÃ GIẢM GIÁ =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('MGG_VIEW', N'Xem Danh Sách Mã Giảm Giá', 'MAGIAMGIA'),
        ('MGG_CREATE', N'Thêm Mã Giảm Giá', 'MAGIAMGIA'),
        ('MGG_EDIT', N'Sửa Mã Giảm Giá', 'MAGIAMGIA'),
        ('MGG_DELETE', N'Xóa Mã Giảm Giá', 'MAGIAMGIA'),
        ('MGG_DETAIL', N'Xem Chi Tiết Mã Giảm Giá', 'MAGIAMGIA')
    
    -- ===== QUẢN LÝ ĐÁNH GIÁ =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('DG_VIEW', N'Xem Danh Sách Đánh Giá', 'DANHGIA'),
        ('DG_DETAIL', N'Xem Chi Tiết Đánh Giá', 'DANHGIA'),
        ('DG_APPROVE', N'Duyệt Đánh Giá', 'DANHGIA'),
        ('DG_DELETE', N'Xóa Đánh Giá', 'DANHGIA'),
        ('DG_REPLY', N'Trả Lời Đánh Giá', 'DANHGIA')
    
    -- ===== XEM THỐNG KÊ =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('TK_DASHBOARD', N'Xem Dashboard Tổng Quan', 'THONGKE'),
        ('TK_REVENUE', N'Xem Thống Kê Doanh Thu', 'THONGKE'),
        ('TK_PRODUCT', N'Xem Thống Kê Sản Phẩm', 'THONGKE'),
        ('TK_CUSTOMER', N'Xem Thống Kê Khách Hàng', 'THONGKE'),
        ('TK_EXPORT', N'Xuất Báo Cáo Thống Kê', 'THONGKE')
    
    -- ===== QUẢN LÝ PHÂN QUYỀN =====
    INSERT INTO Quyen (MaQuyen, TenQuyen, MaChucNang) VALUES 
        ('PQ_CHUCNANG_VIEW', N'Xem Danh Sách Chức Năng', 'PHANQUYEN'),
        ('PQ_CHUCNANG_MANAGE', N'Quản Lý Chức Năng', 'PHANQUYEN'),
        ('PQ_QUYEN_VIEW', N'Xem Danh Sách Quyền', 'PHANQUYEN'),
        ('PQ_QUYEN_MANAGE', N'Quản Lý Quyền', 'PHANQUYEN'),
        ('PQ_ASSIGN', N'Phân Quyền Cho Chức Vụ', 'PHANQUYEN')
    
    -- Đếm tổng số quyền đã thêm
    DECLARE @TotalQuyenCount INT
    SELECT @TotalQuyenCount = COUNT(*) FROM Quyen
    PRINT 'Đã thêm ' + CAST(@TotalQuyenCount AS VARCHAR(10)) + ' Quyền!'
    PRINT ''

    -- =============================================
    -- 4. GÁN QUYỀN CHO CHỨC VỤ
    -- =============================================
    PRINT '4. Gán Quyền cho Chức Vụ...'
    
    -- Lấy mã chức vụ
    DECLARE @MaQuanLy NVARCHAR(50)
    DECLARE @MaNhanVien NVARCHAR(50)
    
    SELECT @MaQuanLy = MaCV FROM ChucVu WHERE TenCV LIKE N'%Quản Lý%'
    SELECT @MaNhanVien = MaCV FROM ChucVu WHERE TenCV LIKE N'%Nhân Viên%'
    
    IF @MaQuanLy IS NULL OR @MaNhanVien IS NULL
    BEGIN
        PRINT 'CẢNH BÁO: Không tìm thấy chức vụ Quản Lý hoặc Nhân Viên!'
        PRINT 'Vui lòng tạo chức vụ trước khi chạy script này.'
    END
    ELSE
    BEGIN
        -- ===== GÁN TẤT CẢ QUYỀN CHO QUẢN LÝ =====
        PRINT '   - Gán quyền cho Quản Lý...'
        
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen)
        SELECT @MaQuanLy, MaQuyen 
        FROM Quyen
        
        DECLARE @CountQuanLy INT
        SELECT @CountQuanLy = COUNT(*) 
        FROM ChucVu_Quyen 
        WHERE MaCV = @MaQuanLy
        
        PRINT '     Đã gán ' + CAST(@CountQuanLy AS VARCHAR(10)) + ' quyền cho Quản Lý!'
        
        -- ===== GÁN QUYỀN CHO NHÂN VIÊN =====
        PRINT '   - Gán quyền cho Nhân Viên...'
        
        -- Sách: Xem, Thêm, Sửa, Chi tiết (Không có Xóa)
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'SACH_VIEW')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'SACH_CREATE')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'SACH_EDIT')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'SACH_DETAIL')
        
        -- Loại Sách: Chỉ xem
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'LS_VIEW')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'LS_DETAIL')
        
        -- Đơn Hàng: Xem và cập nhật trạng thái
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'DH_VIEW')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'DH_DETAIL')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'DH_UPDATE_STATUS')
        
        -- Khách Hàng: Chỉ xem
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'KH_VIEW')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'KH_DETAIL')
        
        -- Mã Giảm Giá: Chỉ xem
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'MGG_VIEW')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'MGG_DETAIL')
        
        -- Đánh Giá: Xem, Duyệt, Trả lời
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'DG_VIEW')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'DG_DETAIL')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'DG_APPROVE')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'DG_REPLY')
        
        -- Thống Kê: Xem Dashboard và Sản phẩm
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'TK_DASHBOARD')
        INSERT INTO ChucVu_Quyen (MaCV, MaQuyen) VALUES (@MaNhanVien, 'TK_PRODUCT')
        
        DECLARE @CountNhanVien INT
        SELECT @CountNhanVien = COUNT(*) 
        FROM ChucVu_Quyen 
        WHERE MaCV = @MaNhanVien
        
        PRINT '     Đã gán ' + CAST(@CountNhanVien AS VARCHAR(10)) + ' quyền cho Nhân Viên!'
    END
    
    PRINT 'Hoàn thành gán quyền!'
    PRINT ''

    -- =============================================
    -- 5. KIỂM TRA KẾT QUẢ
    -- =============================================
    PRINT '===== KẾT QUẢ CẬP NHẬT ====='
    PRINT ''
    
    DECLARE @TotalChucNang INT, @TotalQuyen INT, @TotalPhanQuyen INT
    SELECT @TotalChucNang = COUNT(*) FROM ChucNang
    SELECT @TotalQuyen = COUNT(*) FROM Quyen
    SELECT @TotalPhanQuyen = COUNT(*) FROM ChucVu_Quyen
    
    PRINT 'Tổng số Chức Năng: ' + CAST(@TotalChucNang AS VARCHAR(10))
    PRINT 'Tổng số Quyền: ' + CAST(@TotalQuyen AS VARCHAR(10))
    PRINT 'Tổng số Phân Quyền: ' + CAST(@TotalPhanQuyen AS VARCHAR(10))
    PRINT ''
    
    -- Hiển thị chi tiết số quyền theo chức vụ
    PRINT '===== SỐ QUYỀN THEO CHỨC VỤ ====='
    SELECT 
        cv.TenCV AS [Chức Vụ],
        COUNT(cvq.MaQuyen) AS [Số Quyền]
    FROM ChucVu cv
    LEFT JOIN ChucVu_Quyen cvq ON cv.MaCV = cvq.MaCV
    GROUP BY cv.TenCV
    ORDER BY COUNT(cvq.MaQuyen) DESC
    
    PRINT ''
    
    -- Hiển thị chi tiết số quyền theo chức năng
    PRINT '===== SỐ QUYỀN THEO CHỨC NĂNG ====='
    SELECT 
        cn.TenChucNang AS [Chức Năng],
        COUNT(q.MaQuyen) AS [Số Quyền]
    FROM ChucNang cn
    LEFT JOIN Quyen q ON cn.MaChucNang = q.MaChucNang
    GROUP BY cn.TenChucNang
    ORDER BY COUNT(q.MaQuyen) DESC
    
    PRINT ''
    PRINT '✅ Hoàn tất cập nhật hệ thống phân quyền!'
    
    -- Commit transaction nếu không có lỗi
    COMMIT TRANSACTION
    PRINT '✅ Transaction đã được commit!'
    
END TRY
BEGIN CATCH
    -- Rollback nếu có lỗi
    ROLLBACK TRANSACTION
    
    PRINT ''
    PRINT '❌ LỖI: Có lỗi xảy ra trong quá trình cập nhật!'
    PRINT 'Chi tiết lỗi:'
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10))
    PRINT 'Error Message: ' + ERROR_MESSAGE()
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10))
    PRINT ''
    PRINT '⚠️ Transaction đã được rollback!'
END CATCH

GO

-- =============================================
-- 6. HIỂN thị DANH SÁCH QUYỀN (Tham khảo)
-- =============================================
PRINT ''
PRINT '===== DANH SÁCH TẤT CẢ QUYỀN ====='
SELECT 
    cn.TenChucNang AS [Chức Năng],
    q.MaQuyen AS [Mã Quyền],
    q.TenQuyen AS [Tên Quyền]
FROM Quyen q
INNER JOIN ChucNang cn ON q.MaChucNang = cn.MaChucNang
ORDER BY cn.MaChucNang, q.MaQuyen

GO
