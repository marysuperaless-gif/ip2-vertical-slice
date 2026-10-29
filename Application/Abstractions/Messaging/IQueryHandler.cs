using Application.Features.Admissions.GetAllAdmissions;
using Domain.Common;

namespace Application.Abstractions.Messaging;

public interface IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
