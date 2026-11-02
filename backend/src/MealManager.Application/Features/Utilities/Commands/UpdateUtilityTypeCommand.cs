using FluentValidation;
using MealManager.Application.Abstractions;
using MealManager.Application.Domain;
using MealManager.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MealManager.Application.Features.Utilities;

public record UpdateUtilityTypeCommand(Guid Id, string Name, string? Description, bool IsActive)
    : ITenantRequest, IRequest<UtilityTypeDto>;

public sealed class UpdateUtilityTypeCommandValidator : AbstractValidator<UpdateUtilityTypeCommand>
{
    public UpdateUtilityTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(300);
    }
}

public sealed class UpdateUtilityTypeCommandHandler(IApplicationContext db, ITenantContext tenant)
    : IRequestHandler<UpdateUtilityTypeCommand, UtilityTypeDto>
{
    public async Task<UtilityTypeDto> Handle(UpdateUtilityTypeCommand request, CancellationToken cancellationToken)
    {
        TenantGuards.RequireManager(tenant, "manage utility types");

        var type = await db.UtilityTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.MessId == tenant.MessId!.Value && t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Utility type not found.");

        await UtilityTypeCommandsGuard.EnsureNameFreeAsync(db, tenant.MessId!.Value, request.Name, type.Id, cancellationToken);

        type.Name = request.Name.Trim();
        type.Description = request.Description?.Trim();
        type.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);

        return new UtilityTypeDto(type.Id, type.Name, type.Description, type.IsActive, type.CreatedAt);
    }
}
