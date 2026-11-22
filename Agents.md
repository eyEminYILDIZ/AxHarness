NEVER-EVER DONT READ the Microsoft Dotnet Secrets.

## .NET Conventions

- Put `using` directives before the namespace; use file-scoped namespaces (`namespace X;`).
- No one-line statements: always use braces and put the body on its own lines (`if`, `else`, `for`, `foreach`, `while`, `using`).
- Expression-bodied members are fine for simple members (e.g. `=> x`).
- Classes are `sealed` by default; one top-level type per file, file named after the type.
- Use `async`/`await` with `Async` suffix and pass `CancellationToken` through; never `.Result`/`.Wait()`.
- Prefer `var` when the type is obvious; use `is`/`is not` pattern matching over casts.
- Nullable reference types stay enabled; do not suppress with `!` unless unavoidable.
- Private fields are `_camelCase`; types, methods and properties are `PascalCase`.
- Throw specific exceptions (e.g. `ToolException`); never swallow exceptions silently.
- Cross-platform (Linux, macOS, Windows): no OS-specific APIs outside `Tools/ShellResolver`; guard any unavoidable difference with `OperatingSystem.Is*`. Never hard-code `/` separators, `/bin/bash` or case-sensitive path comparison (use `PathGuard.Comparison`).

## Project Docs

- For project overview, usage and architecture, see [readme.md](readme.md).

## Tests

- xUnit in `tests/AxHarness.Tests`; test names use `Snake_case_describing_behavior`.
- Use `TempRoot` for filesystem tests; never touch real user paths.
- Run `dotnet build` and `dotnet test` before finishing a change.
