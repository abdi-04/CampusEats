using CampusEats.Core.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Infrastructure.Services.AdminServices;
// Returns list of all Couriers
public class GetAllCouriers
{
    public class GetAllCouriersQuery : IRequest<List<Courier>>
    {
    }

    public class Handler : IRequestHandler<GetAllCouriersQuery, List<Courier>>
    {
        private readonly AppDbContext _context;

        public Handler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Courier>> Handle(GetAllCouriersQuery request, CancellationToken cancellationToken)
        {
            return await _context.Couriers.ToListAsync(cancellationToken);
        }
    }
}