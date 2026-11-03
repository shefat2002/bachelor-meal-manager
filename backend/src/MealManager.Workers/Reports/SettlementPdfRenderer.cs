using MealManager.Application.Features.Summary;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MealManager.Workers.Reports;

/// <summary>
/// Renders the monthly settlement (§5 summary) to PDF with QuestPDF (Community license, declared
/// once in AddBmmWorkers). Server-computed rows only — the renderer never recomputes money math.
/// </summary>
public static class SettlementPdfRenderer
{
    public static byte[] Render(MessSummaryDto summary, string messName)
    {
        var month = new DateOnly(summary.Year, summary.Month, 1).ToString("MMMM yyyy");
        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(text => text.FontSize(9.5f));

            page.Header().Column(header =>
            {
                header.Item().Text($"{messName} — Monthly Settlement").FontSize(15).SemiBold();
                header.Item().Text(month).FontColor(Colors.Grey.Darken1);
                header.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });

            page.Content().PaddingVertical(10).Column(content =>
            {
                content.Item().PaddingBottom(10).Row(kpi =>
                {
                    kpi.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Meal rate").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(summary.MealRate.ToString("N4")).FontSize(13).SemiBold();
                    });
                    kpi.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Meal units").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(summary.TotalMealUnits.ToString("N1")).FontSize(13).SemiBold();
                    });
                    kpi.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Bazar").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(summary.TotalMealCosts.ToString("N2")).FontSize(13).SemiBold();
                    });
                    kpi.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Utility").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(summary.TotalUtilityCosts.ToString("N2")).FontSize(13).SemiBold();
                    });
                    kpi.RelativeItem().Column(c =>
                    {
                        c.Item().Text("Deposits").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(summary.TotalDeposits.ToString("N2")).FontSize(13).SemiBold();
                    });
                    kpi.RelativeItem().Column(c =>
                    {
                        c.Item().Text("On hand").FontSize(8).FontColor(Colors.Grey.Darken1);
                        c.Item().Text(summary.OnHandBalance.ToString("N2")).FontSize(13).SemiBold();
                    });
                });

                content.Item().Table(table =>
                {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.4f); // member
                            columns.RelativeColumn(1); // meals
                            columns.RelativeColumn(1.1f); // meal cost
                            columns.RelativeColumn(1.1f); // utility
                            columns.RelativeColumn(1.1f); // total
                            columns.RelativeColumn(1.1f); // deposited
                            columns.RelativeColumn(1.1f); // balance
                        });

                        static IContainer Cell(IContainer container, bool dark = false) => container
                            .BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(4).PaddingHorizontal(3)
                            .Background(dark ? Colors.Grey.Lighten4 : Colors.White);

                        table.Header(header =>
                        {
                            foreach (var title in new[] { "Member", "Meals", "Meal cost", "Utility", "Total cost", "Deposited", "Balance" })
                            {
                                header.Cell().Element(c => Cell(c, true)).Text(title).SemiBold().FontSize(8.5f);
                            }
                        });

                        foreach (var row in summary.Members)
                        {
                            table.Cell().Element(c => Cell(c)).Text(row.Name).SemiBold();
                            table.Cell().Element(c => Cell(c)).AlignRight().Text(row.MealUnits.ToString("N1"));
                            table.Cell().Element(c => Cell(c)).AlignRight().Text(row.MealCost.ToString("N2"));
                            table.Cell().Element(c => Cell(c)).AlignRight().Text(row.UtilityShare.ToString("N2"));
                            table.Cell().Element(c => Cell(c)).AlignRight().Text(row.TotalCost.ToString("N2"));
                            table.Cell().Element(c => Cell(c)).AlignRight().Text(row.Deposited.ToString("N2"));
                            table.Cell().Element(c => Cell(c)).AlignRight().Text(row.Balance.ToString("N2"))
                                .FontColor(row.Balance < 0 ? Colors.Red.Darken1 : Colors.Green.Darken1);
                        }

                        table.Footer(footer =>
                        {
                            footer.Cell().Element(c => Cell(c, true)).Text("Mess total").SemiBold();
                            footer.Cell().Element(c => Cell(c, true)).AlignRight()
                                .Text(summary.TotalMealUnits.ToString("N1")).SemiBold();
                            footer.Cell().Element(c => Cell(c, true)).AlignRight()
                                .Text(summary.TotalMealCosts.ToString("N2")).SemiBold();
                            footer.Cell().Element(c => Cell(c, true)).AlignRight()
                                .Text(summary.TotalUtilityCosts.ToString("N2")).SemiBold();
                            footer.Cell().Element(c => Cell(c, true)).AlignRight()
                                .Text(summary.TotalExpenditure.ToString("N2")).SemiBold();
                            footer.Cell().Element(c => Cell(c, true)).AlignRight()
                                .Text(summary.TotalDeposits.ToString("N2")).SemiBold();
                            footer.Cell().Element(c => Cell(c, true)).AlignRight()
                                .Text(summary.OnHandBalance.ToString("N2")).SemiBold();
                        });
                });

                content.Item().PaddingTop(8).Text(t =>
                {
                    t.Span("Meal rate = bazar ÷ units; utilities split equally per active member; balances are deposits − costs. ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span(summary.Locked ? "Month locked." : "Month open.").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        })).GeneratePdf();
    }
}
