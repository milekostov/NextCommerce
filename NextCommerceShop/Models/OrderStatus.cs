namespace NextCommerceShop.Models
{
    public enum OrderStatus
    {
        Pending = 0,     // created, waiting payment or confirmation
        Paid = 1,        // payment received (if you add payments later)
        Processing = 2,  // preparing / packing
        Shipped = 3,     // sent out
        Completed = 4,   // delivered / closed
        Cancelled = 5,
        Refunded = 6
    }
}
