using System;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using WukongWeb.Infrastructure;
using WukongWeb.Models;
using WukongWeb.Models.ViewModels;

namespace WukongWeb.Controllers
{
    [Authorize]
    public class CheckoutController : BaseController
    {
        // gameId / editionId come from the catalogue or a game details page.
        // With no query string the landing page keeps its original behaviour
        // and buys the featured game's cheapest edition.
        public ActionResult Index(int? gameId, int? editionId)
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                var checkoutUrl = Url.Action("Index", "Checkout", new { gameId, editionId });
                return Redirect("~/Account/Login?returnUrl=" + HttpUtility.UrlEncode(checkoutUrl));
            }

            var selection = ResolveSelection(gameId, editionId);
            if (selection == null)
            {
                return NotFoundPage();
            }

            return View(new CheckoutViewModel
            {
                Game = selection.Game,
                Edition = selection.Edition,
                Email = user.Email,
                Total = selection.Edition.Price
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PlaceOrder(bool confirmDemoPayment, int? editionId)
        {
            if (!confirmDemoPayment)
            {
                Response.StatusCode = 400;
                return Json(new { error = "Confirm the demo payment to create the order." });
            }

            var user = CurrentUser;
            if (user == null)
            {
                Response.StatusCode = 401;
                return Json(new { error = "Log in to your account before checkout." });
            }

            var selection = ResolveSelection(null, editionId);
            if (selection == null)
            {
                Response.StatusCode = 400;
                return Json(new { error = "That edition is no longer on sale. Pick one from the catalogue." });
            }

            var game = selection.Game;
            var edition = selection.Edition;

            Order order;

            // The price comes from the database, never from the browser.
            using (var scope = new TransactionScope())
            {
                order = new Order
                {
                    UserId = user.UserId,
                    OrderDate = DateTime.UtcNow,
                    Status = "Paid",
                    PaymentMethod = "Demo checkout",
                    TotalAmount = edition.Price,
                    Note = "Coursework demo order"
                };

                Db.Orders.AddObject(order);
                Db.SaveChanges();

                Db.OrderDetails.AddObject(new OrderDetail
                {
                    OrderId = order.OrderId,
                    EditionId = edition.EditionId,
                    Quantity = 1,
                    UnitPrice = edition.Price
                });
                Db.SaveChanges();

                scope.Complete();
            }

            return Json(new
            {
                purchase = new
                {
                    id = order.OrderId,
                    product = game.Title,
                    edition = edition.Name,
                    amount = edition.Price,
                    currency = "VND",
                    paymentMethod = "Demo checkout",
                    status = "Paid",
                    paidAtUtc = order.OrderDate.ToString("s")
                },
                demo = true
            });
        }

        public ActionResult Receipt(int id)
        {
            var user = CurrentUser;
            if (user == null)
            {
                AuthService.SignOut();
                return RedirectToAction("Login", "Account");
            }

            var isAdmin = User.IsInRole("Admin");
            var order = Db.Orders.Include("User").FirstOrDefault(o =>
                o.OrderId == id && (isAdmin || o.UserId == user.UserId));
            if (order == null)
            {
                // Do not reveal whether another customer's order exists.
                return NotFoundPage();
            }

            return View(new ReceiptViewModel
            {
                Order = order,
                Details = Db.OrderDetails.Where(d => d.OrderId == id).ToList(),
                User = order.User
            });
        }

        /// <summary>
        /// Resolves what the customer is buying. An explicit edition wins, then an
        /// explicit game (cheapest active edition), then the featured game so the
        /// landing page purchase button keeps working without a query string.
        /// Everything is re-checked against the database, so a hand-edited URL or
        /// a deactivated record cannot slip an invalid product into an order.
        /// </summary>
        private Selection ResolveSelection(int? gameId, int? editionId)
        {
            if (editionId.HasValue && editionId.Value > 0)
            {
                var wantedId = editionId.Value;
                var edition = Db.Editions.FirstOrDefault(e => e.EditionId == wantedId && e.IsActive);
                if (edition == null)
                {
                    return null;
                }

                var ownerId = edition.GameId;
                var game = Db.Games.FirstOrDefault(g => g.GameId == ownerId && g.IsActive);
                return game == null ? null : new Selection(game, edition);
            }

            Game selected;
            if (gameId.HasValue && gameId.Value > 0)
            {
                var wantedGameId = gameId.Value;
                selected = Db.Games.FirstOrDefault(g => g.GameId == wantedGameId && g.IsActive);
                if (selected == null)
                {
                    return null;
                }
            }
            else
            {
                selected = FeaturedGame();
                if (selected == null)
                {
                    return null;
                }
            }

            var selectedId = selected.GameId;
            var cheapest = Db.Editions
                .Where(e => e.GameId == selectedId && e.IsActive)
                .OrderBy(e => e.Price)
                .FirstOrDefault();

            return cheapest == null ? null : new Selection(selected, cheapest);
        }

        /// <summary>A game plus the edition the customer actually pays for.</summary>
        private sealed class Selection
        {
            public Selection(Game game, Edition edition)
            {
                Game = game;
                Edition = edition;
            }

            public Game Game { get; private set; }

            public Edition Edition { get; private set; }
        }

        private Game FeaturedGame()
        {
            return Db.Games.FirstOrDefault(g => g.Slug == "black-myth-wukong" && g.IsActive)
                ?? Db.Games.Where(g => g.IsActive).OrderBy(g => g.GameId).FirstOrDefault();
        }
    }
}
