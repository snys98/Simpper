using System.Reflection;

namespace Simpper.NetFramework.Test
{
    public static class HelperExtensions
    {
        public static string GetReflectedColumnName(this PropertyInfo propertyInfo)
        {
            var attr = propertyInfo.GetCustomAttribute<OrmColumnAttribute>();
            var columnName = attr == null ? propertyInfo.Name : attr.Name;
            return columnName;
        }
    }
}
