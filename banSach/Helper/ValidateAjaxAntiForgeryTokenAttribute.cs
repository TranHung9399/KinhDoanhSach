using System;
using System.Web.Helpers;
using System.Web.Mvc;

namespace banSach.Helper
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class ValidateAjaxAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
	{
		public void OnAuthorization(AuthorizationContext filterContext)
		{
			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			var request = filterContext.HttpContext.Request;

			// Only validate for POST requests
			if (request.HttpMethod == "POST")
			{
				// Get the token from header (set by our AJAX setup)
				var token = request.Headers["RequestVerificationToken"];

				// If not in header, try form/query string (fallback)
				if (string.IsNullOrEmpty(token))
				{
					token = request.Form["__RequestVerificationToken"];
				}

				// Validate the token
				try
				{
					if (!string.IsNullOrEmpty(token))
					{
						// Use AntiForgery.Validate with cookie and form token
						var cookieToken = request.Cookies["__RequestVerificationToken"]?.Value;
						AntiForgery.Validate(cookieToken, token);
					}
					else
					{
						throw new HttpAntiForgeryException("Anti-forgery token not found");
					}
				}
				catch (HttpAntiForgeryException)
				{
					filterContext.Result = new JsonResult
					{
						Data = new { success = false, message = "Invalid security token. Please refresh the page and try again." },
						JsonRequestBehavior = JsonRequestBehavior.AllowGet
					};
				}
			}
		}
	}
}