using System;
using System.Data;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ExcelColor = System.Drawing.Color;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مُصدّر Excel محسّن مع تنسيق احترافي
    /// </summary>
    public static class EnhancedExcelExporter
    {
        /// <summary>
        /// تصدير جدول بيانات إلى Excel مع تنسيق شامل
        /// </summary>
        public static void ExportToExcel(DataTable data, string filePath, string title = "تقرير")
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(title);

            // تعيين اتجاه الورقة من اليمين لليسار
            worksheet.View.RightToLeft = true;

            int currentRow = 1;

            // إضافة شعار أو عنوان رئيسي
            worksheet.Cells[currentRow, 1, currentRow, data.Columns.Count + 1].Merge = true;
            var titleCell = worksheet.Cells[currentRow, 1];
            titleCell.Value = "نظام الوجيه لإدارة الجمعيات";
            titleCell.Style.Font.Name = "Tajawal";
            titleCell.Style.Font.Size = 16;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.Color.SetColor(ExcelColor.White);
            titleCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            titleCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#1E40AF"));
            titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            titleCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Row(currentRow).Height = 30;
            currentRow++;

            // عنوان التقرير
            worksheet.Cells[currentRow, 1, currentRow, data.Columns.Count + 1].Merge = true;
            var reportTitleCell = worksheet.Cells[currentRow, 1];
            reportTitleCell.Value = title.Replace("📊", "").Replace("📅", "").Replace("🏦", "").Replace("⚠️", "").Replace("💰", "").Replace("👤", "").Replace("👥", "").Replace("💵", "").Replace("📋", "").Replace("💳", "").Replace("📈", "").Replace("🏆", "").Trim();
            reportTitleCell.Style.Font.Name = "Tajawal";
            reportTitleCell.Style.Font.Size = 14;
            reportTitleCell.Style.Font.Bold = true;
            reportTitleCell.Style.Font.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#1E40AF"));
            reportTitleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(currentRow).Height = 25;
            currentRow++;

            // تاريخ الطباعة
            worksheet.Cells[currentRow, 1, currentRow, data.Columns.Count + 1].Merge = true;
            var dateCell = worksheet.Cells[currentRow, 1];
            dateCell.Value = $"تاريخ الطباعة: {DateTime.Now:yyyy-MM-dd HH:mm}";
            dateCell.Style.Font.Name = "Tajawal";
            dateCell.Style.Font.Size = 10;
            dateCell.Style.Font.Color.SetColor(ExcelColor.Gray);
            dateCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            currentRow += 2; // مسافة

            // رأس الجدول مع عمود الترقيم
            int headerRow = currentRow;
            
            // عمود الترقيم
            var numberCell = worksheet.Cells[headerRow, 1];
            numberCell.Value = "#";
            numberCell.Style.Font.Name = "Tajawal";
            numberCell.Style.Font.Bold = true;
            numberCell.Style.Font.Size = 12;
            numberCell.Style.Font.Color.SetColor(ExcelColor.White);
            numberCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            numberCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#2563EB"));
            numberCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            numberCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            numberCell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ExcelColor.White);
            
            // بقية الأعمدة
            for (int col = 0; col < data.Columns.Count; col++)
            {
                var cell = worksheet.Cells[headerRow, col + 2];
                cell.Value = data.Columns[col].ColumnName;
                cell.Style.Font.Name = "Tajawal";
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 12;
                cell.Style.Font.Color.SetColor(ExcelColor.White);
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#2563EB"));
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, ExcelColor.White);
            }
            worksheet.Row(headerRow).Height = 25;
            worksheet.Column(1).Width = 8;
            currentRow++;

            // صفوف البيانات
            int dataStartRow = currentRow;
            int rowNumber = 1;
            for (int row = 0; row < data.Rows.Count; row++)
            {
                bool isTotal = false;
                
                // عمود الترقيم
                var rowNumberCell = worksheet.Cells[currentRow, 1];
                var firstValue = data.Rows[row][0]?.ToString() ?? "";
                if (firstValue.Contains("إجمالي") || firstValue.Contains("📊"))
                {
                    rowNumberCell.Value = "";
                    isTotal = true;
                }
                else
                {
                    rowNumberCell.Value = rowNumber++;
                }
                rowNumberCell.Style.Font.Name = "Tajawal";
                rowNumberCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                rowNumberCell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                rowNumberCell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                rowNumberCell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                rowNumberCell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                rowNumberCell.Style.Border.Top.Color.SetColor(ExcelColor.LightGray);
                rowNumberCell.Style.Border.Bottom.Color.SetColor(ExcelColor.LightGray);
                rowNumberCell.Style.Border.Left.Color.SetColor(ExcelColor.LightGray);
                rowNumberCell.Style.Border.Right.Color.SetColor(ExcelColor.LightGray);
                
                // بقية الأعمدة
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    var cell = worksheet.Cells[currentRow, col + 2];
                    var value = data.Rows[row][col];
                    
                    // إزالة الإيموجي من القيم
                    if (value is string strValue)
                    {
                        strValue = strValue.Replace("📊", "").Replace("✅", "").Replace("❌", "").Replace("🔄", "").Trim();
                        cell.Value = strValue;
                    }
                    else
                    {
                        cell.Value = value;
                    }

                    cell.Style.Font.Name = "Tajawal";
                    
                    // تنسيق الأرقام
                    if (value is decimal || value is int || value is double)
                    {
                        cell.Style.Numberformat.Format = "#,##0.00";
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }
                    else
                    {
                        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    }

                    // تنسيق الحدود
                    cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    cell.Style.Border.Top.Color.SetColor(ExcelColor.LightGray);
                    cell.Style.Border.Bottom.Color.SetColor(ExcelColor.LightGray);
                    cell.Style.Border.Left.Color.SetColor(ExcelColor.LightGray);
                    cell.Style.Border.Right.Color.SetColor(ExcelColor.LightGray);
                }

                // تنسيق خاص لصفوف الإجماليات
                if (isTotal)
                {
                    for (int col = 0; col < data.Columns.Count + 1; col++)
                    {
                        var cell = worksheet.Cells[currentRow, col + 1];
                        cell.Style.Font.Bold = true;
                        cell.Style.Font.Color.SetColor(System.Drawing.ColorTranslator.FromHtml("#1E40AF"));
                        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#DBEAFE"));
                    }
                }
                else
                {
                    // تلوين الصفوف المتناوبة
                    if ((row - dataStartRow) % 2 == 0)
                    {
                        for (int col = 0; col < data.Columns.Count + 1; col++)
                        {
                            worksheet.Cells[currentRow, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                            worksheet.Cells[currentRow, col + 1].Style.Fill.BackgroundColor.SetColor(ExcelColor.FromArgb(249, 250, 251));
                        }
                    }
                }

                worksheet.Row(currentRow).Height = 20;
                currentRow++;
            }

            // تعديل عرض الأعمدة تلقائياً
            for (int col = 1; col <= data.Columns.Count + 1; col++)
            {
                worksheet.Column(col).AutoFit();
                if (col == 1)
                {
                    if (worksheet.Column(col).Width < 8)
                        worksheet.Column(col).Width = 8;
                }
                else
                {
                    if (worksheet.Column(col).Width < 15)
                        worksheet.Column(col).Width = 15;
                    if (worksheet.Column(col).Width > 50)
                        worksheet.Column(col).Width = 50;
                }
            }

            // إضافة تجميد للرأس
            worksheet.View.FreezePanes(headerRow + 1, 1);

            // حفظ الملف
            var file = new FileInfo(filePath);
            package.SaveAs(file);
        }
    }
}
