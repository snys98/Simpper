using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Simpper
{
    public class LinqToSqlException : Exception
    {
        public LinqToSqlException()
        {
        }

        public LinqToSqlException(Expression expression) : base("不支持的表达式: " + expression)
        {
        }

        public LinqToSqlException(string message, Expression expression) : base(message + Environment.NewLine + "expression:" + expression)
        {

        }
    }
}
