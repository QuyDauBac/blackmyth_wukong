using System;
using System.Web;
using System.Web.Security;

namespace WukongWeb.Infrastructure
{
    public static class AuthService
    {
        public static void SignIn(string email, string displayName, string role, bool persist = false)
        {
            var issued = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(
                1,
                email,
                issued,
                issued.AddMinutes(120),
                persist,
                (role ?? "User") + "|" + (displayName ?? string.Empty));

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            };

            if (persist)
            {
                cookie.Expires = ticket.Expiration;
            }

            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        public static void SignOut()
        {
            FormsAuthentication.SignOut();
        }

        public static string DisplayName
        {
            get
            {
                var user = HttpContext.Current?.User;
                if (user?.Identity == null || !user.Identity.IsAuthenticated)
                {
                    return string.Empty;
                }

                var identity = user.Identity as FormsIdentity;
                if (identity == null)
                {
                    return user.Identity.Name;
                }

                var parts = (identity.Ticket.UserData ?? string.Empty).Split('|');
                return parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
                    ? parts[1]
                    : user.Identity.Name;
            }
        }

        public static string Role
        {
            get
            {
                var user = HttpContext.Current?.User;
                if (user == null || !user.Identity.IsAuthenticated)
                {
                    return string.Empty;
                }

                var identity = user.Identity as FormsIdentity;
                if (identity == null)
                {
                    return string.Empty;
                }

                var parts = (identity.Ticket.UserData ?? string.Empty).Split('|');
                return parts.Length > 0 ? parts[0] : "User";
            }
        }
    }
}
