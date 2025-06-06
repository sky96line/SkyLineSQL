using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SkyLineSQL
{
    /// <summary>
    /// Interaction logic for ProfilerWindow.xaml
    /// </summary>
    public partial class ProfilerWindow : Window
    {
        ProfilerWindowViewModel VM;
        public ProfilerWindow()
        {
            InitializeComponent();

            VM = new();
            this.DataContext = VM;
        }

        public void SetDataManager(DataManager DM)
        {
            this.Title = $"{DM.CurrentConnection} - SQL Profiler";
            VM.SetDataManager(DM);
        }
    }
}
