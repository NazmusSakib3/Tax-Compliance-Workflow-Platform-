namespace TaxCompliance.Application.Dashboard;

public class DashboardBreakdownItemDto
{
    public string Name { get; set; } = string.Empty;

    public int OpenCount { get; set; }

    public int OverdueCount { get; set; }
}
