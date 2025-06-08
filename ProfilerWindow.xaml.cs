using System.Windows;

namespace SkyLineSQL
{
    /// <summary>
    /// Interaction logic for ProfilerWindow.xaml
    /// </summary>
    public partial class ProfilerWindow : Window
    {
        public ProfilerWindow(string Title, ProfilerWindowViewModel VM)
        {
            InitializeComponent();

            this.Title = Title;
            this.DataContext = VM;
        }
    }
}
