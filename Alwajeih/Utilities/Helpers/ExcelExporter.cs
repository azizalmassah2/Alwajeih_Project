using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مُصدّر إلى Excel
    /// </summary>
    public static class ExcelExporter
    {
        static ExcelExporter()
        {
            // تعيين سياق الترخيص لـ EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// تصدير جدول بيانات إلى ملف Excel
        /// </summary>
        public static void ExportToExcel(DataTable data, string filePath, string sheetName = "البيانات")
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            // إضافة العناوين
            for (int i = 0; i < data.Columns.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = data.Columns[i].ColumnName;
            }

            // تنسيق العناوين
            using (var range = worksheet.Cells[1, 1, 1, data.Columns.Count])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // إضافة البيانات
            for (int i = 0; i < data.Rows.Count; i++)
            {
                for (int j = 0; j < data.Columns.Count; j++)
                {
                    worksheet.Cells[i + 2, j + 1].Value = data.Rows[i][j];
                }
            }

            // ضبط عرض الأعمدة تلقائياً
            worksheet.Cells.AutoFitColumns();

            // حفظ الملف
            var file = new FileInfo(filePath);
            package.SaveAs(file);
        }

        /// <summary>
        /// تصدير قائمة إلى Excel
        /// </summary>
        public static void ExportToExcel<T>(List<T> data, string filePath, string sheetName = "البيانات")
        {
            var dataTable = ConvertListToDataTable(data);
            ExportToExcel(dataTable, filePath, sheetName);
        }

        private static DataTable ConvertListToDataTable<T>(List<T> data)
        {
            var dataTable = new DataTable();
            var properties = typeof(T).GetProperties();

            // إضافة الأعمدة
            foreach (var prop in properties)
            {
                dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }

            // إضافة الصفوف
            foreach (var item in data)
            {
                var row = dataTable.NewRow();
                foreach (var prop in properties)
                {
                    row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}
