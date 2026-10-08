# ZooFinder Code Style

These rules are mandatory when writing or modifying project code. Consult this document before working with code.

## Line Length and Wrapping

- The maximum code line length is 130 characters, including indentation.
- Keep a construct on one line when it fits within this limit. Do not add arbitrary line breaks.
- Exceptions are interface/contract parameters and LINQ method calls: always wrap them according to the rules below.
- Wrap long expressions and call chains at meaningful boundaries while preserving readability.
- Preserve the original value of string literals; wrapping must not change their text or URLs.

## LINQ Methods

Place every LINQ method call on a new line, starting with the dot, including the first and terminal methods in a chain.
This applies to a single call and to chains that would fit entirely within 130 characters.
Format asynchronous EF Core query-execution methods the same way.

```csharp
var animalNames = animals
    .Where(animal => animal.IsPublic)
    .OrderBy(animal => animal.Name)
    .Select(animal => animal.Name)
    .ToList();

var hasAnimals = animals
    .Any();
```

## Braces

Use braces for every `if` branch, including `else` and `else if`, even when the body contains only one statement.
Place a block's opening and closing braces on separate lines.

```csharp
if (request.Cursor.Length > 1024)
{
    throw new FormatException();
}
```

Methods, constructors, and local functions must have block bodies. Do not use expression bodies (`=>`) for them.

```csharp
public void Publish()
{
    IsPublic = true;
}
```

Properties and their accessors may use `=>`.

```csharp
public bool IsPublic => _isPublic;
```

Interface methods without an implementation remain declarations ending in `;`; they do not need a body.

## Interface and Contract Parameters

Place every parameter of an interface method declaration on its own line, regardless of the declaration's total length.
The first parameter starts on the line after the opening parenthesis.

```csharp
public interface IAnimalCatalogService
{
    Task<AnimalResponse> GetAsync(
        Guid animalId,
        CancellationToken cancellationToken);
}
```

Apply the same rule to positional contract declarations (`record` / `record class`).
This also applies to declarations with a single parameter.

```csharp
public sealed record AnimalRegistrationResult(
    Guid AnimalId,
    Guid GeneralRoomId);
```

## Class Method Parameters

Keep a class method declaration on one line if it fits within 130 characters, including indentation.

```csharp
public static string Encode(string scope, DateTime createdAtUtc, Guid id)
{
    // Method body.
}
```

If it does not fit, wrap the entire parameter list: each parameter goes on its own line,
starting after the opening parenthesis. Do not leave some parameters next to the method name
or group multiple parameters on one line. A wrapped declaration has this form:

```csharp
public static string Encode(
    string scope,
    DateTime createdAtUtc,
    Guid id)
{
    // Method body.
}
```

Apply this principle to constructors and local functions as well.

## Blank Lines After Blocks

Leave a blank line after a closing brace `}` when other code follows.
No blank line is needed if the next line is the closing brace of the enclosing block.
No additional blank line is required after the last block at the end of a file.
This rule concerns closing braces; do not add a blank line immediately after an opening brace.

```csharp
if (request.Cursor.Length > 1024)
{
    throw new FormatException();
}

var cursor = JsonSerializer.Deserialize<DatabaseCursor>(Convert.FromBase64String(request.Cursor));
```

Keep adjacent closing braces together without an intervening blank line:

```csharp
public void Validate(string cursor)
{
    if (cursor.Length > 1024)
    {
        throw new FormatException();
    }
}
```
