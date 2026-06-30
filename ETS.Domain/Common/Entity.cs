namespace ETS.Domain.Common
{
    public abstract class Entity<T> : EntityBase
    {
        protected Entity() { }

        protected Entity(T id)
        {
            Id = id;
        }

        public T Id { get; init; } = default!;

        public Guid OutBoxId { get; set; }
    }
}
