using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WukongWeb.Infrastructure;
using WukongWeb.Models;
using WukongWeb.Models.ViewModels;

namespace WukongWeb.Controllers
{
    public class AccountController : BaseController
    {
        // ---------- standard Razor pages ----------

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = FindUser(model.Email);
            if (user == null || !user.IsActive ||
                !PasswordHasher.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                ModelState.AddModelError(string.Empty, "Email or password was not recognized.");
                return View(model);
            }

            user.LastLoginAtUtc = DateTime.UtcNow;
            Db.SaveChanges();

            AuthService.SignIn(user.NormalizedEmail, user.DisplayName, user.Role.RoleName);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (FindUser(model.Email) != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var role = Db.Roles.FirstOrDefault(r => r.RoleName == "User");
            if (role == null)
            {
                ModelState.AddModelError(string.Empty, "The user role is missing from the database. Run database/setup.sql.");
                return View(model);
            }

            var user = CreateUser(model.DisplayName, model.Email, model.Password, role.RoleId, true);
            Db.Users.AddObject(user);
            Db.SaveChanges();

            AuthService.SignIn(user.NormalizedEmail, user.DisplayName, role.RoleName);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public new ActionResult Profile()
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                return RedirectToAction("Login");
            }

            var model = new ProfilePageViewModel
            {
                Profile = new ProfileViewModel { DisplayName = user.DisplayName, Email = user.Email },
                Password = new ChangePasswordViewModel(),
                Orders = Db.Orders
                    .Where(o => o.UserId == user.UserId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList(),
                Reviews = Db.Reviews
                    .Where(r => r.UserId == user.UserId)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .ToList(),
                Wishlist = Db.Wishlists
                    .Where(w => w.UserId == user.UserId)
                    .OrderByDescending(w => w.AddedAtUtc)
                    .Select(w => w.Game)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(ProfileViewModel model)
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = FirstError();
                return RedirectToAction("Profile");
            }

            var normalized = Normalize(model.Email);
            var duplicate = Db.Users.FirstOrDefault(u => u.NormalizedEmail == normalized && u.UserId != user.UserId);
            if (duplicate != null)
            {
                TempData["Error"] = "Another account already uses that email.";
                return RedirectToAction("Profile");
            }

            user.DisplayName = model.DisplayName.Trim();
            user.Email = model.Email.Trim();
            user.NormalizedEmail = normalized;
            Db.SaveChanges();

            AuthService.SignIn(user.NormalizedEmail, user.DisplayName, user.Role.RoleName);
            TempData["Message"] = "Your profile has been updated.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = FirstError();
                return RedirectToAction("Profile");
            }

