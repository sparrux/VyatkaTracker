namespace Hub.Domain.Common.DomainEvents;

public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent() : this(Guid.NewGuid())
    {
    }

    protected DomainEvent(Guid eventId)
    {
        EventId = eventId;
        OccurredOn = DateTimeOffset.UtcNow;
    }
    
    public Guid EventId { get; }
    public DateTimeOffset OccurredOn { get; }
}