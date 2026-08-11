using ERMS.Application.Abstractions.Persistence;
using ERMS.Domain.Enums;
using ERMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Infrastructure.Repositories;

/// <summary>
/// Admin rapor ekranı (bonus) için GroupBy/Count sorguları — RequestQueryRepository ile
/// aynı gerekçeyle, generic Repository'nin dışında ayrı bir query repository'de toplanır.
/// </summary>
public sealed class ReportQueryRepository : IReportQueryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ReportQueryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> GetTotalRequestCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Requests.AsNoTracking().CountAsync(cancellationToken);
    }

    public async Task<List<(RequestStatus Status, int Count)>> GetCountsByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.Requests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.Select(x => (x.Status, x.Count)).ToList();
    }

    public async Task<List<(string TypeName, int Count)>> GetCountsByTypeAsync(
        CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.Requests
            .AsNoTracking()
            .GroupBy(r => r.RequestType.Name)
            .Select(g => new { TypeName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.Select(x => (x.TypeName, x.Count)).ToList();
    }

    public async Task<List<(string DepartmentName, int Count)>> GetCountsByDepartmentAsync(
        CancellationToken cancellationToken = default)
    {
        var grouped = await _dbContext.Requests
            .AsNoTracking()
            .GroupBy(r => r.Requester.Department.Name)
            .Select(g => new { DepartmentName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.Select(x => (x.DepartmentName, x.Count)).ToList();
    }
}
