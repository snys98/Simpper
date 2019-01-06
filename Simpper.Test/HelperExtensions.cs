using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Simpper.Test
{
    public static class HelperExtensions
    {
        public static string GetReflectedColumnName(this PropertyInfo propertyInfo)
        {
            var attr = propertyInfo.GetCustomAttribute<ColumnAttribute>();
            var columnName = attr == null ? propertyInfo.Name : attr.Name;
            return columnName;
        }
    }
}
