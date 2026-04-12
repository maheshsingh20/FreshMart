using ProductService.Domain;
using SharedKernel.CQRS;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace ProductService.Application.Commands;

/// <summary>
/// Handles the <c>CreateProductCommand</c> by enforcing SKU uniqueness and
/// delegating product creation to the domain factory method.
/// SKU uniqueness is checked at the application layer (not just the database)
/// to return a descriptive error message rather than a raw constraint violation.
/// Publishes no event on creation — downstream services discover new products
/// via the catalogue API rather than events.
/// </summary>
public class CreateProductHandler(IProductRepository repo, IEventPublisher events)
    : ICommandHandler<CreateProductCommand, Guid>
{
    /// <summary>
    /// Validates SKU uniqueness, creates the product aggregate, persists it,
    /// and returns the new product's ID.
    /// </summary>
    /// <returns>
    /// <see cref="Result{Guid}.Success"/> with the new product ID, or
    /// <see cref="Result{Guid}.Failure"/> if the SKU already exists.
    /// </returns>
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

/// <summary>
/// Handles the <c>UpdateStockCommand</c> by setting the product's stock to an
/// absolute quantity (e.g. after a physical stock count or a restock delivery).
/// Publishes an <see cref="InventoryUpdatedEvent"/> so downstream services
/// (e.g. notification service for low-stock alerts) can react to the change.
/// </summary>
public class UpdateStockHandler(IProductRepository repo, IEventPublisher events)
    : ICommandHandler<UpdateStockCommand>
{
    /// <summary>
    /// Loads the product, applies the new stock quantity, persists the change,
    /// and publishes an inventory update event.
    /// </summary>
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

/// <summary>
/// Handles the <c>DeductStockCommand</c> by reducing the product's stock by a
/// specified quantity. Used internally when an order is placed to atomically
/// reduce inventory. Publishes an <see cref="InventoryUpdatedEvent"/> carrying
/// both the new quantity and the previous quantity so consumers can compute the delta.
/// </summary>
public class DeductStockHandler(IProductRepository repo, IEventPublisher events)
    : ICommandHandler<DeductStockCommand>
{
    /// <summary>
    /// Loads the product, deducts the requested quantity via the domain method
    /// (which enforces the non-negative invariant), persists, and publishes the event.
    /// </summary>
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

/// <summary>
/// Handles the <c>CreateCategoryCommand</c> by creating a new product category
/// and persisting it. Categories form the top-level taxonomy of the product
/// catalogue and can optionally be nested via <c>ParentId</c>.
/// </summary>
public class CreateCategoryHandler(IProductRepository repo)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    /// <summary>
    /// Creates the category aggregate and persists it, returning the new category's ID.
    /// </summary>
    public async Task<Result<Guid>> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        var category = Category.Create(cmd.Name, cmd.Description, cmd.ImageUrl, cmd.ParentId);
        await repo.AddCategoryAsync(category, ct);
        return Result<Guid>.Success(category.Id);
    }
}
