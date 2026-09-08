using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MVVM_Bonus.Utilities
{
    /// <summary>
    /// Collapses an element while its bound value is null, empty or whitespace.
    /// </summary>
    /// <remarks>
    /// Used for status and error lines that should take up no room until there is
    /// something to say. Set ConverterParameter to "Invert" to show on empty instead.
    /// </remarks>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasContent = value is string text
                ? !string.IsNullOrWhiteSpace(text)
                : value != null;

            bool invert = string.Equals(
                parameter as string,
                "Invert",
                StringComparison.OrdinalIgnoreCase);

            if (invert)
                hasContent = !hasContent;

            return hasContent ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(
                $"{nameof(NullToVisibilityConverter)} is one-way only.");
        }
    }
}