            if (!PasswordHasher.Verify(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            {
                TempData["Error"] = "The current password is not correct.";
                return RedirectToAction("Profile");
            }

            string hash;
            string salt;
            PasswordHasher.Create(model.NewPassword, out hash, out salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            Db.SaveChanges();

            TempData["Message"] = "Your password has been changed.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            AuthService.SignOut();
            return RedirectToAction("Index", "Home");
        }

        // GET /Account/Logout is only a landing spot for stale links; signing out
        // requires the antiforgery-protected POST form so a link cannot end a session.
        [HttpGet]
        [AllowAnonymous]
        [ActionName("Logout")]
        public ActionResult LogoutRedirect()
        {
            return RedirectToAction("Index", "Home");
        }

        // ---------- JSON endpoints used by the landing-page modal ----------

        [HttpGet]
        [AllowAnonymous]
        public JsonResult Me()
        {
            var user = CurrentUser;
            return Json(new
            {
                user = user == null ? null : new
                {
                    name = user.DisplayName,
                    email = user.Email,
                    role = user.Role.RoleName
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public JsonResult JsonLogin(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return JsonErrors();
            }

            var user = FindUser(model.Email);
            if (user == null || !user.IsActive ||
                !PasswordHasher.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            {
                Response.StatusCode = 401;
                return Json(new { error = "Email or password was not recognized." });
            }

            user.LastLoginAtUtc = DateTime.UtcNow;
            Db.SaveChanges();
            AuthService.SignIn(user.NormalizedEmail, user.DisplayName, user.Role.RoleName);

            return Json(new
            {
                user = new { name = user.DisplayName, email = user.Email, role = user.Role.RoleName }
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public JsonResult JsonRegister(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return JsonErrors();
            }

            var existing = FindUser(model.Email);
            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    Response.StatusCode = 403;
                    return Json(new { error = "This account has been deactivated. Contact the administrator." });
                }

                if (PasswordHasher.Verify(model.Password, existing.PasswordHash, existing.PasswordSalt))
                {
                    existing.LastLoginAtUtc = DateTime.UtcNow;
                    Db.SaveChanges();
                    AuthService.SignIn(existing.NormalizedEmail, existing.DisplayName, existing.Role.RoleName);
                    return Json(new
                    {
                        user = new { name = existing.DisplayName, email = existing.Email, role = existing.Role.RoleName },
                        existing = true
                    });
                }

                Response.StatusCode = 409;
                return Json(new { error = "An account with this email already exists. Sign in with its existing password." });
            }

            var role = Db.Roles.FirstOrDefault(r => r.RoleName == "User");
            if (role == null)
            {
                Response.StatusCode = 500;
                return Json(new { error = "The user role is missing from the database. Run database/setup.sql." });
            }

            var user = CreateUser(model.DisplayName, model.Email, model.Password, role.RoleId, true);
            Db.Users.AddObject(user);
            Db.SaveChanges();

            AuthService.SignIn(user.NormalizedEmail, user.DisplayName, role.RoleName);
            Response.StatusCode = 201;
            return Json(new
            {
                user = new { name = user.DisplayName, email = user.Email, role = role.RoleName }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult JsonLogout()
        {
            AuthService.SignOut();
            return Json(new { user = (object)null });
        }

        // The landing page keeps a single antiforgery token for the whole visit.
        // After an AJAX sign-in the original token no longer matches the new
        // identity, so the page requests a fresh token for the signed-in user.
        [HttpGet]
        [AllowAnonymous]
        public JsonResult AntiForgeryToken()
        {
            string cookieToken;
            string formToken;
            System.Web.Helpers.AntiForgery.GetTokens(null, out cookieToken, out formToken);

            if (!string.IsNullOrEmpty(cookieToken))
            {
                var cookie = new HttpCookie(System.Web.Helpers.AntiForgeryConfig.CookieName, cookieToken)
                {
                    HttpOnly = true,
                    Path = string.IsNullOrEmpty(Request.ApplicationPath) ? "/" : Request.ApplicationPath
                };
                Response.Cookies.Set(cookie);
            }

            return Json(new { token = formToken }, JsonRequestBehavior.AllowGet);
        }

        // ---------- helpers ----------

        private WukongWeb.Models.User FindUser(string email)
        {
            var normalized = Normalize(email);
            return Db.Users.FirstOrDefault(u => u.NormalizedEmail == normalized);
        }

        private static string Normalize(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static WukongWeb.Models.User CreateUser(string displayName, string email, string password, int roleId, bool isActive)
        {
            string hash;
            string salt;
            PasswordHasher.Create(password, out hash, out salt);

            return new WukongWeb.Models.User
            {
                UserId = Guid.NewGuid(),
                Email = email.Trim(),
                NormalizedEmail = Normalize(email),
                DisplayName = displayName.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                RoleId = roleId,
                IsActive = isActive,
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        private JsonResult JsonErrors()
        {
            var errors = ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();

            Response.StatusCode = 400;
            return Json(new { error = string.Join(" ", errors) });
        }

        private string FirstError()
        {
            return ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "The form could not be saved.";
        }
    }
}
