using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace SkyLineSQL.Utility
{
    internal class HighlightTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string text = values[0]?.ToString() ?? "";
            string search = values[1]?.ToString() ?? "";
            string color = values[2]?.ToString() ?? "";

            var textBlock = new TextBlock();

            if (!string.IsNullOrEmpty(search) && !string.IsNullOrEmpty(text))
            {
                int index = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    // Before match
                    textBlock.Inlines.Add(new Run(text.Substring(0, index)));

                    // Matched text (highlighted)
                    textBlock.Inlines.Add(new Run(text.Substring(index, search.Length))
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                        Foreground = Brushes.Black,
                        FontWeight = FontWeights.Bold,
                    });

                    // After match
                    textBlock.Inlines.Add(new Run(text.Substring(index + search.Length)));
                }
                else
                {
                    textBlock.Inlines.Add(text);
                }
            }
            else
            {
                textBlock.Inlines.Add(text);
            }

            return textBlock;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
    }
}
