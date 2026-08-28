using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testing.Application.Abstractions.Data;
using Testing.Infrastructure.Persistence.Zam;

namespace Testing.Infrastructure.Persistence;

internal sealed class ApplicationDbContext(IDbContextFactory<ZemogContext> contextFactory) : IApplicationDbContext
{
    public async Task<IReadOnlyList<TResult>> QueryAsync<TResult>(string sql, IReadOnlyCollection<QueryParameter> parameters, CancellationToken cancellationToken = default) where TResult : class
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var sqlParameters = parameters
            .Select(p => new SqlParameter(p.Name, p.Value ?? DBNull.Value))
            .ToArray();

        return await context.Database
            .SqlQueryRaw<TResult>(sql, sqlParameters)
            .ToListAsync(cancellationToken);
    }
}
