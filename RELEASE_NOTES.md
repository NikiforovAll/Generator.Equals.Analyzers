# Release Notes

## Version 1.0.4 - 2025-09-17

### 🔧 Bug Fixes
- **Removed Dictionary and HashSet support** - These collection types are not actually supported by the Generator.Equals library
- **Removed DictionaryEquality and SetEquality attributes** - These attributes don't exist in the Generator.Equals library

### ✅ Supported Collection Types
- **Arrays**: `T[]`
- **Lists**: `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`
- **Collections**: `Collection<T>`, `ObservableCollection<T>`

### ✅ Supported Equality Attributes
- `IgnoreEquality`, `DefaultEquality`, `SequenceEquality`
- `ReferenceEquality`, `OrderedEquality`, `UnorderedEquality`

---

## Version 1.0.3 - 2025-09-17

### 🎉 Initial Release
- **GE001 Diagnostic**: Detects collection properties in `[Equatable]` classes missing equality attributes
- **Basic Code Fix Provider**: Provides code fixes to add equality attributes