using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WukongWeb.Infrastructure;
using WukongWeb.Models;
using WukongWeb.Models.ViewModels;

namespace WukongWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private const int PageSize = 10;

        private static readonly string[] OrderStatuses = { "Pending", "Paid", "Cancelled", "Refunded" };

        // ---------- dashboard ----------

        public ActionResult Index()
        {
            var top = Db.OrderDetails
                .GroupBy(d => d.Edition.GameId)
                .Select(g => new
                {
                    GameId = g.Key,
                    UnitsSold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(g => g.UnitsSold)
                .Take(5)
                .ToList();

            var gameIds = top.Select(t => t.GameId).ToList();
            var titles = Db.Games
                .Where(g => gameIds.Contains(g.GameId))
                .ToDictionary(g => g.GameId, g => g.Title);

            var model = new AdminDashboardViewModel
            {
                UserCount = Db.Users.Count(),
                GameCount = Db.Games.Count(),
                OrderCount = Db.Orders.Count(),
                ReviewCount = Db.Reviews.Count(),
                SubscriberCount = Db.NewsletterSubscribers.Count(),
                RecentSubscribers = Db.NewsletterSubscribers
                    .OrderByDescending(s => s.SubscribedAtUtc)
                    .Take(5)
                    .ToList(),
                Revenue = Db.Orders
                    .Where(o => o.Status == "Paid")
                    .Select(o => o.TotalAmount)
                    .DefaultIfEmpty(0)
                    .Sum(),
                RecentOrders = Db.Orders
                    .Include("User")
                    .OrderByDescending(o => o.OrderDate)
                    .Take(8)
                    .ToList(),
                TopGames = top.Select(t => new TopGameRow
                {
                    GameId = t.GameId,
                    Title = titles.ContainsKey(t.GameId) ? titles[t.GameId] : "(deleted game)",
                    UnitsSold = t.UnitsSold,
                    Revenue = t.Revenue
                }).ToList()
            };

            return View(model);
        }

        // ---------- games CRUD ----------

        public ActionResult Games(string q, int page = 1)
        {
            if (page < 1) page = 1;

            var query = Db.Games.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(g => g.Title.Contains(term) || g.Slug.Contains(term));
            }

            var total = query.Count();
            var items = query
                .OrderBy(g => g.Title)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return View(new AdminGamesViewModel
            {
                Games = new PagedResult<Game>
                {
                    Items = items,
                    Page = page,
                    PageSize = PageSize,
                    TotalCount = total
                },
                Query = q
            });
        }

        public ActionResult GameCreate()
        {
            return View(new GameEditViewModel
            {
                IsActive = true,
                BasePrice = 0,
                CategoryOptions = CategoryOptions()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GameCreate(GameEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CategoryOptions = CategoryOptions();
                return View(model);
            }

            var slug = (model.Slug ?? string.Empty).Trim();
            if (Db.Games.Any(g => g.Slug == slug))
            {
                ModelState.AddModelError("Slug", "Another game already uses this slug.");
                model.CategoryOptions = CategoryOptions();
                return View(model);
            }

            var game = new Game
            {
                Title = model.Title.Trim(),
                Slug = slug,
                Description = (model.Description ?? string.Empty).Trim(),
                CategoryId = model.CategoryId,
                Developer = model.Developer.Trim(),
                Publisher = model.Publisher.Trim(),
                ReleaseDate = model.ReleaseDate,
                BasePrice = model.BasePrice,
                CoverImage = (model.CoverImage ?? string.Empty).Trim(),
                IsActive = model.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };

            Db.Games.AddObject(game);
            Db.SaveChanges();

            TempData["Message"] = "Game created.";
            return RedirectToAction("Games");
        }

        public ActionResult GameEdit(int id)
        {
            var game = Db.Games.FirstOrDefault(g => g.GameId == id);
            if (game == null)
            {
                return NotFoundPage();
            }

            var model = ToEditViewModel(game);
            model.CategoryOptions = CategoryOptions();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GameEdit(int id, GameEditViewModel model)
        {
            var game = Db.Games.FirstOrDefault(g => g.GameId == id);
            if (game == null)
            {
                return NotFoundPage();
            }

            if (!ModelState.IsValid)
            {
                model.GameId = id;
                model.CategoryOptions = CategoryOptions();
                return View(model);
            }

            var slug = (model.Slug ?? string.Empty).Trim();
            if (Db.Games.Any(g => g.Slug == slug && g.GameId != id))
            {
                ModelState.AddModelError("Slug", "Another game already uses this slug.");
                model.GameId = id;
                model.CategoryOptions = CategoryOptions();
                return View(model);
            }

            game.Title = model.Title.Trim();
            game.Slug = slug;
            game.Description = (model.Description ?? string.Empty).Trim();
            game.CategoryId = model.CategoryId;
            game.Developer = model.Developer.Trim();
            game.Publisher = model.Publisher.Trim();
            game.ReleaseDate = model.ReleaseDate;
            game.BasePrice = model.BasePrice;
            game.CoverImage = (model.CoverImage ?? string.Empty).Trim();
            game.IsActive = model.IsActive;

            Db.SaveChanges();

            TempData["Message"] = "Game updated.";
            return RedirectToAction("Games");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GameDelete(int id)
        {
            var game = Db.Games.FirstOrDefault(g => g.GameId == id);
            if (game == null)
            {
                return NotFoundPage();
            }

            // OrderDetails references Editions without a cascade, so a game that has
            // already been sold must be deactivated instead of removed.
            var hasOrders = Db.OrderDetails.Any(d => d.Edition.GameId == id);
            if (hasOrders)
            {
                game.IsActive = false;
                Db.SaveChanges();
                TempData["Message"] = "This game has sales history, so it was deactivated instead of deleted.";
                return RedirectToAction("Games");
            }

            Db.Games.DeleteObject(game);
            Db.SaveChanges();
            TempData["Message"] = "Game deleted.";
            return RedirectToAction("Games");
        }

        // ---------- orders ----------

        public ActionResult Orders(string q, int page = 1)
        {
            if (page < 1) page = 1;

            var query = Db.Orders.Include("User").AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(o =>
                    o.User.Email.Contains(term) ||
                    o.User.DisplayName.Contains(term) ||
                    o.Status.Contains(term));
            }

            var total = query.Count();
            var items = query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return View(new AdminOrdersViewModel
            {
                Orders = new PagedResult<Order>
                {
                    Items = items,
                    Page = page,
                    PageSize = PageSize,
                    TotalCount = total
                },
                Query = q
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OrderStatus(int id, string status)
        {
            var order = Db.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                return NotFoundPage();
            }

            if (string.IsNullOrWhiteSpace(status) || !OrderStatuses.Contains(status))
            {
                TempData["Error"] = "That order status is not allowed.";
                return RedirectToAction("Orders");
            }

            order.Status = status;
            Db.SaveChanges();
            TempData["Message"] = "Order #" + id + " marked as " + status + ".";
            return RedirectToAction("Orders");
        }

        // ---------- users ----------

        public ActionResult Users(string q, int page = 1)
        {
            if (page < 1) page = 1;

            var query = Db.Users.Include("Role").AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(u => u.Email.Contains(term) || u.DisplayName.Contains(term));
            }

            var total = query.Count();
            var items = query
                .OrderByDescending(u => u.CreatedAtUtc)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return View(new AdminUsersViewModel
            {
                Users = new PagedResult<WukongWeb.Models.User>
                {
                    Items = items,
                    Page = page,
                    PageSize = PageSize,
                    TotalCount = total
                },
                Query = q
            });
        }

        public ActionResult UserCreate()
        {
            return View(new UserCreateViewModel
            {
                IsActive = true,
                RoleOptions = RoleOptions()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserCreate(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.RoleOptions = RoleOptions();
                return View(model);
            }

            var normalized = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (Db.Users.Any(u => u.NormalizedEmail == normalized))
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                model.RoleOptions = RoleOptions();
                return View(model);
            }

            string hash;
            string salt;
            PasswordHasher.Create(model.Password, out hash, out salt);

            var user = new WukongWeb.Models.User
            {
                UserId = Guid.NewGuid(),
                Email = model.Email.Trim(),
                NormalizedEmail = normalized,
                DisplayName = model.DisplayName.Trim(),
                PasswordHash = hash,
                PasswordSalt = salt,
                RoleId = model.RoleId,
                IsActive = model.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };

            Db.Users.AddObject(user);
            Db.SaveChanges();

            TempData["Message"] = "Account created.";
            return RedirectToAction("Users");
        }

        public ActionResult UserEdit(Guid id)
        {
            var user = Db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFoundPage();
            }

            return View(new UserEditViewModel
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName,
                Email = user.Email,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                RoleOptions = RoleOptions()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserEdit(Guid id, UserEditViewModel model)
        {
            var user = Db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFoundPage();
            }

            if (!ModelState.IsValid)
            {
                model.UserId = id;
                model.RoleOptions = RoleOptions();
                return View(model);
            }

            var normalized = (model.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (Db.Users.Any(u => u.NormalizedEmail == normalized && u.UserId != id))
            {
                ModelState.AddModelError("Email", "Another account already uses this email.");
                model.UserId = id;
                model.RoleOptions = RoleOptions();
                return View(model);
            }

            var current = CurrentUser;
            if (current != null && current.UserId == id)
            {
                var adminRole = Db.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                if (adminRole == null || model.RoleId != adminRole.RoleId || !model.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "You cannot remove your own administrator access or deactivate your own account.");
                    model.UserId = id;
                    model.RoleOptions = RoleOptions();
                    return View(model);
                }
            }

            user.DisplayName = model.DisplayName.Trim();
            user.Email = model.Email.Trim();
            user.NormalizedEmail = normalized;
            user.RoleId = model.RoleId;
            user.IsActive = model.IsActive;

            Db.SaveChanges();

            TempData["Message"] = "Account updated.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserToggleActive(Guid id)
        {
            var user = Db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFoundPage();
            }

            var current = CurrentUser;
            if (current != null && current.UserId == id)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction("Users");
            }

            user.IsActive = !user.IsActive;
            Db.SaveChanges();
            TempData["Message"] = user.IsActive ? "Account activated." : "Account deactivated.";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserDelete(Guid id)
        {
            var user = Db.Users.FirstOrDefault(u => u.UserId == id);
            if (user == null)
            {
                return NotFoundPage();
            }

            var current = CurrentUser;
            if (current != null && current.UserId == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            // Orders keep their user for reporting, so an account with sales history
            // is deactivated instead of deleted. Reviews and wishlist rows cascade.
            if (Db.Orders.Any(o => o.UserId == id))
            {
                user.IsActive = false;
                Db.SaveChanges();
                TempData["Message"] = "This account has orders, so it was deactivated instead of deleted.";
                return RedirectToAction("Users");
            }

            Db.Users.DeleteObject(user);
            Db.SaveChanges();
            TempData["Message"] = "Account deleted.";
            return RedirectToAction("Users");
        }

        // ---------- reviews ----------

        public ActionResult Reviews(string q, int page = 1)
        {
            if (page < 1) page = 1;

            var query = Db.Reviews.Include("Game").Include("User").AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(r =>
                    r.Game.Title.Contains(term) ||
                    r.User.DisplayName.Contains(term) ||
                    r.Comment.Contains(term));
            }

            var total = query.Count();
            var items = query
                .OrderByDescending(r => r.CreatedAtUtc)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewBag.Query = q;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / PageSize);
            ViewBag.TotalCount = total;
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewDelete(int id)
        {
            var review = Db.Reviews.FirstOrDefault(r => r.ReviewId == id);
            if (review == null)
            {
                return NotFoundPage();
            }

            Db.Reviews.DeleteObject(review);
            Db.SaveChanges();
            TempData["Message"] = "Review deleted.";
            return RedirectToAction("Reviews");
        }

        // ---------- helpers ----------

        private IList<SelectListItem> RoleOptions()
        {
            return Db.Roles
                .OrderBy(r => r.RoleName)
                .Select(r => new SelectListItem { Value = r.RoleId.ToString(), Text = r.RoleName })
                .ToList();
        }

        private IList<SelectListItem> CategoryOptions()
        {
            return Db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                .ToList();
        }

        private static GameEditViewModel ToEditViewModel(Game game)
        {
            return new GameEditViewModel
            {
                GameId = game.GameId,
                Title = game.Title,
                Slug = game.Slug,
                Description = game.Description,
                CategoryId = game.CategoryId,
                Developer = game.Developer,
                Publisher = game.Publisher,
                ReleaseDate = game.ReleaseDate,
                BasePrice = game.BasePrice,
                CoverImage = game.CoverImage,
                IsActive = game.IsActive
            };
        }
    }
}
