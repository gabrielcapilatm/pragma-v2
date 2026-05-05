# C# Coding Conventions — financial-api

Aplica a todos os arquivos `.cs` do projeto. Baseado nas [.NET C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) e no `.editorconfig` deste repositório.

## Naming

| Identificador | Convenção | Exemplo |
|---|---|---|
| Interface | `I` + PascalCase | `IApplicationDbContext` |
| Tipo genérico | `T` + PascalCase | `TEntity`, `TResult` |
| Classe / struct / enum / record | PascalCase | `TenantResolver` |
| Método público | PascalCase | `GetByIdAsync` |
| Propriedade | PascalCase | `TenantId` |
| Evento | PascalCase | `DataProcessed` |
| Constante | PascalCase | `MaxRetryCount` |
| Campo privado de instância | `_camelCase` | `_dbContext` |
| Campo privado estático | `s_camelCase` | `s_instance` |
| Parâmetro / variável local | camelCase | `tenantId` |
| Método assíncrono | sufixo `Async` | `FindByIdAsync` |

## Style

- **`var`**: usar quando o tipo é evidente pelo lado direito (`new`, cast, literal). Não usar com tipos primitivos (`int`, `string`, `bool`). Usar em loops `for`. Não usar em `foreach`.
- **Namespaces**: sempre file-scoped (`namespace FinancialApi.Api;`).
- **`using` directives**: sempre fora do namespace.
- **Chaves**: obrigatórias em todos os blocos `if`, `for`, `foreach`, `while`, `using`.
- **Modificadores de acesso**: sempre explícitos — nunca omitir `private`, `public`, etc.
- **Ordem dos modificadores**: `public protected internal private new abstract virtual override sealed static readonly extern unsafe volatile async`.
- **Tipos primitivos**: usar `string`, `int`, `bool` — nunca `String`, `Int32`, `Boolean`.
- **Null checks**: preferir `x is null` e `x is not null` a `x == null`.
- **Pattern matching**: preferir `is`, `switch expression`, `not` pattern a casts com `as`.
- **`new()`**: usar target-typed `new()` quando o tipo é evidente.
- **Collection expressions**: usar `[...]` em vez de `new List<T> { }` ou `Array.Empty<T>()`.
- **String interpolation**: usar `$"..."` para concatenações curtas. Para loops com muitas concatenações, usar `StringBuilder`.
- **Raw string literals**: preferir `"""..."""` a verbatim strings ou escape sequences.
- **`using` statement**: usar forma sem chaves (`using var x = ...;`) quando possível.
- **Async/await**: usar em todas as operações I/O. Aplicar `ConfigureAwait(false)` em bibliotecas.
- **Expression-bodied members**: usar em propriedades e métodos de uma linha. Não usar em construtores.
- **Primary constructors**: preferir em classes e structs simples onde aplicável.
