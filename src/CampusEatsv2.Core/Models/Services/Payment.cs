namespace CampusEatsv2.Core.Models;
// Creates Payment for Order that includes all neccesary information
// We have different options for payment
// All is mock and we need Id for all of this to make it work
// Payment status was addded for processing
public class Payment
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public PaymentMethods Method { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
}
public enum PaymentMethods
{
    Paypal,
    Vipps,
    CreditCard
}
public enum PaymentStatus
{
    Paid,
    Pending,
    Failed
}