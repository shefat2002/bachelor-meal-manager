using MealManager.Application.Domain;

namespace MealManager.Application.Abstractions;

/// <summary>Resolved tenant for the current request; populated by TenantResolutionBehavior before handlers run.</summary>
public interface ITenantContext
{
    Guid? MessId { get; }

    MessRole? Role { get; }
}

public sealed class TenantContext : ITenantContext
{
    public Guid? MessId { get; private set; }

    public MessRole? Role { get; private set; }

    public void Set(Guid messId, MessRole role)
    {
        MessId = messId;
        Role = role;
    }
}

/// <summary>Marker: the MediatR pipeline resolves X-Mess-Id + membership before these requests reach a handler.</summary>
public interface ITenantRequest;
