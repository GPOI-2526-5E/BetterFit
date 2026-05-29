namespace Betterfit.Contracts.Reports;

public sealed record GymReportExportCatalogResponse(
    IReadOnlyCollection<GymReportExportSummaryResponse> Exports,
    DateTime GeneratedAtUtc);

public sealed record GymReportExportSummaryResponse(
    string Key,
    int RecordsCount);
