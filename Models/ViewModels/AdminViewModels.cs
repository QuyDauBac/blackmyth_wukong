using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using WukongWeb.Infrastructure;

namespace WukongWeb.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int UserCount { get; set; }
        public int GameCount { get; set; }
        public int OrderCount { get; set; }
        public int ReviewCount { get; set; }
        public decimal Revenue { get; set; }
        public int SubscriberCount { get; set; }
        public IList<NewsletterSubscriber> RecentSubscribers { get; set; }
        public IList<Order> RecentOrders { get; set; }
        public IList<TopGameRow> TopGames { get; set; }
    }

    public class TopGameRow
    {
        public int GameId { get; set; }
        public string Title { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminGamesViewModel
    {
        public PagedResult<Game> Games { get; set; }
        public string Query { get; set; }
    }

    public class AdminOrdersViewModel
    {
        public PagedResult<Order> Orders { get; set; }
        public string Query { get; set; }
    }

    public class AdminUsersViewModel
    {
        public PagedResult<User> Users { get; set; }
        public string Query { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(80, MinimumLength = 2)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 10, ErrorMessage = "Use a password between 10 and 128 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm the password.")]
        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Choose a role.")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public IList<SelectListItem> RoleOptions { get; set; }
    }

    public class UserEditViewModel
    {
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(80, MinimumLength = 2)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(254)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Choose a role.")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public IList<SelectListItem> RoleOptions { get; set; }
    }
}
