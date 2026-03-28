namespace Trace.Api.Repositories;

public interface ICosmosDbService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
