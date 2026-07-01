using CampusEatsv2.Core.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CampusEatsv2.Infrastructure.Services.AdminServices;

public class ManageProductsAdmin
{
	public class GetProductsQuery : IRequest<List<Product>>
	{
	}

	public class CreateProductCommand : IRequest<Product>
	{
		public Guid ProductId { get; set; }
		public string Title { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public decimal DeliveryFee { get; set; }
		public string ImageUrl { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}

	public class UpdateProductCommand : IRequest<Product>
	{
		public Guid ProductId { get; set; }
		public string Title { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public decimal DeliveryFee { get; set; }
		public string ImageUrl { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}

	public class DeleteProductCommand : IRequest<Unit>
	{
		public Guid ProductId { get; set; }
	}

	private class CreateProductValidator : AbstractValidator<CreateProductCommand>
	{
		public CreateProductValidator()
		{
			RuleFor(x => x.ProductId).NotEmpty();
			RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.Price).GreaterThan(0);
			RuleFor(x => x.DeliveryFee).GreaterThan(0);
			RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
			RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
		}
	}

	private class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
	{
		public UpdateProductValidator()
		{
			RuleFor(x => x.ProductId).NotEmpty();
			RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.Price).GreaterThan(0);
			RuleFor(x => x.DeliveryFee).GreaterThan(0);
			RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(500);
			RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
		}
	}

	private class DeleteProductValidator : AbstractValidator<DeleteProductCommand>
	{
		public DeleteProductValidator()
		{
			RuleFor(x => x.ProductId).NotEmpty();
		}
	}

	public class GetProductsHandler : IRequestHandler<GetProductsQuery, List<Product>>
	{
		private readonly AppDbContext _context;

		public GetProductsHandler(AppDbContext context)
		{
			_context = context;
		}

		public async Task<List<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
		{
			return await _context.Products
				.OrderBy(p => p.Title)
				.ToListAsync(cancellationToken);
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
			new CreateProductValidator().ValidateAndThrow(request);

			var duplicateTitle = await _context.Products.AnyAsync(
				p => p.Title.ToLower() == request.Title.ToLower(),
				cancellationToken);

			if (duplicateTitle)
			{
				throw new InvalidOperationException("A product with this title already exists.");
			}

			var product = new Product
			{
				ProductId = request.ProductId,
				Title = request.Title,
				Price = request.Price,
				DeliveryFee = request.DeliveryFee,
				ImageUrl = request.ImageUrl,
				Description = request.Description
			};

			_context.Products.Add(product);
			await _context.SaveChangesAsync(cancellationToken);
			return product;
		}
	}

	public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, Product>
	{
		private readonly AppDbContext _context;

		public UpdateProductHandler(AppDbContext context)
		{
			_context = context;
		}

		public async Task<Product> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
		{
			new UpdateProductValidator().ValidateAndThrow(request);

			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken)
				?? throw new KeyNotFoundException("Product not found.");

			var duplicateTitle = await _context.Products.AnyAsync(
				p => p.ProductId != request.ProductId && p.Title.ToLower() == request.Title.ToLower(),
				cancellationToken);

			if (duplicateTitle)
			{
				throw new InvalidOperationException("A product with this title already exists.");
			}

			product.Title = request.Title;
			product.Price = request.Price;
			product.DeliveryFee = request.DeliveryFee;
			product.ImageUrl = request.ImageUrl;
			product.Description = request.Description;

			await _context.SaveChangesAsync(cancellationToken);
			return product;
		}
	}

	public class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Unit>
	{
		private readonly AppDbContext _context;

		public DeleteProductHandler(AppDbContext context)
		{
			_context = context;
		}

		public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
		{
			new DeleteProductValidator().ValidateAndThrow(request);

			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken)
				?? throw new KeyNotFoundException("Product not found.");

			_context.Products.Remove(product);
			await _context.SaveChangesAsync(cancellationToken);
			return Unit.Value;
		}
	}
}
