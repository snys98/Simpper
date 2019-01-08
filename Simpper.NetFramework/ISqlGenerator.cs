//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Text;

//namespace Simpper
//{
//    public interface ISqlGenerator
//    {
//        StringBuilder SqlBuilder { get; }
//        ISqlGenerator Select<T>(int? top = null);
//        ISqlGenerator Count<T>();
//        ISqlGenerator Where<T>(Expression<Func<T, bool>> predicate);

//        ISqlGenerator Insert<T>(T entity);
//        ISqlGenerator Set<T>(T entity);
//        ISqlGenerator Values<T>(T entity);
//        ISqlGenerator Sort<T>(Expression<Func<T, object>> predicate);
//        ISqlGenerator Update<T>(Expression<Func<T, bool>> predicate, object anonymousParams);
//        ISqlGenerator Delete<T>(Expression<Func<T, bool>> predicate);
//        ISqlGenerator IdentitySql<T>(EntityMappingCache<T> entityConfiguration);
//    }
//}