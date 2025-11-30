using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using banSach.Helper;
using banSach.Models;

namespace banSach.Areas.Admin
{
	/// <summary>
	/// Attribute d? ki?m tra quy?n truy c?p theo CH?C NANG
	/// Hi?n th? trang AccessDenied khi không có quy?n thay vì redirect
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class CheckFunctionAccessAttribute : AuthorizeAttribute
	{
		/// <summary>
		/// Mã ch?c nang c?n ki?m tra (VD: "CN002" - Qu?n Lý Sách)
		/// CN001 - Qu?n Lý Nhân Viên
		/// CN002 - Qu?n Lý Sách  
		/// CN003 - Qu?n Lý Ðon Hàng
		/// CN004 - Qu?n Lý Khách Hàng
		/// CN005 - Qu?n Lý Mã Gi?m Giá
		/// CN006 - Xem Th?ng Kê
		/// CN007 - Qu?n Lý Ðánh Giá
		/// CN008 - Qu?n Lý Tác Gi? & NXB
		/// </summary>
		public string FunctionCode { get; set; }

		/// <summary>
		/// Tên ch?c nang (dùng d? hi?n th? thông báo l?i)
		/// </summary>
		public string FunctionName { get; set; }

		protected override bool AuthorizeCore(HttpContextBase httpContext)
		{
			// Ki?m tra dang nh?p
			var user = httpContext.Session["AdminUser"] as NhanVien;
			if (user == null)
			{
				return false;
			}

			// N?u không ch? d?nh FunctionCode, ch? c?n dang nh?p
			if (string.IsNullOrEmpty(FunctionCode))
			{
				return true;
			}

			// Ki?m tra quy?n truy c?p ch?c nang
			return PermissionHelper.HasAccessToFunction(FunctionCode);
		}

		protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
		{
			var httpContext = filterContext.HttpContext;

			// N?u chua dang nh?p ? Redirect Login
			if (httpContext.Session["AdminUser"] == null)
			{
				filterContext.Result = new RedirectToRouteResult(
					new System.Web.Routing.RouteValueDictionary(
						new { controller = "Login", action = "Index", area = "Admin" }
					)
				);
				return;
			}

			// Ðã dang nh?p nhung không có quy?n ? Hi?n th? trang Access Denied
			string message = string.IsNullOrEmpty(FunctionName)
				? $"Ban không có quyền truy cập chức năng này (Mã: {FunctionCode})."
				: $"Ban không có quyền truy cập chức năng <strong>{FunctionName}</strong>.";

			message += "<br><small class='text-muted'>Vui lòng liên hệ Quản trị viên để được cấp quyền.</small>";

			// Set ViewBag d? hi?n th? trong view
			filterContext.Controller.ViewBag.ErrorTitle = "Truy cập bị từ chối";
			filterContext.Controller.ViewBag.ErrorMessage = message;
			filterContext.Controller.ViewBag.FunctionCode = FunctionCode;
			filterContext.Controller.ViewBag.FunctionName = FunctionName;

			// Tr? v? ViewResult thay vì 401 d? tránh IIS ch?n
			filterContext.Result = new ViewResult
			{
				ViewName = "~/Areas/Admin/Views/Shared/AccessDenied.cshtml",
				ViewData = filterContext.Controller.ViewData,
				TempData = filterContext.Controller.TempData
			};
		}
	}
}
