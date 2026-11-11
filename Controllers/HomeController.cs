using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using WukongWeb.Infrastructure;
using WukongWeb.Models;
using WukongWeb.Models.ViewModels;

namespace WukongWeb.Controllers
{
    public class HomeController : BaseController
    {
        private static readonly Regex EmailPattern =
            new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

        public ActionResult Index()
        {
            var model = new HomeViewModel
            {
                MediaItems = Db.MediaAssets
                    .Where(m => m.MediaType == "Image")
                    .OrderBy(m => m.SortOrder)
                    .Take(5)
                    .ToList(),
                Awards = Db.Awards
                    .OrderBy(a => a.AwardId)
                    .ToList(),

                // The landing page "Store" screen: every active title with the
                // cheapest edition that can actually be bought.
                StoreItems = Db.Games
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.GameId)
                    .Select(g => new StoreItemViewModel
                    {
                        GameId = g.GameId,
                        Title = g.Title,
                        CategoryName = g.Category.Name,
                        CoverImage = g.CoverImage,
                        EditionId = g.Editions
                            .Where(e => e.IsActive)
                            .OrderBy(e => e.Price)
                            .Select(e => e.EditionId)
                            .FirstOrDefault(),
                        EditionName = g.Editions
                            .Where(e => e.IsActive)
                            .OrderBy(e => e.Price)
                            .Select(e => e.Name)
                            .FirstOrDefault(),
                        Price = g.Editions
                            .Where(e => e.IsActive)
                            .OrderBy(e => e.Price)
                            .Select(e => e.Price)
                            .FirstOrDefault()
                    })
                    .ToList()
                    .Where(item => item.EditionId > 0)
                    .ToList()
            };

            return View(model);
        }

        // The footer "Subscribe now" button writes the address to NewsletterSubscribers.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Subscribe(string email)
        {
            email = (email ?? string.Empty).Trim();

            if (email.Length == 0 || email.Length > 254 || !EmailPattern.IsMatch(email))
            {
                Response.StatusCode = 400;
                return Json(new { ok = false, error = "Enter a valid email address." });
            }

            var key = email.ToLowerInvariant();
            var existing = Db.NewsletterSubscribers.FirstOrDefault(s => s.Email == key);
            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    Db.SaveChanges();
                }

                return Json(new { ok = true, message = "You are already on the list." });
            }

            Db.NewsletterSubscribers.AddObject(new NewsletterSubscriber
            {
                Email = key,
                SubscribedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
            Db.SaveChanges();

            return Json(new { ok = true, message = "Thanks - you are on the list." });
        }

        public ActionResult Error()
        {
            return View();
        }

        // Reached through the catch-all route for any URL that matches no controller.
        public ActionResult NotFound()
        {
            return NotFoundPage();
        }
    }
}
