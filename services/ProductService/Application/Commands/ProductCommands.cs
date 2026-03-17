using FluentValidation;
using ProductService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace ProductService.Application.Commands;

public record CreateProductCommand(string Name, string Description, decimal Price,
    string SKU, string ImageUrl, Guid CategoryId, int InitialStock,
    string? Brand, decimal? Weight, string? Unit) : ICommand<Guid>;

public record UpdateStockCommand(Guid ProductId, int NewQuantity) : ICommand;
public record DeductStockCommand(Guid ProductId, int Quantity) : ICommand;
public record UpdateProductPriceCommand(Guid ProductId, decimal NewPrice) : ICommand;
public record CreateCategoryCommand(string Name, string? Description, string? ImageUrl, Guid? ParentId) : ICommand<Guid>;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.SKU).NotEmpty();
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
    }
}
