using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Utilities;

public record CreateUtilityTypeCommand(string Name, string? Description)
    : ITenantRequest, IRequest<UtilityTypeDto>;

public sealed class CreateUtilityTypeCommandValidator : AbstractValidator<CreateUtilityTypeCommand>
{
    public CreateUtilityTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

public sealed class CreateUtilityTypeCommandHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<CreateUtilityTypeCommand, UtilityTypeDto>
{
    public async Task<UtilityTypeDto> Handle(CreateUtilityTypeCommand request, CancellationToken cancellationToken)
    {
        TenantGuards.RequireManager(tenant, "manage utility types");

        var messId = tenant.MessId!.Value;
        await UtilityTypeCommandsGuard.EnsureNameFreeAsync(db, messId, request.Name, ignoreTypeId: null, cancellationToken);

        var type = new UtilityType
        {
            Id = Guid.NewGuid(),
            MessId = messId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.UtilityTypes.Add(type);
        await db.SaveChangesAsync(cancellationToken);

        return new UtilityTypeDto(type.Id, type.Name, type.Description, type.IsActive, type.CreatedAt);
    }
}

internal static class UtilityTypeCommandsGuard
{
    public static async Task EnsureNameFreeAsync(
        IApplicationContext db, Guid messId, string name, Guid? ignoreTypeId, CancellationToken ct)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var taken = await db.UtilityTypes.AsNoTracking().IgnoreQueryFilters()
            .AnyAsync(t => t.MessId == messId && t.Id != ignoreTypeId
                && t.Name.ToLower() == normalized, ct);
        if (taken)
        {
            throw new ConflictException("A utility type with this name already exists in the mess.");
        }
    }
}
