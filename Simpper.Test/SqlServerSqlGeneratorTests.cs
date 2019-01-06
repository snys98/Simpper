using Moq;
using Simpper;
using System;
using System.Linq.Expressions;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Simpper.Test
{
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

        //[Fact]
        //public void Select_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();
        //    Expression<Func<T, bool>> predicate = TODO;
        //    Expression<Func<T, object>> sort = TODO;

        //    // Act
        //    var result = unitUnderTest.Select(
        //        predicate,
        //        sort);

        //    // Assert
        //    Assert.True(false);
        //}

        //[Fact]
        //public void GetSelectClause_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();

        //    // Act
        //    var result = unitUnderTest.GetSelectClause();

        //    // Assert
        //    Assert.True(false);
        //}

        //[Fact]
        //public void GetTableIndicatorClause_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();

        //    // Act
        //    var result = unitUnderTest.GetTableIndicatorClause();

        //    // Assert
        //    Assert.True(false);
        //}

        //[Fact]
        //public void Count_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();
        //    Expression<Func<T, bool>> predicate = TODO;

        //    // Act
        //    var result = unitUnderTest.Count(
        //        predicate);

        //    // Assert
        //    Assert.True(false);
        //}

        [Fact]
        public void Insert_StateUnderTest_ExpectedBehavior()
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

        [Fact]
        public void Update_StateUnderTest_ExpectedBehavior()
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
                .AppendLine("SET (StringField)")
                .AppendLine("VALUES (@StringField)")
                .AppendLine("WHERE 1 = 1")
                .Append("AND IntField = @IntField")
                .ToString().Trim());
        }

        //[Fact]
        //public void Delete_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();
        //    Expression<Func<T, bool>> predicate = TODO;

        //    // Act
        //    var result = unitUnderTest.Delete(
        //        predicate);

        //    // Assert
        //    Assert.True(false);
        //}

        //[Fact]
        //public void IdentitySql_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();
        //    EntityMappingCache<T> entityConfiguration = TODO;

        //    // Act
        //    var result = unitUnderTest.IdentitySql(
        //        entityConfiguration);

        //    // Assert
        //    Assert.True(false);
        //}

        //[Fact]
        //public void GetTableName_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();
        //    EntityMappingCache<T> entityConfiguration = TODO;

        //    // Act
        //    var result = unitUnderTest.GetTableName(
        //        entityConfiguration);

        //    // Assert
        //    Assert.True(false);
        //}

        //[Fact]
        //public void GetColumnName_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var unitUnderTest = this.CreateSqlServerSqlGenerator();
        //    string propertyName = TODO;
        //    bool includeAlias = TODO;

        //    // Act
        //    var result = unitUnderTest.GetColumnName(
        //        propertyName,
        //        includeAlias);

        //    // Assert
        //    Assert.True(false);
        //}

        [Fact]
        public void BuildWhereClause_should_work_with_contains()
        {
            var entity = new TestEntity() { StringField = "200" };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("StringField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.StringField.Contains("20"));

            result.SqlBuilder.ToString().Should().Contain(fieldName + " LIKE %20%");
        }

        [Fact]
        public void BuildWhereClause_should_work_with_end_with()
        {
            var entity = new TestEntity() { StringField = "200" };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("StringField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.StringField.EndsWith("20"));

            result.SqlBuilder.ToString().Should().Contain(fieldName + " LIKE %20");
        }

        [Fact]
        public void BuildWhereClause_should_work_with_start_with()
        {
            var entity = new TestEntity() { StringField = "200" };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("StringField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.StringField.StartsWith("20"));

            result.SqlBuilder.ToString().Should().Contain(fieldName + " LIKE 20%");
        }

        [Fact]
        public void BuildWhereClause_should_work_with_simple_eval_value()
        {
            var entity = new TestEntity() { IntField = 3 };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField > 1 + 1);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " > @" + fieldName);
            result.SqlParams["IntField"].Should().Be(2);
        }

        [Fact]
        public void BuildWhereClause_should_work_with_and_condition()
        {
            var entity = new TestEntity() { IntField = 3 };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField > 1 && x.IntField < 4);

            result.SqlBuilder.ToString().Should().Contain(new StringBuilder()
                .AppendLine("WHERE 1 = 1")
                .AppendLine("AND ( IntField > @IntField AND IntField < @IntField0 )")
                .ToString().Trim());
        }

        [Fact]
        public void BuildWhereClause_should_work_with_or_condition()
        {
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField < 2 || x.IntField > 4);

            result.SqlBuilder.ToString().Should().Contain(new StringBuilder()
                .AppendLine("WHERE 1 = 1")
                .AppendLine("AND ( IntField < @IntField OR IntField > @IntField0 )")
                .ToString().Trim());
        }

        [Fact]
        public void BuildWhereClause_should_work_with_complex_eval_value()
        {
            var entity = new TestEntity() { IntField = 7 };
            // Arrange
            var unitUnderTest = this.CreateSqlServerSqlGenerator();
            var fieldName = typeof(TestEntity).GetProperty("IntField").GetReflectedColumnName();

            // Act
            var result = unitUnderTest.Where((x) => x.IntField >= (int)DateTime.Now.DayOfWeek + 1);

            result.SqlBuilder.ToString().Should().Contain(fieldName + " >= @" + fieldName);
            result.SqlParams["IntField"].Should().Be((int)DateTime.Now.DayOfWeek + 1);
        }

        [Fact]
        public void BuildWhereClause_should_work_with_int()
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
