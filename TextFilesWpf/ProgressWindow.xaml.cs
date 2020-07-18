using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TextFilesWpf
{
    /// <summary>
    /// Логика взаимодействия для ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(int totalLinesCount)
        {
            InitializeComponent();
            total.Text = $"{totalLinesCount} строк всего";
            passed.Text = "0 строк экспортировано";
        }

        public void Pass(int passedLinesCount)
        {
            passed.Text = $"{passedLinesCount} строк экспортировано";
        }
    }
}
