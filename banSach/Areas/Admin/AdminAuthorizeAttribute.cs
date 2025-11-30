using banSach.Models;
using DocumentFormat.OpenXml.Math;
using System.Linq;
using System.Web;
using System.Web.Mvc;

public class AdminAuthorizeAttribute : AuthorizeAttribute
{
	public string Roles { get; set; } 

	protected override bool AuthorizeCore(HttpContextBase httpContext)
	{
		var user = httpContext.Session["AdminUser"] as NhanVien;
		if (user == null)
		{
			return false; // Chưa đăng nhập
		}

		if (string.IsNullOrEmpty(Roles))
		{
			return true; // Chỉ cần đăng nhập
		}

		var userRole = httpContext.Session["ChucVu"] as string;
		if (string.IsNullOrEmpty(userRole))
		{
			return false; // Không có chức vụ
		}

		// Tách các vai trò được phép
		var allowedRoles = Roles.Split(',').Select(r => r.Trim()).ToList();

		// Kiểm tra xem vai trò của người dùng có nằm trong danh sách được phép không
		if (allowedRoles.Contains(userRole))
		{
			return true; // Được phép
		}

		return false; // Không được phép
	}
	protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
	{
		var httpContext = filterContext.HttpContext;

		// Nếu chưa đăng nhập → Redirect Login
		if (httpContext.Session["AdminUser"] == null)
		{
			filterContext.Result = new RedirectToRouteResult(
				new System.Web.Routing.RouteValueDictionary(
					new { controller = "Login", action = "Index", area = "Admin" }
				)
			);
			return;
		}

		// Đã đăng nhập nhưng không có quyền → Hiển thị trang Access Denied
		string message = string.IsNullOrEmpty(Roles)
			? $"Bạn không có quyền truy cập chức năng này."
			: $"Bạn không có quyền truy cập chức năng này.";


		// Set ViewBag để hiển thị trong view
		filterContext.Controller.ViewBag.ErrorTitle = "Truy Cập Bị Từ Chối";
		filterContext.Controller.ViewBag.ErrorMessage = message;
		

		// Trả về ViewResult thay vì 401 để tránh IIS chặn
		filterContext.Result = new ViewResult
		{
			ViewName = "~/Areas/Admin/Views/Shared/AccessDenied.cshtml",
			ViewData = filterContext.Controller.ViewData,
			TempData = filterContext.Controller.TempData
		};
	}

	//protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
	//{
	//	// Nếu chưa đăng nhập, chuyển đến trang Login
	//	if (filterContext.HttpContext.Session["AdminUser"] == null)
	//	{
	//		filterContext.Result = new RedirectToRouteResult(
	//			new System.Web.Routing.RouteValueDictionary(
	//				new { controller = "Login", action = "Index", area = "Admin" }
	//			)
	//		);
	//	}
	//}
}