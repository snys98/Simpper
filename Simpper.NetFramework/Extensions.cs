using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simpper.NetFramework
{
    public static class Extensions
    {
        public static bool IsSimpleType(this Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            type = underlyingType ?? type;
            var simpleTypes = new List<Type>
            {
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(bool),
                typeof(string),
                typeof(char),
                typeof(Guid),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(byte[])
            };
            return simpleTypes.Contains(type) || type.IsEnum;
        }

        public static List<PropertyInfo> GetIdProperties(this Type type)
        {
            var tp = type.GetProperties().Where(p =>
                p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(KeyAttribute).Name)).ToList();
            return tp.Any()
                ? tp
                : type.GetProperties().Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public static List<PropertyInfo> GetMutableProperties(this Type type)
        {
            IEnumerable<PropertyInfo> props = type.GetProperties();

            return props.Where(x => x.PropertyType.IsSimpleType())
                .Where(x => x.SetMethod != default(MemberInfo))
                .Where(x => x.GetCustomAttribute<IdentityAttribute>() == null)
                .Where(x => x.GetCustomAttribute<NotMappedAttribute>() == null).ToList();
        }

        public static List<PropertyInfo> GetMappedProperties(this Type type)
        {
            IEnumerable<PropertyInfo> props = type.GetProperties();

            return props.Where(x => x.PropertyType.IsSimpleType())
                .Where(x => x.SetMethod != default(MemberInfo))
                .Where(x => x.GetCustomAttribute<NotMappedAttribute>() == null).ToList();
        }
    }

    public static class PrimitiveExtensions
    {
        public static DateTime ToUnixEpoch(this DateTime @this)
        {
            //这里new了一个,不要在意这种细节=.=
            return new DateTime(621355968000000000L, DateTimeKind.Utc);
        }
    }
}
