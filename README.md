# Generator.Equals.Analyzer

Roslyn analyzer for [Generator.Equals](https://github.com/diegofrata/Generator.Equals) that ensures proper equality attributes on collection properties.

## Features

- **Smart Diagnostics**: Detects missing equality attributes on collection properties in `[Equatable]` classes
- **Intelligent Code Fixes**: Collection-specific suggestions (DictionaryEquality, SetEquality, OrderedEquality, UnorderedEquality)
- **Public Properties Only**: Reduces false positives by focusing on public properties

## Installation

```xml
<PackageReference Include="Nall.Generator.Equals.Analyzers" Version="1.0.3">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

## Examples

### Dictionary Collections
```csharp
[Equatable]
public partial class UserProfile
{
    // ⚠️ GE001: Collection property 'Settings' requires an equality attribute
    public Dictionary<string, object> Settings { get; set; }

    // ✅ Fixed with code fix
    [DictionaryEquality]
    public Dictionary<string, object> Settings { get; set; }
}
```

### Set Collections
```csharp
[Equatable]
public partial class TaggedItem
{
    // ⚠️ GE001: Collection property 'Tags' requires an equality attribute
    public HashSet<string> Tags { get; set; }

    // ✅ Fixed with code fix
    [SetEquality]
    public HashSet<string> Tags { get; set; }
}
```

### List Collections
```csharp
[Equatable]
public partial class ShoppingCart
{
    // ⚠️ GE001: Collection property 'Items' requires an equality attribute
    public List<Product> Items { get; set; }

    // ✅ Two code fix options available:
    [OrderedEquality]     // Order matters
    public List<Product> Items { get; set; }

    [UnorderedEquality]   // Order doesn't matter
    public List<Product> Items { get; set; }
}
```

### Array Collections
```csharp
[Equatable]
public partial class Matrix
{
    // ⚠️ GE001: Collection property 'Values' requires an equality attribute
    public int[] Values { get; set; }

    // ✅ Fixed with code fix
    [OrderedEquality]
    public int[] Values { get; set; }
}
```

## Collection Types

| Type | Suggested Attribute |
|------|---------------------|
| `Dictionary<TKey,TValue>` | `[DictionaryEquality]` |
| `HashSet<T>`, `ISet<T>` | `[SetEquality]` |
| `List<T>`, `T[]`, `IEnumerable<T>` | `[OrderedEquality]` or `[UnorderedEquality]` |

## Development

```bash
# Build and test locally
./tools/install-local-package.sh
./tools/rebuild-playground.sh
```

## License

MIT