namespace Testing.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    Task<IReadOnlyList<TResult>> QueryAsync<TResult>(
        string sql,
        IReadOnlyCollection<QueryParameter> parameters,
        CancellationToken cancellationToken = default) where TResult : class;
}
