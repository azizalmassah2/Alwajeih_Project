using System;
using System.Data;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfColors = QuestPDF.Helpers.Colors;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مُصدّر PDF محسّن مع دعم RTL والخط العربي
    /// </summary>
    public static class EnhancedPdfExporter
    {
        static EnhancedPdfExporter()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        /// <summary>
        /// تصدير جدول بيانات إلى PDF مع تنسيق احترافي
        /// </summary>
        public static void ExportToPdf(DataTable data, string filePath, string title = "تقرير")
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(PdfColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial").Fallback());
                    page.ContentFromRightToLeft(); // RTL Support

                    // الرأس مع الشعار
                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Column(headerColumn =>
                        {
                            // الشعار (إذا كان متوفراً)
                            var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");
                            if (File.Exists(logoPath))
                            {
                                headerColumn.Item().Height(60).AlignCenter().Image(logoPath);
                            }
                            else
                            {
                                // نص بديل للشعار
                                headerColumn.Item().PaddingVertical(10).Text("نظام الوجيه لإدارة الجمعيات")
                                    .FontSize(16).Bold().FontColor(PdfColors.Blue.Darken2);
                            }

                            // العنوان - إزالة الإيموجي
                            var cleanTitle = title.Replace("📊", "").Replace("📅", "").Replace("🏦", "").Replace("⚠️", "").Replace("💰", "").Replace("👤", "").Replace("👥", "").Replace("💵", "").Replace("📋", "").Replace("💳", "").Replace("📈", "").Replace("🏆", "").Trim();
                            headerColumn.Item().PaddingTop(10).Text(cleanTitle)
                                .FontSize(18).Bold().FontColor(PdfColors.Blue.Darken3);

                            // التاريخ
                            headerColumn.Item().PaddingTop(5).Text($"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(10).FontColor(PdfColors.Grey.Darken1);

                            // خط فاصل
                            headerColumn.Item().PaddingTop(10).LineHorizontal(1)
                                .LineColor(PdfColors.Blue.Lighten3);
                        });
                    });

                    // المحتوى
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        // تعريف الأعمدة (مع عمود الترقيم)
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30); // عمود الترقيم
                            for (int i = 0; i < data.Columns.Count; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        // رأس الجدول
                        table.Header(header =>
                        {
                            // عمود الترقيم
                            header.Cell().Element(CellStyle).Background(PdfColors.Blue.Darken2)
                                .Padding(8).AlignCenter().Text("#")
                                .FontSize(12).Bold().FontColor(PdfColors.White);
                            
                            // بقية الأعمدة
                            foreach (DataColumn column in data.Columns)
                            {
                                header.Cell().Element(CellStyle).Background(PdfColors.Blue.Darken2)
                                    .Padding(8).AlignCenter().Text(column.ColumnName)
                                    .FontSize(12).Bold().FontColor(PdfColors.White);
                            }
                        });

                        // صفوف البيانات
                        int rowIndex = 0;
                        int rowNumber = 1;
                        foreach (DataRow row in data.Rows)
                        {
                            var bgColor = rowIndex % 2 == 0 ? PdfColors.White : PdfColors.Grey.Lighten5;
                            var firstValue = row.ItemArray[0]?.ToString() ?? "";
                            var isTotal = firstValue.Contains("إجمالي") || firstValue.Contains("📊");
                            
                            // عمود الترقيم
                            table.Cell().Element(CellStyle).Background(bgColor)
                                .Padding(6).AlignCenter()
                                .Text(text =>
                                {
                                    if (!isTotal)
                                    {
                                        text.Span(rowNumber.ToString()).FontSize(10).FontColor(PdfColors.Black);
                                    }
                                });
                            
                            if (!isTotal) rowNumber++;
                            
                            // بقية الأعمدة
                            foreach (var item in row.ItemArray)
                            {
                                var cellValue = item?.ToString() ?? "-";
                                
                                // إزالة الإيموجي
                                cellValue = cellValue.Replace("📊", "").Replace("✅", "").Replace("❌", "").Replace("🔄", "").Trim();
                                
                                var isNumeric = decimal.TryParse(cellValue, out var numValue);

                                table.Cell().Element(CellStyle).Background(bgColor)
                                    .Padding(6).AlignRight()
                                    .Text(text =>
                                    {
                                        if (isTotal)
                                        {
                                            text.Span(cellValue).FontSize(11).Bold().FontColor(PdfColors.Blue.Darken2);
                                        }
                                        else
                                        {
                                            text.Span(cellValue).FontSize(10).FontColor(PdfColors.Black);
                                        }
                                    });
                            }
                            rowIndex++;
                        }
                    });

                    // الذيل
                    page.Footer().AlignCenter().Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(PdfColors.Grey.Lighten2);
                        footer.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().AlignLeft().Text(text =>
                            {
                                text.Span("صفحة ").FontSize(9).FontColor(PdfColors.Grey.Darken1);
                                text.CurrentPageNumber().FontSize(9).FontColor(PdfColors.Grey.Darken1);
                                text.Span(" من ").FontSize(9).FontColor(PdfColors.Grey.Darken1);
                                text.TotalPages().FontSize(9).FontColor(PdfColors.Grey.Darken1);
                            });

                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("نظام الوجيه لإدارة الجمعيات").FontSize(9).FontColor(PdfColors.Grey.Darken1);
                            });
                        });
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.Border(0.5f).BorderColor(PdfColors.Grey.Lighten2);
        }
    }
}
