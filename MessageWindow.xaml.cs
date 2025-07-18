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
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindow(string title, string msg)
        {
            InitializeComponent();

            this.txt_title.Text = title;
            this.txt_msg.Text = msg;
            this.btn_ok.Focus();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
