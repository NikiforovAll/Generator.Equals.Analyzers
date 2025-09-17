using Generator.Equals;

Console.WriteLine("=== Generator.Equals Analyzer Playground ===");
Console.WriteLine();

Console.WriteLine(
    "This playground demonstrates various scenarios where the GeneratorEquals.Analyzer"
);
Console.WriteLine(
    "will detect missing equality attributes on collection properties in [Equatable] classes."
);
Console.WriteLine();

// Create instances to demonstrate runtime behavior
var badExample1 = new BadExample_MissingAttributes
{
    StringList = ["hello", "world"],
    NumberArray = [1, 2, 3],
    DataDictionary = new() { ["key1"] = 100, ["key2"] = 200 },
    UniqueNumbers = [1, 2, 3, 4, 5],
};

var badExample2 = new BadExample_MissingAttributes
{
    StringList = ["hello", "world"],
    NumberArray = [1, 2, 3],
    DataDictionary = new() { ["key1"] = 100, ["key2"] = 200 },
    UniqueNumbers = [1, 2, 3, 4, 5],
};

Console.WriteLine("BAD EXAMPLES - These should trigger GE001 analyzer warnings:");
Console.WriteLine("============================================================");
Console.WriteLine();
Console.WriteLine("1. BadExample_MissingAttributes:");
Console.WriteLine("   - Contains List<string>, int[], Dictionary<string, int>, HashSet<int>");
Console.WriteLine("   - All collection properties lack equality attributes");
Console.WriteLine("   - Analyzer should show warnings for each collection property");
Console.WriteLine(
    $"   - Instance equality: {badExample1.Equals(badExample2)} (should be true but may fail)"
);
Console.WriteLine();

var goodExample1 = new GoodExample_PropertyAttributed
{
    OrderedItems = ["a", "b", "c"],
    NumberSet = [1, 2, 3],
    DataMap = new() { ["x"] = 10, ["y"] = 20 },
    IgnoredItems = ["ignored", "data"],
};

var goodExample2 = new GoodExample_PropertyAttributed
{
    OrderedItems = ["a", "b", "c"],
    NumberSet = [1, 2, 3],
    DataMap = new() { ["x"] = 10, ["y"] = 20 },
    IgnoredItems = ["different", "ignored", "data"],
};

Console.WriteLine("GOOD EXAMPLES - These should NOT trigger analyzer warnings:");
Console.WriteLine("=========================================================");
Console.WriteLine();
Console.WriteLine("2. GoodExample_PropertyAttributed:");
Console.WriteLine("   - All collection properties have appropriate equality attributes");
Console.WriteLine("   - OrderedEquality for List<string>");
Console.WriteLine("   - SetEquality for HashSet<int>");
Console.WriteLine("   - DefaultEquality for Dictionary<string, int>");
Console.WriteLine("   - IgnoreEquality for items that shouldn't affect equality");
Console.WriteLine(
    $"   - Instance equality: {goodExample1.Equals(goodExample2)} (true, IgnoredItems differences ignored)"
);
Console.WriteLine();

// Test inheritance scenario
var derived1 = new DerivedExample { Items = ["test"], AdditionalData = [1, 2, 3] };
var derived2 = new DerivedExample { Items = ["test"], AdditionalData = [1, 2, 3] };

Console.WriteLine("3. Inheritance Scenario:");
Console.WriteLine("   - BaseExample has [Equatable] attribute");
Console.WriteLine("   - DerivedExample inherits equatable behavior");
Console.WriteLine(
    "   - AdditionalData property should trigger analyzer warning (missing attribute)"
);
Console.WriteLine($"   - Instance equality: {derived1.Equals(derived2)}");
Console.WriteLine();

Console.WriteLine("EDGE CASES:");
Console.WriteLine("===========");
Console.WriteLine();
Console.WriteLine("4. NonEquatable class:");
Console.WriteLine("   - No [Equatable] attribute");
Console.WriteLine(
    "   - Should NOT trigger any analyzer warnings regardless of collection properties"
);
Console.WriteLine();

Console.WriteLine("5. NestedCollections:");
Console.WriteLine("   - Contains List<List<int>> property");
Console.WriteLine("   - Should trigger analyzer warning for missing equality attribute");
Console.WriteLine();

Console.WriteLine("To see the analyzer in action:");
Console.WriteLine("1. Open this project in Visual Studio or Visual Studio Code");
Console.WriteLine("2. Install the GeneratorEquals.Analyzer NuGet package");
Console.WriteLine("3. Look for GE001 warnings on collection properties in [Equatable] classes");
Console.WriteLine("4. Use code fixes to automatically add appropriate equality attributes");

// =============================================================================
// BAD EXAMPLES - These should trigger GE001 analyzer warnings
// =============================================================================

