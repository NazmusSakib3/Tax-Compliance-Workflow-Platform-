namespace TaxCompliance.Application.Common;

public class PagedListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
}
