using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Simpper.NetFramework
{
    public class SqlServerSqlGenerator<T>
    {
        public override string ToString()
        {
            return SqlBuilder.ToString();
        }

        public IDictionary<string, object> SqlParams { get; private set; }

        public StringBuilder SqlBuilder { get; private set; }

        private Dictionary<string, int> DuplicateParamIdentifier = new Dictionary<string, int>();
        public static string ShardingIndex { get; set; }

        public SqlServerSqlGenerator()
        {
            SqlBuilder = new StringBuilder();
            SqlParams = new ExpandoObject() as IDictionary<string, object>;
            if (!EntityConfigurations.ContainsKey(typeof(T)))
            {
                EntityConfigurations.Add(typeof(T), new EntityMappingCache<T>());
            }
        }

        static SqlServerSqlGenerator()
        {
            EntityConfigurations = new Dictionary<Type, EntityMappingCache>();
        }

        public SqlServerSqlGenerator<T> Count()
        {
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var selectPiece = string.Format("SELECT COUNT(1) FROM {0}", tableName);
            SqlBuilder.AppendLine(selectPiece);
            return this;
        }

        public SqlServerSqlGenerator<T> Where(Expression<Func<T, bool>> predicate, ContactorType contactorType = ContactorType.None)
        {
            var expression = predicate as LambdaExpression;
            if (expression == null)
                throw new LinqToSqlException(predicate);
            SqlBuilder.AppendLine("WHERE");
            WhereSubclause(expression);
            SqlBuilder.AppendLine();
            return this;
        }
        public SqlServerSqlGenerator<T> WhereSubclause(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Lambda:
                    return WhereSubclause((expression as LambdaExpression).Body);
                //case ExpressionType.MemberAccess:
                //    {
                //        var memberExpression = expression as MemberExpression;
                //        if (memberExpression.GetValue() != null)
                //        {

                //        }
                //        if (EntityConfigurations[typeof(T)].MappedProperties.Contains(memberExpression.Member as PropertyInfo) 
                //            && Nullable.GetUnderlyingType(memberExpression.Member.DeclaringType) != null)
                //        {
                //            return this.Null();
                //        }

                //        return memberExpression.GetValue();
                //    }
                //子句不单纯, 需要继续拆分
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    {
                        var exactExpression = (BinaryExpression)expression;
                        return expression.NodeType == ExpressionType.AndAlso
                            ? And(exactExpression)
                            : this.Or(exactExpression);
                    }
                case ExpressionType.Not:
                    {
                        return this.Not(expression as UnaryExpression);
                    }
                case ExpressionType.Call:
                    {
                        WhereMethodSubclause(expression as MethodCallExpression);
                        break;
                    }
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    {
                        WhereOperatorSubclause(expression as BinaryExpression);
                        break;
                    }
                case ExpressionType.Constant:
                    this.SqlBuilder.Append(" 1 = 1 ");
                    return this;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return this;
        }

        private SqlServerSqlGenerator<T> WhereMethodSubclause(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments[0] is ConstantExpression &&
                ((ConstantExpression)methodCallExpression.Arguments[0]).Value is string)
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "Contains":
                        SqlBuilder.AppendLine(BuildLikeSubclause(methodCallExpression));
                        break;
                    case "StartsWith":
                        SqlBuilder.AppendLine(BuildLikeSubclause(methodCallExpression, LikeClauseMatchType.StartsWith));
                        break;
                    case "EndsWith":
                        SqlBuilder.AppendLine(BuildLikeSubclause(methodCallExpression, LikeClauseMatchType.EndWith));
                        break;
                }

                return this;
            }

            if (methodCallExpression.Arguments[0] is MemberExpression && EntityConfigurations[typeof(T)].MappedProperties.Contains((methodCallExpression.Arguments[0] as MemberExpression).Member))
            {
                switch (methodCallExpression.Method.Name)
                {
                    case "In":
                        this.In(methodCallExpression);
                        break;
                }
            }


            return this;
        }

        private SqlServerSqlGenerator<T> In(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Arguments[0] is MemberExpression)
            {
                var propertyInfo = (methodCallExpression.Arguments[0] as MemberExpression).Member as PropertyInfo;
                var columnName = GetColumnName(propertyInfo);
                var values = methodCallExpression.Arguments[1].GetValue() as IEnumerable;
                var @params = new List<string>();
                foreach (var value in values)
                {
                    var paramName = GetParamName(columnName);
                    SqlParams[paramName] = value;
                    @params.Add("@" + paramName);
                }

                if (@params.Count == 0)
                {
                    throw new LinqToSqlException(methodCallExpression);
                }
                var inClause = string.Format("{0} IN ({1})", columnName, string.Join(",", @params));
                SqlBuilder.Append(inClause);
            }

            return this;

            //GetColumnName(methodCallExpression.Object.Type.GetProperty());
        }

        public SqlServerSqlGenerator<T> Delete(Expression<Func<T, bool>> predicate)
        {
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var deletePiece = string.Format("DELETE FROM {0}", tableName);
            SqlBuilder.AppendLine(deletePiece);
            Where(predicate, ContactorType.And);
            SqlBuilder.AppendLine(" SELECT @@ROWCOUNT");
            return this;
        }

        public static Dictionary<Type, EntityMappingCache> EntityConfigurations { get; private set; }


        public SqlServerSqlGenerator<T> Insert(T entity)
        {
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var names = EntityConfigurations[typeof(T)].MutableProperties.Select(x => x.Name);
            var insertClause = string.Format("INSERT INTO {0} {1}", tableName,
                string.Format("({0})", string.Join(",", names)));
            SqlBuilder.AppendLine(insertClause);
            Values(entity);
            return this;
        }


        public SqlServerSqlGenerator<T> Select(int? top = null)
        {
            var names = EntityConfigurations[typeof(T)].MappedProperties.Select(x => x.Name);
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var selectPiece =
                string.Format("SELECT {0} {1} FROM {2}", top.HasValue ? "TOP(" + top + ")" : string.Empty,
                    string.Join(",", names), tableName);
            SqlBuilder.AppendLine(selectPiece);
            return this;
        }

        private SqlServerSqlGenerator<T> And(BinaryExpression expression)
        {
            this.SqlBuilder.Append("( ");
            this.WhereSubclause(expression.Left);
            this.SqlBuilder.Append(" AND ");
            this.WhereSubclause(expression.Right);
            this.SqlBuilder.Append(" )");
            return this;
        }

        private SqlServerSqlGenerator<T> Or(BinaryExpression expression)
        {
            this.SqlBuilder.Append("( ");
            this.WhereSubclause(expression.Left);
            this.SqlBuilder.Append(" OR ");
            this.WhereSubclause(expression.Right);
            this.SqlBuilder.Append(" )");
            return this;
        }

        private SqlServerSqlGenerator<T> Not(UnaryExpression unarrayExpression)
        {
            this.SqlBuilder.Append("NOT ");
            return this.WhereSubclause(unarrayExpression.Operand);
        }

        private SqlServerSqlGenerator<T> Set(object entityPartial)
        {
            var dictionary = entityPartial.GetType().GetProperties()
                .Intersect(EntityConfigurations[typeof(T)].MutableProperties,
                    GenericComparer<PropertyInfo>.Create(x => x.Name))
                .ToDictionary(x => x.Name, x => x.GetValue(entityPartial));
            var valuesPiece = string.Format("SET {0}", string.Join(",", dictionary.Select(x => string.Format("{0} = @{1}", x.Key, x.Key))));
            SqlBuilder.Append(valuesPiece);
            foreach (var keyValuePair in dictionary)
                SqlParams[keyValuePair.Key] = keyValuePair.Value;
            return this;
        }

        public SqlServerSqlGenerator<T> Update(Expression<Func<T, bool>> predicate, object entityPartial)
        {
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var updateClause = string.Format("UPDATE {0}", tableName);
            SqlBuilder.AppendLine(updateClause);
            this.Set(entityPartial);
            //this.Values(entityPartial);
            SqlBuilder.AppendLine();
            this.Where(predicate, ContactorType.And);
            return this;
        }

        //private SqlServerSqlGenerator<T> Values(object entityPartial)
        //{
        //    var dictionary = entityPartial.GetType().GetProperties()
        //        .Intersect(EntityConfigurations[typeof(T)].MutableProperties,
        //            GenericComparer<PropertyInfo>.Create(x => x.Name))
        //        .ToDictionary(x => x.Name, x => x.GetValue(entityPartial));
        //    var valuesClause = "VALUES";
        //    SqlBuilder.AppendLine(valuesClause);
        //    this.SqlBuilder.Append("(");
        //    foreach (var keyValuePair in dictionary)
        //    {
        //        var paramName = GetParamName(keyValuePair.Key);
        //        SqlBuilder.Append(string.Format("@{0},", paramName));
        //        SqlParams[paramName] = keyValuePair.Value;
        //    }

        //    SqlBuilder.Remove(SqlBuilder.Length - 1, 1);
        //    this.SqlBuilder.Append("),");
        //    // 去掉尾逗号
        //    SqlBuilder = SqlBuilder.Remove(SqlBuilder.Length - 1, 1);
        //    return this;
        //}

        private SqlServerSqlGenerator<T> Values(T entity)
        {
            var dictionary = EntityConfigurations[typeof(T)].MutableProperties
                .ToDictionary(x => x.Name, x => x.GetValue(entity));
            var valuesClause = "VALUES";
            SqlBuilder.AppendLine(valuesClause);
            this.SqlBuilder.Append("(");
            foreach (var keyValuePair in dictionary)
            {
                var paramName = GetParamName(keyValuePair.Key);
                SqlBuilder.Append(string.Format("@{0},", paramName));
                SqlParams[paramName] = keyValuePair.Value;
            }

            SqlBuilder.Remove(SqlBuilder.Length - 1, 1);
            this.SqlBuilder.Append("),");
            // 去掉尾逗号
            SqlBuilder = SqlBuilder.Remove(SqlBuilder.Length - 1, 1);
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
            var constant = expression.Arguments[0] as ConstantExpression;
            if (constant != null)
            {
                switch (matchType)
                {
                    case LikeClauseMatchType.StartsWith:
                        likeSubclause = string.Format("{0}%", constant.Value);
                        break;
                    case LikeClauseMatchType.EndWith:
                        likeSubclause = string.Format("%{0}", constant.Value);
                        break;
                    default:
                        likeSubclause = string.Format("%{0}%", constant.Value);
                        break;
                }

                var result = string.Format("{0} LIKE {1}", columnName, likeSubclause);
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
            var propertyAccessor = binaryExpression.Left.Simplify() as MemberExpression;
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
            var constant = binaryExpression.Right.GetValue();

            if (constant == null)
            {
                SqlBuilder.Append(string.Format("{0} IS {1}NULL", columnName, @operator == "=" ? "" : "NOT ", paramName));
                return this;
            }

            SqlParams[paramName] = constant;
            SqlBuilder.Append(string.Format("{0} {1} @{2}", columnName, @operator, paramName));
            return this;
        }

        private string GetParamName(string columnName)
        {
            string paramName;
            int identifier;
            if (!DuplicateParamIdentifier.TryGetValue(columnName, out identifier))
            {
                identifier = 0;
            }
            paramName = columnName + identifier;
            DuplicateParamIdentifier[columnName] = identifier + 1;

            return paramName;
        }

        public SqlServerSqlGenerator<T> OrderBy(Expression<Func<T, object>> sort, bool asc = true)
        {
            MemberExpression memberExpression;
            var unaryExpression = sort.Body as UnaryExpression;
            if (unaryExpression != null)
                memberExpression = (MemberExpression)unaryExpression.Operand;
            else
                memberExpression = (MemberExpression)sort.Body;
            var columnName = GetColumnName(memberExpression.Member as PropertyInfo);
            var selectPiece = string.Format(" ORDER BY {0} {1}", columnName, asc?"ASC":"DESC");
            SqlBuilder.AppendLine(selectPiece);
            return this;
        }

        public SqlServerSqlGenerator<T> Offset(int pageIndex = 0, int pageSize = 10)
        {
            var offsetClause =
                string.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", pageIndex * pageSize, pageSize);
            SqlBuilder.AppendLine(offsetClause);
            return this;
        }

        public SqlServerSqlGenerator<T> BulkInsert(IEnumerable<T> entities)
        {
            var tableName = EntityConfigurations[typeof(T)].GetTableName(ShardingIndex);
            var names = EntityConfigurations[typeof(T)].MutableProperties.Select(x => x.Name);
            var insertPiece = string.Format("INSERT INTO {0} {1}", tableName,
                string.Format("({0})", string.Join(",", names)));
            SqlBuilder.AppendLine(insertPiece);
            BulkValues(entities);
            SqlBuilder.Remove(SqlBuilder.Length - 2, 2);
            return this;
        }

        private SqlServerSqlGenerator<T> BulkValues(IEnumerable<T> entities)
        {
            var dictionary = EntityConfigurations[typeof(T)].MutableProperties
                .ToDictionary(x => x.Name, x => new Func<T, object>(y => x.GetValue(y)));
            var valuesClause = "VALUES";
            SqlBuilder.AppendLine(valuesClause);
            foreach (var entity in entities)
            {
                this.SqlBuilder.Append("(");
                foreach (var keyValuePair in dictionary)
                {
                    var paramName = GetParamName(keyValuePair.Key);
                    SqlBuilder.Append(string.Format("@{0},", paramName));
                    SqlParams[paramName] = keyValuePair.Value.Invoke(entity);
                }

                SqlBuilder.Remove(SqlBuilder.Length - 1, 1);
                this.SqlBuilder.Append("),");
                this.SqlBuilder.AppendLine();
            }

            // 去掉尾逗号
            SqlBuilder = SqlBuilder.Remove(SqlBuilder.Length - 1, 1);
            return this;
        }
    }

    public enum ContactorType
    {
        And,
        Or,
        None
    }
    [Flags]
    public enum GenerateStage
    {
        //todo:还没有想清楚如何设计
        Select = 0,
        Insert = 1,
        Update = 2,
        Delete = 4,
        Where = 8,
        GroupBy = 16,
        OrderBy = 32,
        Set = 64,
        Values = 128,
        Offset = 256,
        Fetch = 512,
        Top = 1024
    }

    public enum LikeClauseMatchType
    {
        Contains,
        StartsWith,
        EndWith
    }
}