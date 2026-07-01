using CampusEats.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Infrastructure.Services.AdminServices;

/// Get all orders in the system (admin only)

public class GetAllOrdersQuery
{
    public class GetAllOrdersQueryRequest : IRequest<List<Order>>
    {
    }

    public class GetAllOrdersQueryHandler
        : IRequestHandler<GetAllOrdersQueryRequest, List<Order>>
    {
        private readonly AppDbContext _context;

        public GetAllOrdersQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> Handle(
            GetAllOrdersQueryRequest request,
            CancellationToken cancellationToken)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)  
                .Include(o => o.Payment)     
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
    }
}