using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Alwajeih.Utilities.Helpers
{
    /// <summary>
    /// مساعد الطباعة
    /// </summary>
    public static class PrintHelper
    {
        /// <summary>
        /// طباعة عنصر UI
        /// </summary>
        public static void PrintElement(UIElement element, string documentTitle = "مستند")
        {
            PrintDialog printDialog = new PrintDialog();
            
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintVisual(element, documentTitle);
            }
        }

        /// <summary>
        /// طباعة نص بسيط
        /// </summary>
        public static void PrintText(string text, string documentTitle = "مستند")
        {
            PrintDialog printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                FlowDocument doc = new FlowDocument(new Paragraph(new Run(text)));
                doc.Name = "FlowDoc";
                doc.PageHeight = printDialog.PrintableAreaHeight;
                doc.PageWidth = printDialog.PrintableAreaWidth;
                doc.PagePadding = new Thickness(50);
                doc.ColumnGap = 0;
                doc.ColumnWidth = printDialog.PrintableAreaWidth;

                IDocumentPaginatorSource idpSource = doc;
                printDialog.PrintDocument(idpSource.DocumentPaginator, documentTitle);
            }
        }
    }
}
