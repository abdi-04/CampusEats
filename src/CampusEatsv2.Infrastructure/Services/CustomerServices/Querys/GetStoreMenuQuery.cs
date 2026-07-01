using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.CustomerServices;

public class GetStore
{
    public record ProductDto(
        Guid ProductId,
        string Title,
        string Description,
        decimal Price,
        string? ImageUrl
    );
    public class GetStoreMenuQuery : IRequest<List<ProductDto>> {
        // No need to show products, would take them from db anyway
        //public List<Product> Products { get; set;}
    }

    public class GetStoreMenuHandler : IRequestHandler<GetStoreMenuQuery, List<ProductDto>>
    {
        private readonly AppDbContext _context;

        public GetStoreMenuHandler(AppDbContext context) => _context = context;


        public async Task<List<ProductDto>> Handle(GetStoreMenuQuery request, CancellationToken ct)
        {
            // Fetch all products and map to DTO
            var products = await _context.Products
                .Select(p => new ProductDto(
                    p.ProductId,
                    p.Title,
                    p.Description,
                    p.Price,
                    p.ImageUrl
                ))
                .ToListAsync(ct);

            return products;
        }
    }

}

