using CampusEatsv2.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CustomerServices;

/// <summary>
/// Query to get all orders for a specific customer
/// </summary>
public class GetCustomerOrdersQuery
{
    public class GetCustomerOrdersQueryRequest : IRequest<List<OrderDashboardDto>>
    {
        public Guid CustomerId { get; set; }
    }

    public class GetCustomerOrdersQueryHandler : IRequestHandler<GetCustomerOrdersQueryRequest, List<OrderDashboardDto>>
    {
        private readonly AppDbContext _context;

        public GetCustomerOrdersQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDashboardDto>> Handle(GetCustomerOrdersQueryRequest request, CancellationToken cancellationToken)
        {
            var orders = await _context.Orders
                .Where(o => o.CustomerId == request.CustomerId)
                .Select(o => new OrderDashboardDto
                {
                    OrderId = o.OrderId,
                    CustomerId = o.CustomerId,
                    CourierId = o.CourierId,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    CreationTime = o.CreationTime
                })
                .ToListAsync(cancellationToken);

            return orders;
        }
    }
}
