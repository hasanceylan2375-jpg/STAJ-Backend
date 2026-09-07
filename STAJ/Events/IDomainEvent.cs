namespace STAJ.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOnUtc { get; }
    }
}
