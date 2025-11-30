using System.Collections.Generic;
using System.Linq;
using System.Web;
using banSach.Models;

namespace banSach.Helper
{
    /// <summary>
    /// Helper class để kiểm tra quyền của người dùng
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// Kiểm tra xem người dùng hiện tại có quyền cụ thể không
        /// </summary>
        /// <param name="maQuyen">Mã quyền cần kiểm tra</param>
        /// <returns>True nếu có quyền, False nếu không</returns>
        public static bool HasPermission(string maQuyen)
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null || httpContext.Session == null)
                return false;

            var user = httpContext.Session["AdminUser"] as NhanVien;
            if (user == null)
                return false;

            // ❌ BỎ DÒNG NÀY - Quản Lý cũng phải kiểm tra quyền từ database
            // if (IsManager())
            //     return true;

            // Lấy danh sách quyền từ Session (đã cache khi login)
            var permissions = httpContext.Session["UserPermissions"] as List<string>;
            
            // Nếu Session hết hạn hoặc chưa có, load lại từ database
            if (permissions == null)
            {
                permissions = LoadUserPermissionsFromDatabase(user.MaCV);
                httpContext.Session["UserPermissions"] = permissions;
            }

            return permissions != null && permissions.Contains(maQuyen);
        }

        /// <summary>
        /// Load quyền từ database (dùng khi Session hết hạn)
        /// </summary>
        private static List<string> LoadUserPermissionsFromDatabase(string maCV)
        {
            using (var db = new QLBanSachEntities())
            {
                var chucVu = db.ChucVus
                    .Include("Quyens")
                    .FirstOrDefault(cv => cv.MaCV == maCV);

                if (chucVu == null)
                    return new List<string>();

                return chucVu.Quyens.Select(q => q.MaQuyen).ToList();
            }
        }

        /// <summary>
        /// Làm mới quyền từ database (gọi sau khi thay đổi quyền)
        /// </summary>
        public static void RefreshUserPermissions()
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null || httpContext.Session == null)
                return;

            var user = httpContext.Session["AdminUser"] as NhanVien;
            if (user == null)
                return;

            var permissions = LoadUserPermissionsFromDatabase(user.MaCV);
            httpContext.Session["UserPermissions"] = permissions;
        }

        /// <summary>
        /// Kiểm tra xem người dùng có bất kỳ quyền nào trong danh sách không
        /// </summary>
        /// <param name="maQuyens">Danh sách mã quyền</param>
        /// <returns>True nếu có ít nhất 1 quyền</returns>
        public static bool HasAnyPermission(params string[] maQuyens)
        {
            if (maQuyens == null || maQuyens.Length == 0)
                return false;

            // ❌ BỎ - Quản lý cũng phải check quyền
            // if (IsManager())
            //     return true;

            foreach (var maQuyen in maQuyens)
            {
                if (HasPermission(maQuyen))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Kiểm tra xem người dùng có tất cả quyền trong danh sách không
        /// </summary>
        /// <param name="maQuyens">Danh sách mã quyền</param>
        /// <returns>True nếu có tất cả quyền</returns>
        public static bool HasAllPermissions(params string[] maQuyens)
        {
            if (maQuyens == null || maQuyens.Length == 0)
                return false;

            // ❌ BỎ - Quản lý cũng phải check quyền
            // if (IsManager())
            //     return true;

            foreach (var maQuyen in maQuyens)
            {
                if (!HasPermission(maQuyen))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Lấy tất cả quyền của người dùng hiện tại
        /// </summary>
        /// <returns>Danh sách mã quyền</returns>
        public static List<string> GetUserPermissions()
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null || httpContext.Session == null)
                return new List<string>();

            var user = httpContext.Session["AdminUser"] as NhanVien;
            if (user == null)
                return new List<string>();

            var permissions = httpContext.Session["UserPermissions"] as List<string>;
            
            if (permissions == null)
            {
                permissions = LoadUserPermissionsFromDatabase(user.MaCV);
                httpContext.Session["UserPermissions"] = permissions;
            }

            return permissions ?? new List<string>();
        }

        /// <summary>
        /// Kiểm tra xem người dùng có phải là Quản Lý không (CHỈ DÙNG ĐỂ HIỂN THỊ)
        /// ⚠️ KHÔNG DÙNG ĐỂ KIỂM TRA QUYỀN!
        /// </summary>
        /// <returns>True nếu là Quản Lý</returns>
        public static bool IsManager()
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null || httpContext.Session == null)
                return false;

            var chucVu = httpContext.Session["ChucVu"] as string;
            return !string.IsNullOrEmpty(chucVu) && chucVu.Contains("Quản Lý");
        }

        /// <summary>
        /// Kiểm tra xem người dùng có quyền truy cập chức năng không
        /// </summary>
        /// <param name="maChucNang">Mã chức năng</param>
        /// <returns>True nếu có quyền</returns>
        public static bool HasAccessToFunction(string maChucNang)
        {
            var httpContext = HttpContext.Current;
            if (httpContext == null || httpContext.Session == null)
                return false;

            var user = httpContext.Session["AdminUser"] as NhanVien;
            if (user == null)
                return false;

            // ❌ BỎ - Quản lý cũng phải check quyền
            // if (IsManager())
            //     return true;

            using (var db = new QLBanSachEntities())
            {
                var chucVu = db.ChucVus
                    .Include("Quyens")
                    .Include("Quyens.ChucNang")
                    .FirstOrDefault(cv => cv.MaCV == user.MaCV);

                if (chucVu == null)
                    return false;

                // Kiểm tra xem có quyền nào thuộc chức năng này không
                return chucVu.Quyens.Any(q => q.MaChucNang == maChucNang);
            }
        }
    }
}
