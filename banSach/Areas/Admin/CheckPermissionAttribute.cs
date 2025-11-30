using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using banSach.Helper;
using banSach.Models;

namespace banSach.Areas.Admin
{
    /// <summary>
    /// Attribute để kiểm tra quyền dựa trên mã quyền trong database
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class CheckPermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Mã quyền cần kiểm tra (VD: "Q001", "Q005")
        /// </summary>
        public string Permission { get; set; }

        /// <summary>
        /// Danh sách mã quyền, người dùng cần có ít nhất 1 quyền (ngăn cách bởi dấu phẩy)
        /// VD: "Q001,Q002,Q003"
        /// </summary>
        public string Permissions { get; set; }

        /// <summary>
        /// Yêu cầu có tất cả quyền (true) hay chỉ cần 1 quyền (false)
        /// Mặc định: false
        /// </summary>
        public bool RequireAll { get; set; } = false;

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Kiểm tra đăng nhập
            var user = httpContext.Session["AdminUser"] as NhanVien;
            if (user == null)
            {
                return false;
            }

            // Kiểm tra quyền đơn lẻ
            if (!string.IsNullOrEmpty(Permission))
            {
                return PermissionHelper.HasPermission(Permission);
            }

            // Kiểm tra danh sách quyền
            if (!string.IsNullOrEmpty(Permissions))
            {
                var permissionList = Permissions.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToArray();

                if (permissionList.Length == 0)
                    return false;

                if (RequireAll)
                {
                    // Yêu cầu có tất cả quyền
                    return PermissionHelper.HasAllPermissions(permissionList);
                }
                else
                {
                    // Chỉ cần có 1 quyền
                    return PermissionHelper.HasAnyPermission(permissionList);
                }
            }

            // Nếu không chỉ định quyền cụ thể, chỉ cần đăng nhập
            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var httpContext = filterContext.HttpContext;

            // Nếu chưa đăng nhập
            if (httpContext.Session["AdminUser"] == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary(
                        new { controller = "Login", action = "Index", area = "Admin" }
                    )
                );
            }
            else
            {
                // Đã đăng nhập nhưng không có quyền
                // Hiển thị trang AccessDenied thay vì trả về 401
                string permissionInfo = !string.IsNullOrEmpty(Permission) 
                    ? Permission 
                    : Permissions;

                filterContext.Controller.ViewBag.ErrorTitle = "Truy Cập Bị Từ Chối";
                filterContext.Controller.ViewBag.ErrorMessage = 
                    $"Bạn không có quyền thực hiện thao tác này. <br>Quyền cần có: <strong>{permissionInfo}</strong>";
                filterContext.Controller.ViewBag.FunctionCode = permissionInfo;
                
                // Trả về ViewResult thay vì 401 để tránh IIS chặn
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Areas/Admin/Views/Shared/AccessDenied.cshtml",
                    ViewData = filterContext.Controller.ViewData,
                    TempData = filterContext.Controller.TempData
                };
            }
        }
    }
}
