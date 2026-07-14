# JohnSharp

JohnSharp is a collection of reusable .NET libraries built and refined through real-world backend development.

Rather than being created as a showcase project, this repository has evolved organically while solving practical problems encountered during day-to-day development. Since 2024, the library has continuously grown alongside production services, with new features and improvements added whenever repetitive patterns or common infrastructure needs emerged.

## Philosophy

The goal of JohnSharp is simple.

* Reduce repetitive boilerplate.
* Improve consistency across projects.
* Build practical components that can be reused in multiple applications.
* Prefer maintainability and developer productivity over unnecessary complexity.

This is not intended to replace mature frameworks such as Entity Framework Core or ASP.NET Core. Instead, it provides additional building blocks that make application development faster and more consistent.

## Current Modules

The repository currently contains libraries for:

* Entity Framework query building
* Dynamic filtering and sorting
* Generic pagination
* Transaction execution helpers
* Caching
* Mail utilities
* Message Queue integration
* Encryption utilities
* LLM integrations
* MCP integrations
* Shared models and common extensions

Each module is designed to be independent where possible while sharing common abstractions through the core libraries.

## Example

```csharp
await queryExecutor.ToResponseListAutoMappingAsync<Entity, Response>(
    query,
    request);
```

Dynamic filtering, sorting and pagination are handled automatically through metadata.

## Why this project?

After more than ten years of backend development, I found myself repeatedly solving the same infrastructure problems across different projects.

Instead of copying code from project to project, I gradually extracted reusable components into a shared library. What started as a small collection of helpers has evolved into a modular framework used across my personal and production projects.

The codebase is continuously refactored as new requirements arise, and many APIs have changed multiple times based on practical experience rather than theoretical design.

## Status

This project is actively maintained.

While there are still many areas that can be improved, every module included here has been created to solve an actual development problem.

Feedback, suggestions and pull requests are always welcome.

## License

MIT License
