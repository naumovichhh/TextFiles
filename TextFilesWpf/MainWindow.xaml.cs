using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TextFilesWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void buttonGenerate_ClickAsync(object sender, RoutedEventArgs e)
        {
            SetButtonsEnabled(false);
            try
            {
                var waitWindow = new WaitWindow();
                try
                {
                    waitWindow.Show();
                    await Task.Run(() => TextFilesHandler.Generate100Files());
                }
                finally
                {
                    waitWindow.Close();
                }

                MessageBox.Show("Выполнено");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            SetButtonsEnabled(true);
        }

        private async void buttonUnite_ClickAsync(object sender, RoutedEventArgs e)
        {
            SetButtonsEnabled(false);
            var inputWindow = new InputWindow("Введите пропускаемую подстроку");
            var result = inputWindow.ShowDialog();
            if (result == true)
            {
                var substr = inputWindow.Text;
                try
                {
                    int numOfSkippedLines;
                    var waitWindow = new WaitWindow();
                    try
                    {
                        waitWindow.Show();
                        numOfSkippedLines = await Task.Run(() => TextFilesHandler.UniteFiles(substr));
                    }
                    finally
                    {
                        waitWindow.Close();
                    }

                    MessageBox.Show($"Выполнено, {numOfSkippedLines} строк пропущено");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            SetButtonsEnabled(true);
        }

        private async void buttonExport_ClickAsync(object sender, RoutedEventArgs e)
        {
            SetButtonsEnabled(false);
            var inputWindow = new InputWindow("Введите имя файла");
            var result = inputWindow.ShowDialog();
            if (result == true)
            {
                var fileName = inputWindow.Text;
                try
                {
                    var progressWindow = new ProgressWindow(TextFilesHandler.GetLinesCount(fileName));
                    try
                    {
                        progressWindow.Show();
                        var currentSyncContext = SynchronizationContext.Current;
                        await Task.Run(() => TextFilesHandler.ExportFilesToDB(fileName, i => currentSyncContext.Send(o => progressWindow.Pass((int)o), i)));
                    }
                    finally
                    {
                        progressWindow.Close();
                    }
                    

                    MessageBox.Show("Выполнено");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            SetButtonsEnabled(true);
        }

        private void SetButtonsEnabled(bool flag)
        {
            buttonGenerate.IsEnabled = flag;
            buttonUnite.IsEnabled = flag;
            buttonExport.IsEnabled = flag;
        }
    }
}
