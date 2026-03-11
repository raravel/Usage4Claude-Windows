using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Usage4Claude.Helpers;

/// <summary>
/// Converts between an enum value and a boolean, enabling RadioButton binding to enum properties.
/// The ConverterParameter specifies the enum member name to match against.
/// Returns true when the bound value matches the parameter; converts back by parsing the parameter.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter != null)
            return Enum.Parse(targetType, parameter.ToString()!);
        return Binding.DoNothing;
    }
}
