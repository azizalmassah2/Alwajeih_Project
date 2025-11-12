// Global using directives لحل تعارضات الأسماء

// System
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;

// WPF
global using System.Windows;
global using System.Windows.Controls;
global using System.Windows.Data;
global using System.Windows.Documents;
global using System.Windows.Input;
global using System.Windows.Media;
global using System.Windows.Media.Imaging;
global using System.Windows.Navigation;
global using System.Windows.Shapes;

// تحديد الأسماء المستعارة لتجنب التعارضات
global using WinForms = System.Windows.Forms;
global using WpfApplication = System.Windows.Application;
global using WpfBrush = System.Windows.Media.Brush;
global using WpfButton = System.Windows.Controls.Button;
global using WpfUserControl = System.Windows.Controls.UserControl;
global using ThreadingTimer = System.Threading.Timer;
