using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Simpper.NetFramework.Test
{
    [TestClass]
    public class OrmContextTests
    {
        private static readonly string outputFolder =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Data");

        private readonly MockRepository mockRepository;

        private readonly string _connStr =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Test;Integrated Security=True";

        private readonly string _connStr1 =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=TestShader1;Integrated Security=True";

        private readonly string _connStr2 =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=TestShader2;Integrated Security=True";

        public OrmContextTests()
        {
            // try create database

            mockRepository = new MockRepository(MockBehavior.Strict);
            CreateDatabaseIfNotExist();
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
                    [DateTimeField] DATETIME NOT NULL,
                    [NullableDateTimeField] DATETIME NULL,
                    [EnumField] INT NOT NULL,
                    [DecimalField] DECIMAL(18,18) NOT NULL,
                    [GuidField] UNIQUEIDENTIFIER NOT NULL,
                    [LongField] BIGINT NULL,
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
            mockRepository.VerifyAll();
        }

        [TestMethod]
        public void select_one_should_success_when_has_key_identity()
        {
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                var count = ormContext.Insert(new TestEntity
                {
                    IntField = 3,
                    StringField = "233"
                });
                ormContext.QueryFirst<TestEntity>(x => x.IntField == 3).Should().NotBeNull();
                ormContext.QueryFirst<TestEntity>(x => x.IntField >= 3).Should().NotBeNull();
                ormContext.QueryFirst<TestEntity>(x => x.IntField > 2).Should().NotBeNull();
                ormContext.QueryFirst<TestEntity>(x => x.IntField < 4).Should().NotBeNull();
                try
                {
                    ormContext.QueryFirst<TestEntity>(x => x.IntField != 3);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<InvalidOperationException>();
                }

                try
                {
                    ormContext.QueryFirst<TestEntity>(x => x.IntField > 3);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<InvalidOperationException>();
                }

                try
                {
                    ormContext.QueryFirst<TestEntity>(x => x.IntField > 3);
                }
                catch (Exception e)
                {
                    e.Should().BeOfType<InvalidOperationException>();
                }
            }
        }

        [TestMethod]
        public void insert_and_query_should_be_in_correct_sharding()
        {
            using (var ormContext1 = new SqlConnection(_connStr).ToOrmContext(x => x))
            {
                ormContext1.SwitchSharding<ShardingEntity>("1", new SqlConnection(_connStr1));
                for (var i = 0; i < 10; i++)
                    ormContext1.Insert(new ShardingEntity
                    {
                        IntField = -i
                    });
            }

            using (var ormContext2 = new SqlConnection(_connStr).ToOrmContext(x => x))
            {
                ormContext2.SwitchSharding<ShardingEntity>("2", new SqlConnection(_connStr2));
                for (var i = 0; i < 10; i++)
                    ormContext2.Insert(new ShardingEntity
                    {
                        IntField = i
                    });
            }

            using (var ormContext1 = new SqlConnection(_connStr).ToOrmContext(x => x))
            {
                ormContext1.SwitchSharding<ShardingEntity>("1", new SqlConnection(_connStr1));
                var result = ormContext1.QueryPage<ShardingEntity>(x => true, x => x.Id);
                result.All(x => x.IntField <= 0).Should().BeTrue();
            }

            using (var ormContext2 = new SqlConnection(_connStr).ToOrmContext(x => x))
            {
                ormContext2.SwitchSharding<ShardingEntity>("2", new SqlConnection(_connStr2));
                var result = ormContext2.QueryPage<ShardingEntity>(x => true, x => x.Id);
                result.All(x => x.IntField >= 0).Should().BeTrue();
            }
        }

        [TestMethod]
        public void select_list_should_success()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 50; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i - 48
                    });

                var pageIndex = 1;
                var pageSize = 20;

                // Act
                var result = ormContext.QueryPage<TestEntity>(
                    x => true,
                    x => x.StringField,
                    pageIndex: pageIndex,
                    pageSize: pageSize);

                // Assert
                result.Count.Should().Be(pageSize);
                result[0].IntField.Should().Be(-28);
                result[0].IntField.Should().Be(result.Max(x => x.IntField));
                for (var i = 1; i < result.Count; i++) (result[i - 1].IntField > result[i].IntField).Should().BeTrue();
            }
        }

        [TestMethod]
        public void select_should_work_with_nest_conditions()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 50; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i,
                        StringField = (i + 1).ToString(),
                        EnumField = TestEnum.One,
                        LongField = 2 * i
                    });

                // Act
                var result = ormContext.QueryPage<TestEntity>(
                    x => x.IntField == 30 || x.LongField == 68 || x.StringField == "32" ||
                         x.StringField == "33" && x.IntField == 33 || !(x.IntField < 49),
                    x => x.IntField, pageIndex: 0, pageSize: 100);

                // Assert
                result.Count.Should().Be(4);
                result[0].IntField.Should().Be(30);
                result[1].IntField.Should().Be(31);
                //result[2].IntField.Should().Be(32);
                result[2].IntField.Should().Be(34);
                result[3].IntField.Should().Be(49);
            }
        }

        [TestMethod]
        public void select_should_work_with_nullable_param()
        {
            GetValueByIntField(10);
            // Arrange
        }

        private int GetValueByIntField(int? value)
        {
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 50; i++) ormContext.Insert(new TestEntity {IntField = i, StringField = (i + 1).ToString(), EnumField = TestEnum.One, LongField = 2 * i});

                // Act
                var result = ormContext.QueryFirst<TestEntity>(x => x.IntField == value);

                // Assert
                result.IntField.Should().Be(value);
                return result.IntField;
            }
        }

        [TestMethod]
        public void select_should_work_with_nullable_date_time()
        {
            GetValueByDateTimeField((DateTime?) DateTime.Now).Should().BeAfter(DateTime.Now);
            GetValueByDateTimeField(null).Should().Be(DateTime.Today.AddDays(1));
            // Arrange
        }

        private DateTime GetValueByDateTimeField(DateTime? value)
        {
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                ormContext.Insert(new TestEntity
                {
                    IntField = 0,
                    StringField = (0 + 1).ToString(),
                    EnumField = TestEnum.One,
                    LongField = 2 * 0,
                    DateTimeField = DateTime.Today.AddDays(1)
                });

                // Act
                var result = ormContext.QueryFirst<TestEntity>(x => x.DateTimeField >= (value ?? DateTimeOffset.MinValue));

                // Assert
                return result.DateTimeField;
            }
        }

        [TestMethod]
        public void select_should_work_with_self_nullable()
        {
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 2; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i,
                        StringField = (i + 1).ToString(),
                        EnumField = TestEnum.One,
                        LongField = 2 * i,
                        NullableDateTimeField = i == 0 ? DateTime.Now : (DateTime?) null
                    });

                // Act
                var result = ormContext.QueryFirst<TestEntity>(
                    x => x.NullableDateTimeField != null);

                // Assert
                result.IntField.Should().Be(0);

                var result1 = ormContext.QueryFirst<TestEntity>(
                    x => x.NullableDateTimeField == null);

                // Assert
                result1.IntField.Should().Be(1);
            }

            // Arrange
        }

        [TestMethod]
        public void select_should_work_with_list_contains()
        {
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 10; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i,
                        StringField = (i + 1).ToString(),
                        EnumField = TestEnum.One,
                        LongField = 2 * i,
                        NullableDateTimeField = i == 0 ? DateTime.Now : (DateTime?) null
                    });

                // Act
                var result = ormContext.QueryPage<TestEntity>(
                    x => x.IntField.In(new[] {2, 4, 7, 8}) && x.IntField != 8, x => x.IntField, false);

                // Assert
                result.Count.Should().Be(3);

                result[0].IntField.Should().Be(7);
                result[1].IntField.Should().Be(4);
                result[2].IntField.Should().Be(2);
            }
        }

        [TestMethod]
        public void select_should_work_with_string_contains()
        {
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 10; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i,
                        StringField = "string"+(i + 1).ToString(),
                        EnumField = TestEnum.One,
                        LongField = 2 * i,
                        NullableDateTimeField = i == 0 ? DateTime.Now : (DateTime?)null
                    });

                // Act
                var result = ormContext.QueryPage<TestEntity>(
                    x => x.StringField.Contains("string1"), x => x.IntField, false);

                // Assert
                result.Count.Should().Be(2);

                result[0].IntField.Should().Be(9);
                result[1].IntField.Should().Be(0);
            }
        }

        [TestMethod]
        public void select_list_should_return_rest_when_not_enough()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 50; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i - 48
                    });

                Expression<Func<TestEntity, bool>> predicate = x => x.IntField != 900;
                Expression<Func<TestEntity, object>> sort = x => x.StringField;
                var pageIndex = 1;
                var pageSize = 30;

                // Act
                var result = ormContext.QueryPage(
                    predicate,
                    sort,
                    pageIndex: pageIndex,
                    pageSize: pageSize);

                // Assert
                result.Count.Should().Be(20);
            }
        }

        [TestMethod]
        public void count_should_success()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 50; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i
                    });
                // Act
                var result = ormContext.Count<TestEntity>(x => x.IntField < 28);

                // Assert
                result.Should().Be(28);
            }
        }

        [TestMethod]
        public void insert_all_fields_should_success()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                var entity = new TestEntity
                {
                    IntField = 1,
                    DateTimeField = DateTime.Now.ToUnixEpoch(),
                    DecimalField = 0.23333333333M,
                    EnumField = TestEnum.One,
                    ExtraNotMapped = "ExtraNotMapped",
                    ExtraNotMappedWithBackingField = "ExtraNotMappedWithBackingField",
                    GuidField = Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"),
                    LongField = 2222222222,
                    NullableDateTimeField = null,
                    StringField = "StringField"
                };
                ormContext.Insert(entity);
                // Act
                var result = ormContext.QueryFirst<TestEntity>(x =>
                    x.GuidField == Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"));

                // Assert
                result.IntField.Should().Be(entity.IntField);
                result.DateTimeField.ToString("s").Should().Be(entity.DateTimeField.ToString("s"));
                result.DecimalField.Should().Be(entity.DecimalField);
                result.EnumField.Should().Be(entity.EnumField);
                result.ExtraNotMapped.Should().Be(null);
                result.ExtraNotMappedWithBackingField.Should().Be(null);
                result.GuidField.Should().Be(Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"));
                result.LongField.Should().Be(entity.LongField);
                result.NullableDateTimeField.Should().Be(null);
                result.StringField.Should().Be(entity.StringField);
            }
        }

        [TestMethod]
        public void update_partial_fields_should_success()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                var entity = new TestEntity
                {
                    IntField = 1,
                    DateTimeField = DateTime.Now.ToUnixEpoch(),
                    DecimalField = 0.23333333333M,
                    EnumField = TestEnum.One,
                    ExtraNotMapped = "ExtraNotMapped",
                    ExtraNotMappedWithBackingField = "ExtraNotMappedWithBackingField",
                    GuidField = Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"),
                    LongField = 2222222222,
                    NullableDateTimeField = null,
                    StringField = "StringField"
                };
                ormContext.Insert(entity);
                // Act
                var result = ormContext.QueryFirst<TestEntity>(x =>
                    x.GuidField == Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"));

                // Assert
                result.IntField.Should().Be(entity.IntField);
                result.DateTimeField.ToString("s").Should().Be(entity.DateTimeField.ToString("s"));
                result.DecimalField.Should().Be(entity.DecimalField);
                result.EnumField.Should().Be(entity.EnumField);
                result.ExtraNotMapped.Should().Be(null);
                result.ExtraNotMappedWithBackingField.Should().Be(null);
                result.GuidField.Should().Be(Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"));
                result.LongField.Should().Be(entity.LongField);
                result.NullableDateTimeField.Should().Be(null);
                result.StringField.Should().Be(entity.StringField);

                var updateResult = ormContext.Update<TestEntity>(
                    x => x.GuidField == Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"), new
                    {
                        LongField = entity.LongField + 3,
                        IntField = entity.IntField + 4
                    });
                updateResult.Should().Be(1);
                var afterEntity = ormContext.QueryFirst<TestEntity>(x =>
                    x.GuidField == Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"));
                afterEntity.LongField.Should().Be(entity.LongField + 3);
                afterEntity.IntField.Should().Be(entity.IntField + 4);
            }
        }

        [TestMethod]
        public void delete_should_success()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var i = 0; i < 50; i++)
                    ormContext.Insert(new TestEntity
                    {
                        IntField = i
                    });

                // Act
                var result = ormContext.Delete<TestEntity>(x => x.IntField < 28);

                // Assert
                result.Should().Be(28);
            }
        }

        [TestMethod]
        public void bulk_insert_should_success()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                var list = new List<TestEntity>();
                for (var i = 0; i < 100; i++)
                    list.Add(new TestEntity
                    {
                        IntField = i,
                        StringField = i.ToString()
                    });
                ormContext.BulkInsert(list);
                var pageIndex = 0;
                var pageSize = 1001;

                // Act
                var result = ormContext.QueryPage<TestEntity>(
                    x => x.IntField >= 0 && x.IntField <= 100,
                    x => x.StringField,
                    pageIndex: pageIndex,
                    pageSize: pageSize);

                // Assert
                result.Count.Should().Be(100);
            }
        }

        [TestMethod]
        [Ignore]
        public void PerformanceTest_100W_rows_page_query()
        {
            // Arrange
            using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
            {
                for (var j = 0; j < 1000; j++)
                {
                    var list = new List<TestEntity>();
                    for (var i = 0; i < 1000; i++)
                        list.Add(new TestEntity
                        {
                            IntField = i,
                            StringField = i.ToString()
                        });

                    ormContext.BulkInsert(list);
                }

                var pageIndex = 200;
                var pageSize = 1000;

                // Act
                var result = ormContext.QueryPage<TestEntity>(
                    x => true,
                    x => x.StringField,
                    pageIndex: pageIndex,
                    pageSize: pageSize);

                // Assert
                result.Count.Should().Be(pageSize);
            }
        }

        public static void CreateDatabaseIfNotExist()
        {
            if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

            var testDbPath = Path.Combine(outputFolder, "Test.mdf");
            if (!File.Exists(testDbPath))
            {
                var connectionString =
                    "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = string.Format(@"exec sp_detach_db '{0}';", "Test");
                    cmd.CommandText += string.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", "Test",
                        testDbPath);
                    cmd.ExecuteNonQuery();
                }
            }

            var testShader1DbPath = Path.Combine(outputFolder, "TestShader1.mdf");
            if (!File.Exists(testShader1DbPath))
            {
                var connectionString =
                    "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = string.Format(@"exec sp_detach_db '{0}';", "TestShader1");
                    cmd.CommandText += string.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')",
                        "TestShader1", testShader1DbPath);
                    cmd.ExecuteNonQuery();
                }
            }

            var testShader2DbPath = Path.Combine(outputFolder, "TestShader2.mdf");
            if (!File.Exists(testShader2DbPath))
            {
                var connectionString =
                    "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = string.Format(@"exec sp_detach_db '{0}';", "TestShader2");
                    cmd.CommandText += string.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')",
                        "TestShader2", testShader2DbPath);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}