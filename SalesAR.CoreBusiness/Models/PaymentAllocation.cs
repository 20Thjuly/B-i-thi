namespace SalesAR.CoreBusiness.Models;

public class PaymentAllocation
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public int InvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }
}
