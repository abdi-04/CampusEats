namespace CampusEatsv2.Core.Models;




/// <summary>
/// This is a static utility class 
/// that defines the payment split logic between couriers and the platform.

// The platform takes a percentage of the delivery fee, and the courier 
///receives the rest plus any tips.
/// </summary>
public static class PaymentRules
{
    public const decimal CourierSharePercentage = 0.80m;

    public static decimal CalculateCourierEarning(
        decimal deliveryFee,
        decimal tip,
        OrderStatus status)
    {
        var courierFee = deliveryFee * CourierSharePercentage;

        if (status == OrderStatus.Delivered)
            return courierFee + tip;

        return courierFee;
    }

    public static decimal CalculatePlatformFee(decimal deliveryFee)
        => deliveryFee * (1 - CourierSharePercentage);
}