using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Simpper
{
    public class SqlServerSqlGenerator<T>
    {
        public override string ToString()
        {
            return SqlBuilder.ToString();
        }

        public IDictionary<string, object> SqlParams { get; } = new ExpandoObject() as IDictionary<string, object>;

        public StringBuilder SqlBuilder { get; private set; }

        private OperationType _operationType;

        private int DuplicateParamIdentifier;
        public static string ShardingIndex { get; set; }

        public SqlServerSqlGenerator()
        {
            SqlBuilder = new StringBuilder();
            EntityConfigurations.TryAdd(typeof(T), new EntityMappingCache<T>());
        }

        public SqlServerSqlGenerator<T> Count()
        {
            _operationType = OperationType.Select;
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var selectPiece = $"SELECT COUNT(1) FROM {tableName}";
            SqlBuilder.AppendLine(selectPiece);
            return this;
        }

        public SqlServerSqlGenerator<T> Where(Expression<Func<T, bool>> predicate, ContactorType contactorType = ContactorType.None)
        {
            if (!(predicate is LambdaExpression expression))
                throw new LinqToSqlException(predicate);
            var wherePiece = "WHERE 1 = 1";
            SqlBuilder.AppendLine(wherePiece);
            WhereSubclause(expression, contactorType);

            return this;
        }

        private SqlServerSqlGenerator<T> WhereSubclause(Expression expression,
            ContactorType contactorType = ContactorType.None)
        {
            switch (contactorType)
            {
                case ContactorType.And:
                    SqlBuilder.Append("AND ");
                    break;
                case ContactorType.Or:
                    SqlBuilder.Append("OR ");
                    break;
                case ContactorType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contactorType), contactorType, null);
            }

            if (expression is LambdaExpression lambdaExpression)
            {
                WhereSubclause(lambdaExpression.Body);
            }
            else
            {
                //子句不单纯, 需要继续拆分
                if (expression.NodeType == ExpressionType.AndAlso || expression.NodeType == ExpressionType.OrElse)
                {
                    var exactExpression = (BinaryExpression) expression;
                    SqlBuilder.Append("AND ");
                    return expression.NodeType == ExpressionType.AndAlso ? And(exactExpression) : Or(exactExpression);
                }

                if (expression.NodeType == ExpressionType.Constant)
                    return this.Append(" 1 = 1 ");

                switch (expression.NodeType)
                {
                    case ExpressionType.Call:
                    {
                        var methodCallExpression = (MethodCallExpression) expression;
                        WhereLikeSubclause(methodCallExpression);
                        break;
                    }
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    {
                        var binaryExpression = (BinaryExpression) expression;
                        WhereOperatorSubclause(binaryExpression);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return this;
        }

        private SqlServerSqlGenerator<T> WhereLikeSubclause(MethodCallExpression methodCallExpression)
        {
            if (!((methodCallExpression.Arguments[0] as ConstantExpression)?.Value is string))
                return this;
            if (methodCallExpression.Method.Name == "Contains")
                SqlBuilder.AppendLine(BuildLikeSubclause(methodCallExpression));
            if (methodCallExpression.Method.Name == "StartsWith")
                SqlBuilder.AppendLine(BuildLikeSubclause(methodCallExpression, LikeClauseMatchType.StartsWith));
            if (methodCallExpression.Method.Name == "EndsWith")
                SqlBuilder.AppendLine(BuildLikeSubclause(methodCallExpression, LikeClauseMatchType.EndWith));
            return this;
        }

        public SqlServerSqlGenerator<T> Delete(Expression<Func<T, bool>> predicate)
        {
            _operationType = OperationType.Delete;
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var deletePiece = $"DELETE FROM {tableName}";
            SqlBuilder.AppendLine(deletePiece);
            Where(predicate, ContactorType.And);
            SqlBuilder.AppendLine(" SELECT @@ROWCOUNT");
            return this;
        }

        public static Dictionary<Type, EntityMappingCache> EntityConfigurations { get; } =
            new Dictionary<Type, EntityMappingCache>();


        public SqlServerSqlGenerator<T> Insert(T entity)
        {
            _operationType = OperationType.Insert;
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var names = EntityConfigurations[typeof(T)].MutableProperties.Select(x => x.Name);
            var insertPiece = $"INSERT INTO {tableName} {$"({string.Join(',', names)})"}";
            SqlBuilder.AppendLine(insertPiece);
            Values(entity);
            return this;
        }


        public SqlServerSqlGenerator<T> Select(int? top = null)
        {
            _operationType = OperationType.Select;
            var names = EntityConfigurations[typeof(T)].MappedProperties.Select(x => x.Name);
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var selectPiece =
                $"SELECT {(top.HasValue ? "TOP(" + top + ")" : string.Empty)} {string.Join(',', names)} FROM {tableName}";
            SqlBuilder.AppendLine(selectPiece);
            return this;
        }

        private SqlServerSqlGenerator<T> And(BinaryExpression expression)
        {
            Append("( ")
                .WhereSubclause(expression.Left)
                .Append(" AND ")
                .WhereSubclause(expression.Right)
                .Append(" )");
            return this;
        }

        private SqlServerSqlGenerator<T> Or(BinaryExpression expression)
        {
            Append("( ")
                .WhereSubclause(expression.Left)
                .Append(" OR ")
                .WhereSubclause(expression.Right)
                .Append(" )");
            return this;
        }

        private SqlServerSqlGenerator<T> Append(string s)
        {
            SqlBuilder.Append(s);
            return this;
        }

        private SqlServerSqlGenerator<T> Not(T entity)
        {
            var names = EntityConfigurations[typeof(T)].MutableProperties.Select(x => x.Name);
            var setPiece = $"SET ({string.Join(',', names)})";
            SqlBuilder.AppendLine(setPiece);
            return this;
        }

        private SqlServerSqlGenerator<T> Set(object entityPartial)
        {
            var dictionary = entityPartial.GetType().GetProperties()
                .Intersect(EntityConfigurations[typeof(T)].MutableProperties,
                    GenericComparer<PropertyInfo>.Create(x => x.Name))
                .ToDictionary(x => x.Name, x => x.GetValue(entityPartial));
            var valuesPiece = $"SET ({string.Join(',', dictionary.Select(x => x.Key))})";
            SqlBuilder.AppendLine(valuesPiece);
            foreach (var name in dictionary) SqlParams[name.Key] = name.Value;
            return this;
        }

        public SqlServerSqlGenerator<T> Update(Expression<Func<T, bool>> predicate, object entityPartial)
        {
            _operationType = OperationType.Update;
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var insertPiece = $"UPDATE {tableName}";
            AppendLine(insertPiece)
                .Set(entityPartial)
                .Values(entityPartial)
                .Where(predicate,ContactorType.And);
            return this;
        }

        private SqlServerSqlGenerator<T> AppendLine(string insertPiece)
        {
            SqlBuilder.AppendLine(insertPiece);
            return this;
        }

        private SqlServerSqlGenerator<T> Values(object entityPartial)
        {
            var dictionary = entityPartial.GetType().GetProperties()
                .Intersect(EntityConfigurations[typeof(T)].MutableProperties,
                    GenericComparer<PropertyInfo>.Create(x => x.Name))
                .ToDictionary(x => x.Name, x => x.GetValue(entityPartial));
            var valuesPiece = $"VALUES ({string.Join(',', dictionary.Select(x => "@" + x.Key))})";
            SqlBuilder.AppendLine(valuesPiece);
            foreach (var name in dictionary) SqlParams[name.Key] = name.Value;
            return this;
        }

        private SqlServerSqlGenerator<T> Values(T entity)
        {
            var dictionary = EntityConfigurations[typeof(T)].MutableProperties
                .ToDictionary(x => x.Name, x => x.GetValue(entity));
            var valuesPiece = $"VALUES ({string.Join(',', dictionary.Select(x => "@" + x.Key))})";
            SqlBuilder.AppendLine(valuesPiece);
            foreach (var name in dictionary) SqlParams[name.Key] = name.Value;
            return this;
        }

        public string BuildLikeSubclause(MethodCallExpression expression,
            LikeClauseMatchType matchType = LikeClauseMatchType.Contains)
        {
            if (expression.Object.NodeType != ExpressionType.MemberAccess)
                throw new LinqToSqlException("只能对Entity的直接成员进行Contains筛选", expression);

            var propertyInfo = (expression.Object as MemberExpression).Member as PropertyInfo;
            var columnName = GetColumnName(propertyInfo);
            string likeSubclause;
            if (expression.Arguments[0] is ConstantExpression constant)
            {
                switch (matchType)
                {
                    case LikeClauseMatchType.StartsWith:
                        likeSubclause = $"{constant.Value}%";
                        break;
                    case LikeClauseMatchType.EndWith:
                        likeSubclause = $"%{constant.Value}";
                        break;
                    default:
                        likeSubclause = $"%{constant.Value}%";
                        break;
                }

                var result = $"{columnName} LIKE {likeSubclause}";
                return result;
            }

            throw new NotSupportedException();
        }

        private string GetColumnName(PropertyInfo member)
        {
            return EntityConfigurations[typeof(T)].PropertyColumnMappings[member];
        }

        private SqlServerSqlGenerator<T> WhereOperatorSubclause(BinaryExpression binaryExpression)
        {
            var propertyAccessor = binaryExpression.Left as MemberExpression;
            if (propertyAccessor == null)
                throw new LinqToSqlException("propertyAccessor需要在运算符左侧", binaryExpression);
            string @operator;
            switch (binaryExpression.NodeType)
            {
                case ExpressionType.Equal:
                    @operator = "=";
                    break;
                case ExpressionType.NotEqual:
                    @operator = "!=";
                    break;
                case ExpressionType.GreaterThan:
                    @operator = ">";
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    @operator = ">=";
                    break;
                case ExpressionType.LessThan:
                    @operator = "<";
                    break;
                case ExpressionType.LessThanOrEqual:
                    @operator = "<=";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("binaryExpression", "不能被转换成运算符");
            }

            var columnName = GetColumnName(propertyAccessor.Member as PropertyInfo);
            var paramName = GetParamName(columnName);
            if (binaryExpression.Right is ConstantExpression constant)
            {
                SqlParams[paramName] = constant.Value;
                SqlBuilder.Append($"{columnName} {@operator} @{paramName}");
                return this;
            }

            var value = Expression.Lambda(binaryExpression.Right).Compile().DynamicInvoke();
            SqlParams[paramName] = value;
            SqlBuilder.Append($"{columnName} {@operator} @{paramName}");
            return this;
        }

        private string GetParamName(string columnName)
        {
            if (SqlParams.ContainsKey(columnName)) return columnName + DuplicateParamIdentifier++;

            return columnName;
        }

        public SqlServerSqlGenerator<T> OrderBy(Expression<Func<T, object>> sort, bool asc = true)
        {
            MemberExpression memberExpression;
            if (sort.Body is UnaryExpression unaryExpression)
                memberExpression = (MemberExpression) unaryExpression.Operand;
            else
                memberExpression = (MemberExpression) sort.Body;
            var columnName = GetColumnName(memberExpression.Member as PropertyInfo);
            var selectPiece = $" ORDER BY {columnName}";
            SqlBuilder.AppendLine(selectPiece);
            return this;
        }

        public SqlServerSqlGenerator<T> Offset(int pageIndex = 0, int pageSize = 10)
        {
            var offsetClause = $"OFFSET {pageIndex * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            SqlBuilder.AppendLine(offsetClause);
            return this;
        }

        public SqlServerSqlGenerator<T> BulkInsert(IEnumerable<T> entities)
        {
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var names = EntityConfigurations[typeof(T)].MutableProperties.Select(x => x.Name);
            var insertPiece = $"INSERT INTO {tableName} {$"({string.Join(',', names)})"}";
            SqlBuilder.AppendLine(insertPiece);
            BulkValues(entities);
            return this;
        }

        private SqlServerSqlGenerator<T> BulkValues(IEnumerable<T> entities)
        {
            var dictionary = EntityConfigurations[typeof(T)].MutableProperties
                .ToDictionary(x => x.Name, x => new Func<T, object>(y => x.GetValue(y)));
            var valuesPiece = "VALUES";
            SqlBuilder.AppendLine(valuesPiece);
            foreach (var entity in entities)
            {
                var line = $"{$"({string.Join(',', dictionary.Select(x => x.Value.Invoke(entity)))}),"}";
                SqlBuilder.AppendLine(line);
            }

            // 去掉尾逗号和\r\n, 共计3个字符
            SqlBuilder = SqlBuilder.Remove(SqlBuilder.Length - 3, 2);
            return this;
        }
    }

    public enum ContactorType
    {
        And,
        Or,
        None
    }

    public enum OperationType
    {
        Select,
        Insert,
        Update,
        Delete
    }

    public enum LikeClauseMatchType
    {
        Contains,
        StartsWith,
        EndWith
    }
}