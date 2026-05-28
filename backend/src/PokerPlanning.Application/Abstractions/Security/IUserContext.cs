namespace PokerPlanning.Application.Abstractions.Security;

public interface IUserContext
{
    Guid? CurrentUserId { get; }
}
