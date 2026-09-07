namespace STAJ.Events
{
    public interface IDomainEventDispatcher
    {
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
