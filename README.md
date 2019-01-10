# Simpper<!-- omit in toc -->

This is a lightweight orm mapping and sql query excuter.

Any kind of support is welcomed.

# Status: [![Build Status](https://travis-ci.com/snys98/Simpper.svg?branch=VS2013_compatible)](https://travis-ci.com/snys98/Simpper)<!-- omit in toc -->

- [Installation](#installation)
- [Usage](#usage)
  - [Sample entity](#sample-entity)
  - [Insert](#insert)
  - [BulkInsert](#bulkinsert)
  - [Update](#update)
  - [Query](#query)
    - [SingleQuery](#singlequery)
    - [CountQuery](#countquery)
    - [PagedQuery](#pagedquery)
  - [Delete](#delete)
- [Advanced usage:](#advanced-usage)
  - [Sharding:](#sharding)

# Installation

Todo: pack as nuget package

# Usage

## Sample entity

```csharp
[Table("TestEntity")]
public class TestEntity
{
    public TestEntity()
    {
        DateTimeField = DateTime.Now;
    }

    [Key]
    [Identity]
    public int Id { get; set; }

    [Column("StringField")]
    public string StringField { get; set; }
    [Column("IntField")]
    public int IntField { get; set; }
    [Column("DateTimeField")]
    public DateTime DateTimeField { get; set; }
    [Column("NullableDateTimeField")]
    public DateTime? NullableDateTimeField { get; set; }
    [Column("EnumField")]
    public TestEnum EnumField { get; set; }
    [Column("DecimalField")]
    public decimal DecimalField { get; set; }
    [Column("GuidField")]
    public Guid GuidField { get; set; }
    [Column("LongField")]
    public long LongField { get; set; }
    public string ExtraReadOnly {
        get { return this.StringField + this.IntField; }
    }
    [NotMapped]
    public string ExtraNotMapped { get; set; }
    [NotMapped]
    public string ExtraNotMappedWithBackingField { get; set; }
}

public enum TestEnum
{
    Default,
    One
}
```

## Insert

```cs
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
    var affectRowCount = ormContext.Insert(entity);
}
```

## BulkInsert

```cs
using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
{
    var list = new List<TestEntity>();
    for (var i = 0; i < 100; i++)
    {
        list.Add(new TestEntity
        {
            IntField = i,
            StringField = i.ToString()
        });
    }
    var affectRowCount = ormContext.BulkInsert(list);
}
```

## Update

```cs
using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
{
    var affectRowCount = ormContext.Update<TestEntity>(
        x => x.GuidField == Guid.Parse("025017ca-7259-4604-8760-85b4b343270a"),
        new
        {
            LongField = entity.LongField + 3,
            IntField = entity.IntField + 4
        });
}
```

## Query

**Important:**

- **In all conditional query, the entity field should be in the left side.**
- **All functions that do with entity field(excluding these introduced bellow) is not supported**

**Note:**
- To select all, just pass `x=>true` (or optional params will be available in the near future).
- Support `Contains`, `StartWith` and `EndsWith` for strings.

    `x.StringField.Contains("xx")` to generate `[StringField] LIKE '%xx%'`.

- Support sql `in`

    ```cs
    var result = ormContext.QueryFirst<TestEntity>(x => x.IntField.In(new[] {2, 4, 7}));
    ```

- Support nested conditions

    ```cs
    var result = ormContext.QueryFirst<TestEntity>(
                    x => x.IntField == 30 || x.LongField == 68 || x.StringField == "32" || x.StringField == "33" && x.IntField == 33 || !(x.IntField < 49))
    ```

### SingleQuery

```cs
using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
{
    var entity = ormContext.QueryFirst<TestEntity>(x => x.IntField == 3);
}
```

### CountQuery

```cs
using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
{
    var result = ormContext.Count<TestEntity>(x => x.IntField < 28);
}
```

### PagedQuery

```cs
using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
{
    var list = ormContext.QueryPage<TestEntity>(
        x => x.IntField >= 0,
        sort: x => x.StringField,
        asc: false,
        pageIndex: 0,
        pageSize: 10);
}
```

## Delete

```cs
using (var ormContext = new SqlConnection(_connStr).ToOrmContext())
{
    var result = ormContext.Delete<TestEntity>(x => x.IntField < 28);
}
```

# Advanced usage:

## Sharding:
Please try look at the test project