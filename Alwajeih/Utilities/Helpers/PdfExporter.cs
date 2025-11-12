using System.Collections.Generic;
using System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfColors = QuestPDF.Helpers.Colors;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مُصدّر إلى PDF
    /// </summary>
    public static class PdfExporter
    {
        static PdfExporter()
        {
            // تعيين نوع الترخيص
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// تصدير جدول بيانات إلى PDF
        /// </summary>
        public static void ExportToPdf(DataTable data, string filePath, string title = "تقرير")
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(PdfColors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    // الرأس
                    page.Header()
                        .Text(title)
                        .SemiBold().FontSize(20).FontColor(PdfColors.Blue.Medium);

                    // المحتوى
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            // تعريف الأعمدة
                            table.ColumnsDefinition(columns =>
                            {
                                for (int i = 0; i < data.Columns.Count; i++)
                                {
                                    columns.RelativeColumn();
                                }
                            });

                            // العناوين
                            table.Header(header =>
                            {
                                foreach (DataColumn column in data.Columns)
                                {
                                    header.Cell().Element(CellStyle).Text(column.ColumnName).SemiBold();
                                }
                            });

                            // البيانات
                            foreach (DataRow row in data.Rows)
                            {
                                foreach (var cell in row.ItemArray)
                                {
                                    table.Cell().Element(CellStyle).Text(cell?.ToString() ?? "");
                                }
                            }
                        });

                    // التذييل
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("صفحة ");
                            x.CurrentPageNumber();
                            x.Span(" من ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(filePath);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(PdfColors.Grey.Lighten2).Padding(5);
        }
    }
}
