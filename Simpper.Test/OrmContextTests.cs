using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Dapper;
using FluentAssertions;
using Moq;
using Xunit;

namespace Simpper.Test
{
    public class OrmContextTests : IDisposable
    {
        private MockRepository mockRepository;
        private string _connStr = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Test;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        private string _connStr1 = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=TestShader1;Integrated Security=True";
        private string _connStr2 = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=TestShader2;Integrated Security=True";


        public OrmContextTests()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);
            using (var conn = new SqlConnection(_connStr))
            {
                conn.Execute("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
                conn.Execute("EXEC sp_MSforeachtable @command1 = \"DROP TABLE ?\"");
                conn.Execute(@"CREATE TABLE [dbo].[TestEntity]
                (
                    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
                    [StringField] NVARCHAR(MAX) NULL, 
                    [IntField] INT NOT NULL,
                    [ShadowField] NVARCHAR(MAX) NULL,
                )
                ");


            }

            using (var conn = new SqlConnection(_connStr1))
            {
                conn.Execute("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
                conn.Execute("EXEC sp_MSforeachtable @command1 = \"DROP TABLE ?\"");
                conn.Execute(@"CREATE TABLE [dbo].[ShardingEntity_1]
                (
                    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
                    [StringField] NVARCHAR(MAX) NULL, 
                    [IntField] INT NOT NULL,
                    [ShadowField] NVARCHAR(MAX) NULL,
                )
                ");

                conn.Execute(@"CREATE TABLE [dbo].[ShardingEntity_2]
                (
                    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
                    [StringField] NVARCHAR(MAX) NULL, 
                    [IntField] INT NOT NULL,
                    [ShadowField] NVARCHAR(MAX) NULL,
                )
                ");
            }

            using (var conn = new SqlConnection(_connStr2))
            {
                conn.Execute("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
                conn.Execute("EXEC sp_MSforeachtable @command1 = \"DROP TABLE ?\"");
                conn.Execute(@"CREATE TABLE [dbo].[ShardingEntity_1]
                (
                    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
                    [StringField] NVARCHAR(MAX) NULL, 
                    [IntField] INT NOT NULL,
                    [ShadowField] NVARCHAR(MAX) NULL,
                )
                ");

                conn.Execute(@"CREATE TABLE [dbo].[ShardingEntity_2]
                (
                    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
                    [StringField] NVARCHAR(MAX) NULL, 
                    [IntField] INT NOT NULL,
                    [ShadowField] NVARCHAR(MAX) NULL,
                )
                ");
            }
        }

        public void Dispose()
        {
            this.mockRepository.VerifyAll();
        }

        private OrmContext CreateOrmContext(SqlConnection connection)
        {
            return new OrmContext(connection);
        }

        private OrmContext CreateOrmContext(SqlConnection connection, Func<string, string> sharding)
        {
            return new OrmContext(connection, sharding);
        }
        [Fact]
        public void QueryFirst_should_success_when_has_key_identity()
        {
            using (var unitUnderTest = this.CreateOrmContext(new SqlConnection(_connStr)))
            {
                var count = unitUnderTest.Insert(new TestEntity()
                {
                    IntField = 3,
                    StringField = "233"
                });
                unitUnderTest.QueryFirst<TestEntity>(x => x.IntField == 3).Should().NotBeNull();
                unitUnderTest.QueryFirst<TestEntity>(x => x.IntField >= 3).Should().NotBeNull();
                unitUnderTest.QueryFirst<TestEntity>(x => x.IntField > 2).Should().NotBeNull();
                unitUnderTest.QueryFirst<TestEntity>(x => x.IntField < 4).Should().NotBeNull();
                try
                {
                    unitUnderTest.QueryFirst<TestEntity>(x => x.IntField != 3);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<InvalidOperationException>();
                }
                try
                {
                    unitUnderTest.QueryFirst<TestEntity>(x => x.IntField > 3);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<InvalidOperationException>();
                }
                try
                {
                    unitUnderTest.QueryFirst<TestEntity>(x => x.IntField > 3);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<InvalidOperationException>();
                }
            }
        }

        [Fact]
        public void insert_and_query_should_be_in_correct_sharding()
        {
            using (var unitUnderTest1 = this.CreateOrmContext(new SqlConnection(_connStr), x => x))
            {
                unitUnderTest1.SwitchSharding<ShardingEntity>("1", new SqlConnection(_connStr1));
                for (int i = 0; i < 10; i++)
                {
                    unitUnderTest1.Insert(new ShardingEntity()
                    {
                        IntField = -i
                    });
                }
            }

            using (var unitUnderTest2 = this.CreateOrmContext(new SqlConnection(_connStr), x => x))
            {
                unitUnderTest2.SwitchSharding<ShardingEntity>("2", new SqlConnection(_connStr2));
                for (int i = 0; i < 10; i++)
                {
                    unitUnderTest2.Insert(new ShardingEntity()
                    {
                        IntField = i
                    });
                }
            }

            using (var unitUnderTest1 = this.CreateOrmContext(new SqlConnection(_connStr), x => x))
            {
                unitUnderTest1.SwitchSharding<ShardingEntity>("1", new SqlConnection(_connStr1));
                var result = unitUnderTest1.QueryPage<ShardingEntity>(x => true, x => x.Id);
                result.All(x => x.IntField <= 0).Should().BeTrue();
            }

            using (var unitUnderTest2 = this.CreateOrmContext(new SqlConnection(_connStr), x => x))
            {
                unitUnderTest2.SwitchSharding<ShardingEntity>("2", new SqlConnection(_connStr2));
                var result = unitUnderTest2.QueryPage<ShardingEntity>(x => true, x => x.Id);
                result.All(x => x.IntField >= 0).Should().BeTrue();
            }
        }

        [Fact]
        public void QueryPage_should_success()
        {
            // Arrange
            using (var unitUnderTest = this.CreateOrmContext(new SqlConnection(_connStr)))
            {

                for (int i = 0; i < 50; i++)
                {
                    unitUnderTest.Insert(new TestEntity()
                    {
                        IntField = i - 48
                    });
                }

                int pageIndex = 1;
                int pageSize = 20;

                // Act
                var result = unitUnderTest.QueryPage<TestEntity>(
                    x => true,
                    x => x.StringField,
                    pageIndex,
                    pageSize);

                // Assert
                result.Count.Should().Be(pageSize);
                result[0].IntField.Should().Be(-28);
                result[0].IntField.Should().Be(result.Max(x => x.IntField));
                for (var i = 1; i < result.Count; i++)
                {
                    (result[i - 1].IntField > result[i].IntField).Should().BeTrue();
                }
            }
        }

        [Fact]
        public void QueryPage_should_return_rest_when_not_enough()
        {
            // Arrange
            using (var unitUnderTest = this.CreateOrmContext(new SqlConnection(_connStr)))
            {

                for (int i = 0; i < 50; i++)
                {
                    unitUnderTest.Insert(new TestEntity()
                    {
                        IntField = i - 48
                    });
                }

                Expression<Func<TestEntity, bool>> predicate = x => true;
                Expression<Func<TestEntity, object>> sort = x => x.StringField;
                int pageIndex = 1;
                int pageSize = 30;

                // Act
                var result = unitUnderTest.QueryPage(
                    predicate,
                    sort,
                    pageIndex,
                    pageSize);

                // Assert
                result.Count.Should().Be(20);
            }
        }

        [Fact]
        public void Count_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            using (var unitUnderTest = this.CreateOrmContext(new SqlConnection(_connStr)))
            {
                for (int i = 0; i < 50; i++)
                {
                    unitUnderTest.Insert(new TestEntity()
                    {
                        IntField = i
                    });
                }
                // Act
                var result = unitUnderTest.Count<TestEntity>(x => x.IntField < 28);

                // Assert
                result.Should().Be(28);
            }
        }
        [Fact]
        public void Delete_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            using (var unitUnderTest = this.CreateOrmContext(new SqlConnection(_connStr)))
            {
                for (int i = 0; i < 50; i++)
                {
                    unitUnderTest.Insert(new TestEntity()
                    {
                        IntField = i
                    });
                }

                // Act
                var result = unitUnderTest.Delete<TestEntity>(x => x.IntField < 28);

                // Assert
                result.Should().Be(28);
            }
        }

        [Fact]
        public void bulk_insert_should_success()
        {
            // Arrange
            using (var unitUnderTest = this.CreateOrmContext(new SqlConnection(_connStr)))
            {
                var list = new List<TestEntity>();
                for (int i = 0; i < 1000; i++)
                {
                    list.Add(new TestEntity()
                    {
                        IntField = i,
                        StringField = i.ToString()
                    });
                }

                unitUnderTest.BulkInsert(list);
                int pageIndex = 0;
                int pageSize = 1000;

                // Act
                var result = unitUnderTest.QueryPage<TestEntity>(
                    x => true,
                    x => x.StringField,
                    pageIndex,
                    pageSize);

                // Assert
                result.Count.Should().Be(1000);
            }
        }

        [Fact(Skip = "skip performance test")]
        public void PerformanceTest_100W_rows_page_query()
        {
            // Arrange
            using (var unitUnderTest = this.CreateOrmContext(new SqlConnection(_connStr)))
            {
                for (int j = 0; j < 1000; j++)
                {
                    var list = new List<TestEntity>();
                    for (int i = 0; i < 1000; i++)
                    {
                        list.Add(new TestEntity()
                        {
                            IntField = i,
                            StringField = i.ToString()
                        });
                    }

                    unitUnderTest.BulkInsert(list);
                }
                int pageIndex = 200;
                int pageSize = 1000;

                // Act
                var result = unitUnderTest.QueryPage<TestEntity>(
                    x => true,
                    x => x.StringField,
                    pageIndex,
                    pageSize);

                // Assert
                result.Count.Should().Be(pageSize);
            }
        }
    }
}
