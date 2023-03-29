using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WatchDogWpf.ValueConverter;

public class EnumValueConverter : IValueConverter
{
    private IValueConverter _valueConverterImplementation;

    public static string GetDescription(Enum value)
    {
        // 将枚举值转换为对应的描述
        var field = value.GetType().GetField(value.ToString());
        var attribute = field.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
        return attribute is DescriptionAttribute descriptionAttribute ? descriptionAttribute.Description : value.ToString();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 将枚举值转换为对应的描述
        return GetDescription((Enum)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // 不支持双向转换
        throw new NotSupportedException();
    }
}