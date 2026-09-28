using MediatR;

namespace MaintenanceChronicle.Application.Contracts.Utils.Queries;
/// <summary>
/// Query to get list of entities.
/// </summary>
/// <typeparam name="TEntity">Entity type to query</typeparam>
/// <returns>List of desired entities</returns>
public record GetListOfEntityQuery<TEntity>(PageRequest? PageRequest = null) : IRequest<List<TEntity>>;
