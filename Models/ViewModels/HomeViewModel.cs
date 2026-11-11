using System.Collections.Generic;

namespace WukongWeb.Models.ViewModels
{
    public class HomeViewModel
    {
        public IList<MediaAsset> MediaItems { get; set; }
        public IList<Award> Awards { get; set; }

        /// <summary>Titles that are actually on sale, with their cheapest active edition.</summary>
        public IList<StoreItemViewModel> StoreItems { get; set; }
    }

    public class StoreItemViewModel
    {
        public int GameId { get; set; }
        public string Title { get; set; }
        public string CategoryName { get; set; }
        public string CoverImage { get; set; }
        public int EditionId { get; set; }
        public string EditionName { get; set; }
        public decimal Price { get; set; }
    }
}
