using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;

namespace CampusEatsv2.Infrastructure.Services.SharedServices;

// Using fluentvalidation and mediatr for validation and command handling
public class ProductService
{
    public class CreateProductCommand : IRequest<Product>
    {
        public Guid ProductId { get; set; }
        public required string Title { get; set; }
        public decimal Price { get; set; }
        public required string ImageUrl { get; set; }
        public required string Description { get; set; }
    }

    // Things to validate:
    // - ProductId should exist and not be null
    // - Title should not be empty or null
    // - Price should be greater than 0
    // - ImageUrl should not be empty or null
    // - Description should not be empty or null
    private class Validator : AbstractValidator<CreateProductCommand>
    {
        public Validator()
        {
            RuleFor(valid => valid.ProductId)
                .NotEmpty().WithMessage("ProductId cannot be empty.")
                .NotNull().WithMessage("ProductId cannot be null.");
           RuleFor(valid => valid.Title)
                .NotEmpty().WithMessage("Title cannot be empty.")
                .NotNull().WithMessage("Title is required.");
            RuleFor(valid => valid.Price).
                GreaterThan(0).WithMessage("Price cannot be negative.");
            RuleFor(valid => valid.ImageUrl)
                .NotEmpty().WithMessage("ImageUrl cannot be empty.")
                .NotNull().WithMessage("ImageUrl cannot be null.");
            RuleFor(valid => valid.Description)
                .NotEmpty().WithMessage("Description cannot be empty.")
                .NotNull().WithMessage("Description cannot be null.");
        }
    }

    public class CreateProductHandler : IRequestHandler<CreateProductCommand, Product>
    {
        private readonly AppDbContext _context;
        public CreateProductHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Product> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var validator = new Validator();
            validator.ValidateAndThrow(request);

            var productDTO = new Product
            {
                ProductId = request.ProductId,
                Title = request.Title,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                Description = request.Description
            };
            _context.Add(productDTO);
            await _context.SaveChangesAsync(cancellationToken);
            return productDTO; // return the productDTO
        }
    }
}