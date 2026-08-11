using ERMS.Domain.Enums;

namespace ERMS.Application.Abstractions.Persistence;

/// <summary>
/// Admin rapor ekranı (bonus) için toplama (GroupBy/Count) sorguları — generic
/// Repository'nin karşılayamadığı, Include/join gerektiren bir başka örnek
/// (bkz. <see cref="IRequestQueryRepository"/> ile aynı gerekçe).
/// </summary>
public interface IReportQueryRepository
{
    Task<int> GetTotalRequestCountAsync(CancellationToken cancellationToken = default);

    Task<List<(RequestStatus Status, int Count)>> GetCountsByStatusAsync(
        CancellationToken cancellationToken = default);

    Task<List<(string TypeName, int Count)>> GetCountsByTypeAsync(
        CancellationToken cancellationToken = default);

    Task<List<(string DepartmentName, int Count)>> GetCountsByDepartmentAsync(
        CancellationToken cancellationToken = default);
}
