using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.OrderServices;

public class PaymentService
{
    public class ProcessPaymentCommand : IRequest<bool>
    {
        public Guid OrderId { get; set; }
        public PaymentMethods PaymentMethod { get; set; }
        public decimal Amount { get; set; }
    }

    private class Validator : AbstractValidator<ProcessPaymentCommand>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).NotEmpty();                
            RuleFor(x => x.PaymentMethod)
                .IsInEnum()
                .WithMessage("Invalid payment method");
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Payment amount must be greater than zero");
        }
    }

    public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentCommand, bool>
    {
        private readonly AppDbContext _context;

        public ProcessPaymentHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
        {
            new Validator().ValidateAndThrow(request);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
            if (order == null) return false;

            var payment = order.Payment;
            if (payment == null) return false;
            if (payment.Status == PaymentStatus.Paid) return false; // already paid

            var expectedAmount = order.TotalAmount;
            if (request.Amount != expectedAmount) return false;

            // Simulate payment processing logic here
            // For example, integrate with a payment gateway API from Klarna eg...

            // If payment is successful, update payment and order status
            payment.Method = request.PaymentMethod;
            payment.Amount = request.Amount;
            payment.Status = PaymentStatus.Paid;
            order.Status = OrderStatus.Submitted; // Courier must accept!;

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}