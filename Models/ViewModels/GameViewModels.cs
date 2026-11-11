using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using WukongWeb.Infrastructure;

namespace WukongWeb.Models.ViewModels
{
    public class GamesIndexViewModel
    {
        public PagedResult<Game> Games { get; set; }
        public string Query { get; set; }
        public int? CategoryId { get; set; }
        public string Sort { get; set; }
        public IList<Category> Categories { get; set; }
    }

    public class GameDetailsViewModel
    {
        public Game Game { get; set; }
        public IList<Edition> Editions { get; set; }
        public IList<Review> Reviews { get; set; }
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool InWishlist { get; set; }
        public bool HasReviewed { get; set; }
        public ReviewViewModel NewReview { get; set; }
    }

    public class ReviewViewModel
    {
        [Required]
        public int GameId { get; set; }

        [Required(ErrorMessage = "Choose a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment must be 1000 characters or fewer.")]
        [Display(Name = "Comment")]
        public string Comment { get; set; }
    }

    public class WishlistViewModel
    {
        public IList<Game> Games { get; set; }
    }

    public class GameEditViewModel
    {
        public int GameId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(160, ErrorMessage = "Title must be 160 characters or fewer.")]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Slug is required.")]
        [StringLength(160)]
        [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Use lowercase letters, numbers and single hyphens.")]
        [Display(Name = "Slug")]
        public string Slug { get; set; }

        [StringLength(4000)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Choose a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Developer is required.")]
        [StringLength(120)]
        [Display(Name = "Developer")]
        public string Developer { get; set; }

        [Required(ErrorMessage = "Publisher is required.")]
        [StringLength(120)]
        [Display(Name = "Publisher")]
        public string Publisher { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Release date")]
        public DateTime? ReleaseDate { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, 999999999, ErrorMessage = "Price must be zero or greater.")]
        [Display(Name = "Base price")]
        public decimal BasePrice { get; set; }

        [StringLength(255)]
        [Display(Name = "Cover image path")]
        public string CoverImage { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        public IList<SelectListItem> CategoryOptions { get; set; }
    }
}
