using System.Collections.Generic;

namespace WukongWeb.Models.ViewModels
{
    public class ProfilePageViewModel
    {
        public ProfileViewModel Profile { get; set; }
        public ChangePasswordViewModel Password { get; set; }
        public IList<Order> Orders { get; set; }
        public IList<Review> Reviews { get; set; }
        public IList<Game> Wishlist { get; set; }
    }
}
