namespace ETS.Domain.Contracts
{
    public interface IUserContext
    {
        string? UserId { get; }
        string? UserName { get; }
        string? UserEmail { get; }
        string? UserPhone { get; }
    }
}
