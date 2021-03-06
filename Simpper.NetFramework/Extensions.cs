﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
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
                p.GetCustomAttributes(true).Any(attr => attr.GetType().Name == typeof(OrmKeyAttribute).Name)).ToList();
            return tp.Any()
                ? tp
                : type.GetProperties().Where(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public static List<PropertyInfo> GetMutableProperties(this Type type)
        {
            IEnumerable<PropertyInfo> props = type.GetProperties();

            return props.Where(x => x.PropertyType.IsSimpleType())
                .Where(x => x.SetMethod != default(MemberInfo))
                .Where(x => x.GetCustomAttribute<OrmIdentityAttribute>() == null)
                .Where(x => x.GetCustomAttribute<OrmNotMappedAttribute>() == null).ToList();
        }

        public static List<PropertyInfo> GetMappedProperties(this Type type)
        {
            IEnumerable<PropertyInfo> props = type.GetProperties();

            return props.Where(x => x.PropertyType.IsSimpleType())
                .Where(x => x.SetMethod != default(MemberInfo))
                .Where(x => x.GetCustomAttribute<OrmNotMappedAttribute>() == null).ToList();
        }

        public static object GetValue(this Expression expression)
        {
            return Expression.Lambda(expression).Compile().DynamicInvoke();
        }

        public static Expression Simplify(this Expression expression)
        {
            if (expression is UnaryExpression)
            {
                return (expression as UnaryExpression).Operand;
            }

            return expression;
        }

        public static bool In<T>(this T item, IEnumerable<T> list)
        {
            return list.Contains(item);
        }

        public static OrmContext ToOrmContext(this SqlConnection @this, Func<string, string> shardingIndexSelector = null)
        {
            return new OrmContext(@this, shardingIndexSelector);
        }
    }

    public static class ExpressionOperateExpressions
    {
        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> @this,
            Expression<Func<T, bool>> and)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(@this.Body, and.Body), @this.Parameters[0]);
        }

        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> @this,
            Expression<Func<T, bool>> @else)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(@this.Body, @else.Body), @this.Parameters[0]);
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
