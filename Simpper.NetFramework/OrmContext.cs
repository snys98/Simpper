using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Dapper;

namespace Simpper.NetFramework
{
    public class OrmContext : IDisposable
    {
        private SqlConnection _conn;
        private Func<string, string> _shardingIndexSelector;

        public T QueryFirst<T>(Expression<Func<T, bool>> predicate)
        {
            var generator = new SqlServerSqlGenerator<T>().Select(1).Where(predicate, ContactorType.And);
            var sql = generator.ToString();
            return this._conn.QueryFirst<T>(sql, generator.SqlParams);
        }

        public List<T> QueryPage<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, object>> sort, int pageIndex = 0, int pageSize = 10)
        {
            var generator = new SqlServerSqlGenerator<T>().Select().Where(predicate,ContactorType.And).OrderBy(sort).Offset(pageIndex, pageSize);
            var sql = generator.ToString();
            return this._conn.Query<T>(sql, generator.SqlParams).ToList();
        }

        public int Insert<T>(T entity)
        {
            var generator = new SqlServerSqlGenerator<T>().Insert(entity);
            var sql = generator.ToString();
            return this._conn.Execute(sql, generator.SqlParams);
        }

        public int Update<T>(Expression<Func<T,bool>> predicate,object entityPartial)
        {
            var generator = new SqlServerSqlGenerator<T>().Update(predicate,entityPartial);
            var sql = generator.ToString();
            return this._conn.Execute(sql, generator.SqlParams);
        }

        public int BulkInsert<T>(IEnumerable<T> entities)
        {
            var generator = new SqlServerSqlGenerator<T>().BulkInsert(entities);
            var sql = generator.ToString();
            return this._conn.Execute(sql, generator.SqlParams);
        }

        public long Count<T>(Expression<Func<T, bool>> predicate)
        {
            var generator = new SqlServerSqlGenerator<T>().Count().Where(predicate, ContactorType.And);
            var sql = generator.ToString();
            return this._conn.ExecuteScalar<long>(sql, generator.SqlParams);
        }

        public long Delete<T>(Expression<Func<T, bool>> predicate)
        {
            var generator = new SqlServerSqlGenerator<T>().Delete(predicate);
            var sql = generator.ToString();
            return this._conn.ExecuteScalar<long>(sql, generator.SqlParams);
        }

        public OrmContext(SqlConnection conn, Func<string, string> shardingIndexSelector = null)
        {
            _conn = conn;
            _shardingIndexSelector = shardingIndexSelector ?? (x => x);
        }

        public void SwitchSharding<T>(string dbIndex, SqlConnection conn)
        {
            var tableIndex = this._shardingIndexSelector.Invoke(dbIndex);
            SqlServerSqlGenerator<T>.ShardingIndex = tableIndex;
            _conn = conn;
        }

        public void Dispose()
        {
            this._conn.Dispose();
        }
    }
}
