using System.Collections.Generic;

namespace WukongWeb.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public Game Game { get; set; }
        public Edition Edition { get; set; }
        public string Email { get; set; }
        public decimal Total { get; set; }
    }

    public class ReceiptViewModel
    {
        public Order Order { get; set; }
        public IList<OrderDetail> Details { get; set; }
        public User User { get; set; }
    }
}
