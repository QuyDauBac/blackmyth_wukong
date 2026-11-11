using System;
using System.Linq;
using System.Web.Mvc;
using WukongWeb.Infrastructure;
using WukongWeb.Models;
using WukongWeb.Models.ViewModels;

namespace WukongWeb.Controllers
{
    public class GamesController : BaseController
    {
        private const int PageSize = 6;

        public ActionResult Index(string q, int? categoryId, string sort, int page = 1)
        {
            if (page < 1) page = 1;

            var query = Db.Games.Where(g => g.IsActive);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(g => g.Title.Contains(term) || g.Description.Contains(term));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(g => g.CategoryId == categoryId.Value);
            }

            switch ((sort ?? string.Empty).ToLowerInvariant())
            {
                case "price":
                    query = query.OrderBy(g => g.BasePrice);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(g => g.BasePrice);
                    break;
                case "newest":
                    query = query.OrderByDescending(g => g.ReleaseDate);
                    break;
                default:
                    query = query.OrderBy(g => g.Title);
                    break;
            }

            var total = query.Count();
            var items = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var model = new GamesIndexViewModel
            {
                Games = new PagedResult<Game>
                {
                    Items = items,
                    Page = page,
                    PageSize = PageSize,
                    TotalCount = total
                },
                Query = q,
                CategoryId = categoryId,
                Sort = sort,
                Categories = Db.Categories.OrderBy(c => c.Name).ToList()
            };

            return View(model);
        }

        public ActionResult Details(int id)
        {
            var game = Db.Games.FirstOrDefault(g => g.GameId == id && g.IsActive);
            if (game == null)
            {
                return NotFoundPage();
            }

            var user = CurrentUser;

            var model = new GameDetailsViewModel
            {
                Game = game,
                Editions = Db.Editions
                    .Where(e => e.GameId == id && e.IsActive)
                    .OrderBy(e => e.Price)
                    .ToList(),
                Reviews = Db.Reviews
                    .Where(r => r.GameId == id)
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .ToList(),
                AverageRating = Db.Reviews
                    .Where(r => r.GameId == id)
                    .Average(r => (int?)r.Rating),
                ReviewCount = Db.Reviews.Count(r => r.GameId == id),
                InWishlist = user != null && Db.Wishlists.Any(w => w.UserId == user.UserId && w.GameId == id),
                HasReviewed = user != null && Db.Reviews.Any(r => r.UserId == user.UserId && r.GameId == id),
                NewReview = new ReviewViewModel { GameId = id, Rating = 5 }
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult AddReview(ReviewViewModel model)
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Choose a rating between 1 and 5.";
                return RedirectToAction("Details", new { id = model.GameId });
            }

            var existing = Db.Reviews.FirstOrDefault(r => r.GameId == model.GameId && r.UserId == user.UserId);
            if (existing == null)
            {
                Db.Reviews.AddObject(new Review
                {
                    GameId = model.GameId,
                    UserId = user.UserId,
                    Rating = model.Rating,
                    Comment = (model.Comment ?? string.Empty).Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.Rating = model.Rating;
                existing.Comment = (model.Comment ?? string.Empty).Trim();
            }

            Db.SaveChanges();
            TempData["Message"] = "Your review has been saved.";
            return RedirectToAction("Details", new { id = model.GameId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleWishlist(int gameId)
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                return RedirectToAction("Login", "Account");
            }

            var item = Db.Wishlists.FirstOrDefault(w => w.UserId == user.UserId && w.GameId == gameId);
            if (item == null)
            {
                Db.Wishlists.AddObject(new Wishlist
                {
                    UserId = user.UserId,
                    GameId = gameId,
                    AddedAtUtc = DateTime.UtcNow
                });
                TempData["Message"] = "Added to your wishlist.";
            }
            else
            {
                Db.Wishlists.DeleteObject(item);
                TempData["Message"] = "Removed from your wishlist.";
            }

            Db.SaveChanges();
            return RedirectToAction("Details", new { id = gameId });
        }

        [Authorize]
        public ActionResult Wishlist()
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                return RedirectToAction("Login", "Account");
            }

            var model = new WishlistViewModel
            {
                Games = Db.Wishlists
                    .Where(w => w.UserId == user.UserId)
                    .OrderByDescending(w => w.AddedAtUtc)
                    .Select(w => w.Game)
                    .ToList()
            };

            return View(model);
        }
    }
}
