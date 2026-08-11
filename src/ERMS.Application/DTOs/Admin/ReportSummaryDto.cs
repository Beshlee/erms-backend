namespace ERMS.Application.DTOs.Admin;

/// <summary>Bölüm 8.3 bonus — admin rapor ekranı, GET /api/admin/reports/summary yanıtı.</summary>
public sealed record ReportSummaryDto(
    int TotalRequests,
    IReadOnlyList<StatusCountDto> ByStatus,
    IReadOnlyList<TypeCountDto> ByType,
    IReadOnlyList<DepartmentCountDto> ByDepartment);

public sealed record StatusCountDto(string Status, int Count);

public sealed record TypeCountDto(string TypeName, int Count);

public sealed record DepartmentCountDto(string DepartmentName, int Count);
