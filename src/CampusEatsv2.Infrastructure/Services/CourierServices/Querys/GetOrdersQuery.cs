using CampusEatsv2.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CourierServices;

/// Get all available orders that has Status "Submitted", "Accepted", "PickedUp"
public class GetOrdersQuery : IRequest<List<Order>> { }

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, List<Order>>
{
    private readonly AppDbContext _context;

    public GetOrdersHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Where(o =>
                o.Status == OrderStatus.Submitted ||
                o.Status == OrderStatus.Accepted ||
                o.Status == OrderStatus.PickedUp)
            .ToListAsync(cancellationToken);
    }
}
