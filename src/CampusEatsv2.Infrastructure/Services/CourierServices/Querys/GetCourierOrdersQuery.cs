using CampusEatsv2.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CourierServices;

/// Get all orders assigned to a specific courier
public class GetCourierOrdersQuery
{
    public class GetCourierOrdersQueryRequest : IRequest<List<OrderDashboardDto>>
    {
        public Guid CourierId { get; set; }
    }

    public class GetCourierOrdersQueryHandler : IRequestHandler<GetCourierOrdersQueryRequest, List<OrderDashboardDto>>
    {
        private readonly AppDbContext _context;

        public GetCourierOrdersQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDashboardDto>> Handle(GetCourierOrdersQueryRequest request, CancellationToken cancellationToken)
        {
            var orders = await _context.Orders
                .Where(o => o.CourierId == request.CourierId)
                .Select(o => new OrderDashboardDto
                {
                    OrderId = o.OrderId,
                    CustomerId = o.CustomerId,
                    CourierId = o.CourierId,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    DeliveryFee = o.DeliveryFee,
                    Tip = o.Tip,
                    CreationTime = o.CreationTime,
                })
                .ToListAsync(cancellationToken);

            return orders;
        }
    }
}
