public class CustomerLookupDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? GstNumber { get; set; }
    public string? BillingAddressLine { get; set; }
    public string? ShippingAddressLine { get; set; }
}
