using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkyLineSQL.Utility
{
    public class KeyToCharBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            AssociatedObject.KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            char? typedChar = null;
            char? typedDigit = null;

            if (e.Key >= Key.A && e.Key <= Key.Z)
                typedChar = (char)('a' + (e.Key - Key.A));
            else if (e.Key >= Key.D0 && e.Key <= Key.D9)
                typedDigit = (char)('0' + (e.Key - Key.D0));
            else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                typedDigit = (char)('0' + (e.Key - Key.NumPad0));

            if (typedDigit.HasValue && Command != null)
            {
                e.Handled = true;
                Command.Execute(typedDigit.Value);
            }
            else if (typedChar.HasValue && Command != null && AssociatedObject.Text.Length >= 3)
            {
                Command.Execute(typedChar.Value);
            }


        }

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(KeyToCharBehavior));
    }
}
