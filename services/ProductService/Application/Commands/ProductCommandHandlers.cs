using ProductService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace ProductService.Application.Commands;

public class CreateProductHandler(IProductRepository repo, IEventPublisher events)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand cmd, CancellationToken ct)
    {
        if (await repo.GetBySkuAsync(cmd.SKU, ct) is not null)
            return Result<Guid>.Failure($"SKU '{cmd.SKU}' already exists.");

        var product = Product.Create(cmd.Name, cmd.Description, cmd.Price,
            cmd.SKU, cmd.ImageUrl, cmd.CategoryId, cmd.InitialStock,
            cmd.Brand, cmd.Weight, cmd.Unit);

        await repo.AddAsync(product, ct);
        return Result<Guid>.Success(product.Id);
    }
}

public class UpdateStockHandler(IProductRepository repo, IEventPublisher events)
    : ICommandHandler<UpdateStockCommand>
{
    public async Task<Result> Handle(UpdateStockCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result.Failure("Product not found.");

        product.UpdateStock(cmd.NewQuantity);
        await repo.UpdateAsync(product, ct);

        await events.PublishAsync(new InventoryUpdatedEvent(
            product.Id, cmd.NewQuantity, 0, DateTime.UtcNow), ct);

        return Result.Success();
    }
}

public class DeductStockHandler(IProductRepository repo, IEventPublisher events)
    : ICommandHandler<DeductStockCommand>
{
    public async Task<Result> Handle(DeductStockCommand cmd, CancellationToken ct)
    {
        var product = await repo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return Result.Failure("Product not found.");

        product.DeductStock(cmd.Quantity);
        await repo.UpdateAsync(product, ct);

        await events.PublishAsync(new InventoryUpdatedEvent(
            product.Id, product.StockQuantity, product.StockQuantity + cmd.Quantity, DateTime.UtcNow), ct);

        return Result.Success();
    }
}

public class CreateCategoryHandler(IProductRepository repo)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        var category = Category.Create(cmd.Name, cmd.Description, cmd.ImageUrl, cmd.ParentId);
        await repo.AddCategoryAsync(category, ct);
        return Result<Guid>.Success(category.Id);
    }
}
