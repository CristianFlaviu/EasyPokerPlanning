using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Common;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
