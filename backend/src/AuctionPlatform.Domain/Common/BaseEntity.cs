namespace AuctionPlatform.Domain.Common;

/// <summary>
/// Base class untuk semua entity. Menyediakan Id dan mekanisme domain event
/// (opsional dipakai nanti kalau butuh side-effect seperti "OnBidPlaced").
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
