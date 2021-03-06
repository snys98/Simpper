using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Simpper.NetFramework.Test
{
    [TestClass]
    public class SqlServerSqlGeneratorTests : IDisposable
    {
        private MockRepository mockRepository;

        public SqlServerSqlGeneratorTests()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);
        }

        public void Dispose()
        {
            this.mockRepository.VerifyAll();
        }

        private SqlServerSqlGenerator<TestEntity> CreateSqlServerSqlGenerator()
        {
            return new SqlServerSqlGenerator<TestEntity>();
        }

        [TestMethod]
        public void insert_should_success()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            TestEntity entity = new TestEntity()
            {
                IntField = 1,
                StringField = "1"
            };

            // Act
            unitUnderTest.Insert(
                entity).SqlBuilder.ToString().Should().NotContain("ExtraReadOnly")
                .And.NotContain("ExtraNotMapped")
                .And.NotContain("Id");
        }

        [TestMethod]
        public void bulk_insert_should_generate_correct_sql()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var entities = new List<TestEntity>
            {
                new TestEntity()
                {
                    IntField = 1, StringField = "1", DateTimeField = DateTime.Parse("3333-03-01"), DecimalField = 1M,
                    EnumField = TestEnum.One, GuidField = Guid.Empty, LongField = 1
                },
                new TestEntity()
                {
                    IntField = 1, StringField = "1", DateTimeField = DateTime.Parse("3333-03-01"), DecimalField = 1M,
                    EnumField = TestEnum.One, GuidField = Guid.Empty, LongField = 1
                },
            };

            // Act
            unitUnderTest.BulkInsert(entities).ToString().Should().Contain(@"INSERT INTO TestEntity (StringField,IntField,DateTimeField,NullableDateTimeField,EnumField,DecimalField,GuidField,LongField)
VALUES
(@StringField0,@IntField0,@DateTimeField0,@NullableDateTimeField0,@EnumField0,@DecimalField0,@GuidField0,@LongField0),
(@StringField1,@IntField1,@DateTimeField1,@NullableDateTimeField1,@EnumField1,@DecimalField1,@GuidField1,@LongField1)".Trim());
        }

        [TestMethod]
        public void update_should_work_with_where()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            TestEntity entity = new TestEntity()
            {
                IntField = 1,
                StringField = "1"

            };

            // Act
            unitUnderTest.Update(x => x.IntField == 1, new
            {
                entity.StringField
            }).SqlBuilder.ToString().Should().Contain(new StringBuilder()
                .AppendLine("UPDATE TestEntity")
                .AppendLine("SET StringField = @StringField")
                .AppendLine("WHERE")
                .AppendLine("IntField = @IntField0")
                .ToString().Trim());
        }

        [TestMethod]
        public void where_should_work_with_contains()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("StringField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.StringField.Contains("20"));

            result.SqlBuilder.ToString().Should().Contain(fieldName + " LIKE '%20%'");
        }

        [TestMethod]
        public void where_contains_should_work_with_primitive_types()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();

            // Act
            var result = unitUnderTest.Where((x) => x.StringField.In(new []{"10","20"}));

            result.SqlBuilder.ToString().Should().Contain("StringField IN (@StringField0,@StringField1)");
            result.SqlParams["StringField0"].Should().Be("10");
            result.SqlParams["StringField1"].Should().Be("20");

            result = unitUnderTest.Where((x) => x.IntField.In(new[] { 10, 20 }));

            result.SqlBuilder.ToString().Should().Contain("IntField IN (@IntField0,@IntField1)");
            result.SqlParams["IntField0"].Should().Be(10);
            result.SqlParams["IntField1"].Should().Be(20);

            result = unitUnderTest.Where((x) => x.DateTimeField.In(new[] { DateTime.Today.ToUnixEpoch(), DateTime.Today.AddDays(1).ToUnixEpoch() }));

            result.SqlBuilder.ToString().Should().Contain("DateTimeField IN (@DateTimeField0,@DateTimeField1)");
            result.SqlParams["DateTimeField0"].Should().Be(DateTime.Today.ToUnixEpoch());
            result.SqlParams["DateTimeField1"].Should().Be(DateTime.Today.AddDays(1).ToUnixEpoch());

            result = unitUnderTest.Where((x) => x.NullableDateTimeField.In(new[] { DateTime.Today.ToUnixEpoch(), (DateTime?)null }));

            result.SqlBuilder.ToString().Should().Contain("NullableDateTimeField IN (@NullableDateTimeField0,@NullableDateTimeField1)");
            result.SqlParams["NullableDateTimeField0"].Should().Be(DateTime.Today.ToUnixEpoch());
            result.SqlParams["NullableDateTimeField1"].Should().Be(null);

            result = unitUnderTest.Where((x) => x.DecimalField.In(new[] { 1.1M, -3.3333333333333333M }.ToList()));

            result.SqlBuilder.ToString().Should().Contain("DecimalField IN (@DecimalField0,@DecimalField1)");
            result.SqlParams["DecimalField0"].Should().Be(1.1M);
            result.SqlParams["DecimalField1"].Should().Be(-3.3333333333333333M);
        }

        [TestMethod]
        public void where_should_work_with_not_contains()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("StringField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => !x.StringField.Contains("20"));

            result.SqlBuilder.ToString().Should().Contain("NOT "+ fieldName + " LIKE '%20%'");
        }

        [TestMethod]
        public void where_should_work_with_end_with()
        {
            var entity = new TestEntity() { StringField = "200" };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("StringField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.StringField.EndsWith("20"));

            result.SqlBuilder.ToString().Should().Contain(fieldName + " LIKE '%20'");
        }

        [TestMethod]
        public void where_should_work_with_start_with()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("StringField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.StringField.StartsWith("20"));

            result.SqlBuilder.ToString().Should().Contain(fieldName + " LIKE '20%'");
        }

        [TestMethod]
        public void where_should_work_with_simple_eval_value()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField > 1 + 1);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " > @" + fieldName);
            result.SqlParams["IntField0"].Should().Be(2);
        }

        [TestMethod]
        public void where_should_work_with_multiple_where()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField > 1 + 1).Where(x => x.IntField < 0);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " > @" + fieldName);
            result.SqlParams["IntField0"].Should().Be(2);
        }

        [TestMethod]
        public void where_should_work_with_and_condition()
        {
            var entity = new TestEntity() { IntField = 3 };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField > 1 && x.IntField < 4);

            result.SqlBuilder.ToString().Should().Contain(new StringBuilder()
                .AppendLine("WHERE")
                .AppendLine("( IntField > @IntField0 AND IntField < @IntField1 )")
                .ToString().Trim());
        }

        [TestMethod]
        public void where_should_work_with_nullable()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();

            // Act
            var result = unitUnderTest.Where((x) => x.NullableDateTimeField != null);

            result.SqlBuilder.ToString().Should().Contain(new StringBuilder()
                .AppendLine("WHERE")
                .AppendLine("NullableDateTimeField IS NOT NULL")
                .ToString().Trim());
        }

        [TestMethod]
        public void where_should_work_with_or_condition()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField < 2 || x.IntField > 4);

            result.SqlBuilder.ToString().Should().Contain(new StringBuilder()
                .AppendLine("WHERE")
                .AppendLine("( IntField < @IntField0 OR IntField > @IntField1 )")
                .ToString().Trim());
        }

        [TestMethod]
        public void where_should_work_with_complex_eval_value()
        {
            var entity = new TestEntity() { IntField = 7 };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField >= (int)DateTime.Now.DayOfWeek + 1);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " >= @" + fieldName);
            result.SqlParams["IntField0"].Should().Be((int)DateTime.Now.DayOfWeek + 1);
        }

        [TestMethod]
        public void where_should_work_with_complex_condition()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField >= (int)DateTime.Now.DayOfWeek + 1 && x.IntField < 30 || x.LongField < 0);

            result.SqlBuilder.ToString().Should().Contain(@"WHERE
( ( IntField >= @IntField0 AND IntField < @IntField1 ) OR LongField < @LongField0 )
");
            result.SqlParams["IntField0"].Should().Be((int)DateTime.Now.DayOfWeek + 1);
            result.SqlParams["IntField1"].Should().Be(30);
            result.SqlParams["LongField0"].Should().Be(0);
        }

        [TestMethod]
        public void where_should_work_with_int()
        {
            var entity = new TestEntity() { IntField = 20 };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var propInfo = typeof(TestEntity).GetProperty("IntField");
            var fieldName = propInfo.GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField > 30);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " > @" + fieldName);

            result = unitUnderTest.Where((x) => x.IntField >= 30);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " >= @" + fieldName);

            result = unitUnderTest.Where((x) => x.IntField == 30);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " = @" + fieldName);

            result = unitUnderTest.Where((x) => x.IntField <= 30);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " <= @" + fieldName);

            result = unitUnderTest.Where((x) => x.IntField < 30);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " < @" + fieldName);
        }
    }
}
