using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Infrastructure.Utils;
public static class EnumExtensions
{


    // This extension method is broken out so you can use a similar pattern with 
    // other MetaData elements in the future. This is your base method for each.
    public static T GetAttribute<T>(this Enum value) where T : Attribute
    {
        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString());
        var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
        return (T)attributes[0];
    }



    public static string ToDisplayName(this Enum value)
    {
        try
        {
            var attribute = value.GetAttribute<DisplayAttribute>();
            return attribute == null ? value.ToString() : attribute.Name;
        }
        catch
        {
            return value.ToString();
        }
    }
    public static string ToGroupName(this Enum value)
    {
        try
        {

            var attribute = value.GetAttribute<DisplayAttribute>();
            return attribute == null ? value.ToString() : attribute.GroupName;
        }
        catch
        {
            return value.ToString();
        }
    }
    public static string ToShortName(this Enum value)
    {
        try
        {
            var attribute = value.GetAttribute<DisplayAttribute>();
            return attribute == null ? value.ToString() : attribute.ShortName;
        }
        catch
        {
            return value.ToString();
        }
    }
    public static int ToOrder(this Enum value)
    {
        try
        {
            var attribute = value.GetAttribute<DisplayAttribute>();
            return attribute == null ? 0 : attribute.Order;
        }
        catch
        {

        }
        return 0;

    }
    public static IEnumerable<TEnum> EnumGetOrderedValues<TEnum>(this Type enumType)
    {

        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        var orderedValues = new List<Tuple<int, TEnum>>();
        foreach (var field in fields)
        {
            var orderAtt = field.GetCustomAttributes(typeof(DisplayAttribute), false).SingleOrDefault() as DisplayAttribute;
            if (orderAtt != null)
            {
                orderedValues.Add(new Tuple<int, TEnum>(orderAtt.Order, (TEnum)field.GetValue(null)));
            }
        }

        return orderedValues.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();
    }
}
