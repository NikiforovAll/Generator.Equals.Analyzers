# Nall.Generator.Equals.Analyzers

A Roslyn analyzer for the [Generator.Equals](https://github.com/diegofrata/Generator.Equals) library that ensures proper equality attribute usage on collection properties.

## Installation
```bash
dotnet add package Nall.Generator.Equals.Analyzers
```

## Features

### Diagnostics

- **GE001**: Detects collection properties in `[Equatable]` classes that lack required equality attributes
- **GE002**: Detects complex object properties in `[Equatable]` classes where the property type lacks `[Equatable]` attribute
- **GE003**: Detects collection properties with complex element types that lack `[Equatable]` attribute on the element type

### Code Fixes

- **Intelligent Attribute Suggestions**: Provides collection-specific attribute recommendations:
  - `List<T>` / `Array` / `IEnumerable<T>` → `[OrderedEquality]` or `[UnorderedEquality]`

## Example Usage

### GE001: Collection Properties Without Equality Attributes

```csharp
// Before
[Equatable]
public partial class MyClass
{
    public List<string> Items { get; set; } // ⚠️ GE001: Missing equality attribute
}

// After
[Equatable]
public partial class MyClass
{
    [UnorderedEquality] // or [OrderedEquality]
    public List<string> Items { get; set; }
}
```

### GE002: Complex Object Properties Without Equatable

```csharp
// Before
[Equatable]
public partial class Person
{
    public Address HomeAddress { get; set; } // ⚠️ GE002: Type 'Address' needs [Equatable]
}

public class Address { /* ... */ }

// After
[Equatable]
public partial class Person
{
    public Address HomeAddress { get; set; } // ✅ No warning
}

[Equatable]
public partial class Address { /* ... */ }
```

### GE003: Collection Element Types Without Equatable

```csharp
// Before
[Equatable]
public partial class CustomerList
{
    [OrderedEquality]
    public List<Customer> Customers { get; set; } // ⚠️ GE003: Element type 'Customer' needs [Equatable]
}

public class Customer { /* ... */ }

// After
[Equatable]
public partial class CustomerList
{
    [OrderedEquality]
    public List<Customer> Customers { get; set; } // ✅ No warning
}

[Equatable]
public partial class Customer { /* ... */ }
```

Example of code fix suggestions:
![](./assets/ge-code-fix.png)
