using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Alwajeih.Utilities
{
    /// <summary>
    /// مولد الأيقونات - إنشاء أيقونة التطبيق
    /// </summary>
    public static class IconGenerator
    {
        /// <summary>
        /// إنشاء أيقونة الوجيه بأحجام متعددة
        /// </summary>
        public static Icon CreateAlwajeihIcon(int size = 256)
        {
            using (Bitmap bitmap = new Bitmap(size, size))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                // خلفية دائرية بلون أزرق
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(59, 130, 246))) // #3B82F6
                {
                    g.FillEllipse(brush, 4, 4, size - 8, size - 8);
                }

                // ظل خفيف للدائرة
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(100, 37, 99, 235), 2))
                {
                    g.DrawEllipse(pen, 4, 4, size - 8, size - 8);
                }

                // رسم حرف "و" بالعربي
                float fontSize = size * 0.5f; // 50% من حجم الأيقونة
                using (var font = new System.Drawing.Font("Tajawal", fontSize, System.Drawing.FontStyle.Bold))
                using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                {
                    var text = "و";
                    var textSize = g.MeasureString(text, font);
                    var x = (size - textSize.Width) / 2;
                    var y = (size - textSize.Height) / 2;
                    g.DrawString(text, font, textBrush, x, y);
                }

                // تحويل Bitmap إلى Icon
                IntPtr hIcon = bitmap.GetHicon();
                Icon icon = Icon.FromHandle(hIcon);
                return icon;
            }
        }

        /// <summary>
        /// حفظ الأيقونة كملف .ico
        /// </summary>
        public static void SaveIconToFile(string filePath, params int[] sizes)
        {
            if (sizes == null || sizes.Length == 0)
            {
                sizes = new int[] { 16, 32, 48, 64, 128, 256 };
            }

            using (var ms = new MemoryStream())
            {
                // ICONDIR header
                ms.WriteByte(0); ms.WriteByte(0); // Reserved
                ms.WriteByte(1); ms.WriteByte(0); // Type (1 = ICO)
                ms.WriteByte((byte)sizes.Length); ms.WriteByte(0); // Count

                var imageStreams = new MemoryStream[sizes.Length];
                int offset = 6 + (16 * sizes.Length); // Header + Directory entries

                // كتابة Directory entries وإنشاء الصور
                for (int i = 0; i < sizes.Length; i++)
                {
                    int size = sizes[i];
                    imageStreams[i] = new MemoryStream();

                    using (var bitmap = CreateIconBitmap(size))
                    {
                        bitmap.Save(imageStreams[i], ImageFormat.Png);
                    }

                    imageStreams[i].Position = 0;
                    int imageSize = (int)imageStreams[i].Length;

                    // ICONDIRENTRY
                    ms.WriteByte((byte)(size == 256 ? 0 : size)); // Width
                    ms.WriteByte((byte)(size == 256 ? 0 : size)); // Height
                    ms.WriteByte(0); // Color palette
                    ms.WriteByte(0); // Reserved
                    ms.WriteByte(1); ms.WriteByte(0); // Color planes
                    ms.WriteByte(32); ms.WriteByte(0); // Bits per pixel
                    
                    // Image size
                    ms.Write(BitConverter.GetBytes(imageSize), 0, 4);
                    // Image offset
                    ms.Write(BitConverter.GetBytes(offset), 0, 4);

                    offset += imageSize;
                }

                // كتابة بيانات الصور
                foreach (var imageStream in imageStreams)
                {
                    imageStream.CopyTo(ms);
                    imageStream.Dispose();
                }

                // حفظ الملف
                File.WriteAllBytes(filePath, ms.ToArray());
            }
        }

        /// <summary>
        /// إنشاء Bitmap للأيقونة
        /// </summary>
        private static Bitmap CreateIconBitmap(int size)
        {
            var bitmap = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                // خلفية دائرية
                int margin = Math.Max(2, size / 16);
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(59, 130, 246)))
                {
                    g.FillEllipse(brush, margin, margin, size - (margin * 2), size - (margin * 2));
                }

                // رسم حرف "و"
                float fontSize = size * 0.5f;
                using (var font = new System.Drawing.Font("Tajawal", fontSize, System.Drawing.FontStyle.Bold))
                using (var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                {
                    var text = "و";
                    var textSize = g.MeasureString(text, font);
                    var x = (size - textSize.Width) / 2;
                    var y = (size - textSize.Height) / 2;
                    g.DrawString(text, font, textBrush, x, y);
                }
            }
            return bitmap;
        }

        /// <summary>
        /// إنشاء ملف الأيقونة في مجلد Resources
        /// </summary>
        public static string GenerateAppIcon()
        {
            try
            {
                string resourcesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                if (!Directory.Exists(resourcesPath))
                {
                    Directory.CreateDirectory(resourcesPath);
                }

                string iconPath = System.IO.Path.Combine(resourcesPath, "app-icon.ico");
                SaveIconToFile(iconPath, 16, 32, 48, 64, 128, 256);
                
                return iconPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ خطأ في إنشاء الأيقونة: {ex.Message}");
                return null;
            }
        }
    }
}
