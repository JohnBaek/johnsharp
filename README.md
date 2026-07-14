# JohnSharp

> A collection of reusable .NET libraries built from real-world backend development.

JohnSharp is a modular .NET library designed to reduce repetitive infrastructure code and improve developer productivity.

Unlike a showcase project, this repository has been continuously refined while developing production applications. Since 2024, every module has been added or refactored to solve actual problems encountered during backend development.

The primary goal is simple:

* Build once.
* Reuse everywhere.
* Keep application code clean.

---

# Why JohnSharp?

After more than 10 years of backend development, I realized I was solving the same problems repeatedly across different projects.

Instead of copying code between repositories, I gradually extracted reusable components into a shared library.

Today JohnSharp is used as a common library across my personal projects and production services.

The project continues to evolve whenever new requirements arise or existing APIs can be improved.

---

# Features

## Entity Framework

* Dynamic Query Builder
* Dynamic Sorting
* Dynamic Filtering
* Expression Tree based Query Generation
* Generic Pagination
* Generic Response Mapping
* Transaction Helper
* Attribute-based Search Metadata

## Infrastructure

* Cache
* Mail
* Message Queue
* Encryption
* Common Extensions
* Shared Models

## AI

* LLM Integration
* MCP Support

---

# Example

## Simple Query

```csharp
IQueryable<User> query = queryBuilder.BuildQuery<User>(request);

return await queryExecutor.ToResponseListAsync(
    query,
    request);
```

---

## Automatic DTO Mapping

```csharp
return await queryExecutor
    .ToResponseListAutoMappingAsync<UserEntity, ResponseUser>(
        query,
        request);
```

No manual pagination or mapping is required.

---

# Dynamic Search Metadata

Search behavior is declared directly on the DTO.

```csharp
public class ResponseUser
{
    public Guid Id { get; set; }

    [QueryMetaConvert(EnumQuerySearchType.Like)]
    public string Name { get; set; }

    [QueryMetaConvert(EnumQuerySearchType.NumericOrEnums)]
    public UserStatus Status { get; set; }

    [QueryMetaConvert(EnumQuerySearchType.RangeDate)]
    public DateTime RegDate { get; set; }
}
```

The QueryBuilder automatically reads the metadata and generates the corresponding Expression Tree.

No manual switch statements.

No handwritten LINQ.

No duplicated filtering logic.

---

# Pagination

```csharp
RequestQuery request = new()
{
    Skip = 0,
    PageCount = 20
};

ResponseList<ResponseUser> users =
    await queryExecutor
        .ToResponseListAutoMappingAsync<User, ResponseUser>(
            query,
            request);
```

---

# Transaction Helper

```csharp
await queryExecutor.ExecuteWithTransactionAutoCommitAsync(
    async db =>
    {
        db.Users.Add(user);

        await db.SaveChangesAsync();

        return Response.Success();
    });
```

---

# Design Goals

JohnSharp follows a few simple principles.

* Strong typing over magic strings.
* Generic APIs whenever possible.
* Practical abstractions.
* Performance-conscious implementations.
* Reusable across multiple services.
* Minimize boilerplate code.

---

# Project Structure

```
JohnSharp
│
├── JohnIsDev.Core.EntityFramework
├── JohnIsDev.Core.Cache
├── JohnIsDev.Core.Mail
├── JohnIsDev.Core.MessageQue
├── JohnIsDev.Core.LLM
├── JohnIsDev.Core.Mcp
├── JohnIsDev.Core.Extensions
└── JohnIsDev.Core.Models
```

---

# Philosophy

This project is not intended to compete with existing frameworks.

Entity Framework, ASP.NET Core and Microsoft's ecosystem already provide excellent foundations.

JohnSharp focuses on reducing repetitive application code while keeping business logic simple and expressive.

Most modules in this repository exist because they solved real production problems—not because they were created as demonstrations.

---

# Roadmap

* Improve unit test coverage
* Performance benchmarking
* Source Generator support
* Additional Entity Framework helpers
* NuGet package publishing
* Documentation improvements

---

# Contributing

Suggestions, issues and pull requests are always welcome.

If this library helps simplify your projects, feedback is greatly appreciated.

---

# License

MIT License
