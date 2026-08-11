using ERMS.Application.Abstractions.Persistence;
using ERMS.Application.Common.Models;
using ERMS.Domain.Entities;
using ERMS.Domain.Enums;
using ERMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Infrastructure.Repositories;

/// <summary>
/// Generic Repository'nin karşılayamadığı, birden çok tabloyu (Include) ve özel
/// filtre/sayfalama mantığı gerektiren Request sorguları burada toplanır.
/// Böylece Application katmanı EF Core'a (Include, AsNoTracking, IQueryable) bağımlı olmaz.
/// </summary>
public sealed class RequestQueryRepository : IRequestQueryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RequestQueryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<Request>> GetEmployeeRequestsAsync(
        int employeeId,
        RequestQuery query,
        CancellationToken cancellationToken = default)
    {
        var requestsQuery = _dbContext.Requests
            .AsNoTracking()
            .Include(x => x.RequestType)
            .Where(x => x.RequesterId == employeeId);

        if (query.Status is not null)
        {
            requestsQuery = requestsQuery.Where(x => x.Status == query.Status);
        }

        if (query.RequestTypeId is not null)
        {
            requestsQuery = requestsQuery.Where(x => x.RequestTypeId == query.RequestTypeId);
        }

        // Bölüm 8.3 bonus — "Global arama": yalnızca başlıkta değil, açıklamada ve talep
        // türü adında da arar.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            requestsQuery = requestsQuery.Where(x =>
                x.Title.Contains(query.Search) ||
                x.Description.Contains(query.Search) ||
                x.RequestType.Name.Contains(query.Search));
        }

        // Bölüm 8.3 bonus — "gelişmiş filtreleme (birden çok kritere göre)".
        if (query.Priority is not null)
        {
            requestsQuery = requestsQuery.Where(x => x.Priority == query.Priority);
        }

        if (query.CreatedFrom is not null)
        {
            requestsQuery = requestsQuery.Where(x => x.CreatedAt >= query.CreatedFrom);
        }

        if (query.CreatedTo is not null)
        {
            // Kullanıcı bir gün seçtiğinde ("10.08.2026") o günün tamamını kastediyor —
            // saat kısmını günün sonuna (23:59:59.999) tamamlıyoruz, aksi halde sadece
            // gece yarısından önceki kayıtlar dahil olurdu.
            var inclusiveEnd = query.CreatedTo.Value.Date.AddDays(1).AddTicks(-1);
            requestsQuery = requestsQuery.Where(x => x.CreatedAt <= inclusiveEnd);
        }

        if (query.MinAmount is not null)
        {
            requestsQuery = requestsQuery.Where(x => x.Amount >= query.MinAmount);
        }

        if (query.MaxAmount is not null)
        {
            requestsQuery = requestsQuery.Where(x => x.Amount <= query.MaxAmount);
        }

        var totalCount = await requestsQuery.CountAsync(cancellationToken);

        var items = await requestsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Request>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<Request>> GetPendingManagerRequestsAsync(
        int managerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Requests
            .AsNoTracking()
            .Include(x => x.Requester)
            .Include(x => x.RequestType)
            .Where(x =>
                x.Status == RequestStatus.Pending &&
                x.Requester.ManagerId == managerId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Request?> GetDetailAsync(
        int requestId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Requests
            .AsNoTracking()
            .Include(x => x.RequestType)
            .Include(x => x.Requester)
            .Include(x => x.Approvals)
                .ThenInclude(a => a.Approver)
            .Include(x => x.Comments)
                .ThenInclude(c => c.Author)
            .Include(x => x.History.OrderBy(h => h.ChangedAt))
                .ThenInclude(h => h.ChangedBy)
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }
}