/// <summary>
/// This class should trigger GE001 warnings for all collection properties
/// because they lack explicit equality attributes.
/// </summary>
[Equatable]
public partial class BadExample_MissingAttributes
{
    // Should trigger GE001: List<string> property needs equality attribute
    public List<string> StringList { get; set; } = [];

    // Should trigger GE001: Array property needs equality attribute
    public int[] NumberArray { get; set; } = [];

    // Should trigger GE001: Dictionary property needs equality attribute
    public Dictionary<string, int> DataDictionary { get; set; } = [];

    // Should trigger GE001: HashSet property needs equality attribute
    public HashSet<int> UniqueNumbers { get; set; } = [];

    // Should NOT trigger warning: not a collection type
    public string RegularProperty { get; set; } = "";

    // Should NOT trigger warning: not a collection type
    public int NumberProperty { get; set; }
}

/// <summary>
/// Another bad example with different collection types
/// </summary>
[Equatable]
public partial class BadExample_MoreCollectionTypes
{
    // Should trigger GE001: IList<T> needs equality attribute
    public IList<string> InterfaceList { get; set; } = [];

    // Should trigger GE001: ICollection<T> needs equality attribute
    public ICollection<double> InterfaceCollection { get; set; } = [];

    // Should trigger GE001: IEnumerable<T> needs equality attribute
    public IEnumerable<DateTime> InterfaceEnumerable { get; set; } = [];
}

// =============================================================================
// GOOD EXAMPLES - These should NOT trigger analyzer warnings
// =============================================================================

/// <summary>
/// This class should NOT trigger any analyzer warnings because all
/// collection properties have appropriate equality attributes.
/// </summary>
[Equatable]
public partial class GoodExample_PropertyAttributed
{
    // Properly attributed with OrderedEquality for List<T>
    [OrderedEquality]
    public List<string> OrderedItems { get; set; } = [];

    // Properly attributed with SetEquality for HashSet<T>
    [SetEquality]
    public HashSet<int> NumberSet { get; set; } = [];

    // Properly attributed with DefaultEquality for Dictionary<TKey, TValue>
    [DefaultEquality]
    public Dictionary<string, int> DataMap { get; set; } = new();

    // Properly attributed with IgnoreEquality to exclude from equality
    [IgnoreEquality]
    public List<string> IgnoredItems { get; set; } = [];

    // Non-collection property (no attribute needed)
    public string Name { get; set; } = "";
}

/// <summary>
/// Demonstrates various equality attribute options
/// </summary>
[Equatable]
public partial class GoodExample_VariousAttributes
{
    [UnorderedEquality]
    public string[] UnorderedArray { get; set; } = [];

    [OrderedEquality]
    public List<int> SequenceList { get; set; } = [];

    [SetEquality]
    public HashSet<string> StringSet { get; set; } = [];

    [ReferenceEquality]
    public List<object> ReferenceList { get; set; } = [];
}

// =============================================================================
// INHERITANCE SCENARIOS
// =============================================================================

/// <summary>
/// Base class with [Equatable] attribute
/// </summary>
[Equatable]
public partial class BaseExample
{
    [OrderedEquality]
    public List<string> Items { get; set; } = [];
}

/// <summary>
/// Derived class should inherit equatable behavior.
/// The AdditionalData property should trigger GE001 warning.
/// </summary>
public partial class DerivedExample : BaseExample
{
    // Should trigger GE001: inherits [Equatable] but this property lacks equality attribute
    public List<int> AdditionalData { get; set; } = [];
}

// =============================================================================
// EDGE CASES AND NON-EQUATABLE EXAMPLES
// =============================================================================

/// <summary>
/// This class should NOT trigger any analyzer warnings because
/// it doesn't have the [Equatable] attribute.
/// </summary>
public class NonEquatable
{
    // Should NOT trigger warning: class is not [Equatable]
    public List<string> UnattributedList { get; set; } = [];
    public int[] UnattributedArray { get; set; } = [];
    public Dictionary<string, object> UnattributedDictionary { get; set; } = new();
}

/// <summary>
/// Nested collection scenario
/// </summary>
[Equatable]
public partial class NestedCollections
{
    // Should trigger GE001: nested collection needs equality attribute
    public List<List<int>> NestedLists { get; set; } = [];

    // Should trigger GE001: array of arrays needs equality attribute
    public string[][] JaggedArray { get; set; } = [];
}

/// <summary>
/// Example with only non-public properties (should not trigger warnings)
/// </summary>
[Equatable]
public partial class NonPublicPropertiesExample
{
    // Should NOT trigger warning: private property (analyzer only checks public properties)
    private List<string> PrivateList { get; set; } = [];

    // Should NOT trigger warning: protected property
    protected int[] ProtectedArray { get; set; } = [];

    // Should NOT trigger warning: internal property
    internal Dictionary<string, int> InternalDictionary { get; set; } = new();

    // This is public - should trigger GE001 warning
    public HashSet<int> PublicSet { get; set; } = [];
}
