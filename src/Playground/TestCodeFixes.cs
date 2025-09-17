using System.Collections.Generic;
using Generator.Equals;

namespace Playground;

// =============================================================================
// GE002 Test Cases - Complex Object Properties
// =============================================================================

/// <summary>
/// This class demonstrates GE002 diagnostics for complex object properties
/// that lack [Equatable] attribute on their type.
/// </summary>
[Equatable]
public partial class OrderWithComplexObjects
{
    // Should trigger GE002: Customer type lacks [Equatable] attribute
    public Customer Customer { get; set; } = new();

    // Should trigger GE002: Address type lacks [Equatable] attribute
    public Address ShippingAddress { get; set; } = new();

    // Should NOT trigger GE002: primitive types are fine
    public string OrderId { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// This class demonstrates the proper way - both container and contained types
/// have [Equatable] attribute.
/// </summary>
[Equatable]
public partial class OrderWithEquatableObjects
{
    // Should NOT trigger GE002: EquatableCustomer has [Equatable] attribute
    public EquatableCustomer Customer { get; set; } = new();

    // Should NOT trigger GE002: EquatableAddress has [Equatable] attribute
    public EquatableAddress ShippingAddress { get; set; } = new();

    public string OrderId { get; set; } = "";
    public decimal Total { get; set; }
}

// =============================================================================
// GE003 Test Cases - Collection Element Types
// =============================================================================

/// <summary>
/// This class demonstrates GE003 diagnostics for collection properties
/// where the element type lacks [Equatable] attribute.
/// </summary>
[Equatable]
public partial class StoreWithComplexCollections
{
    // Should trigger GE003: Product element type lacks [Equatable] attribute
    [OrderedEquality]
    public List<Product> Products { get; set; } = [];

    // Should trigger GE003: Customer element type lacks [Equatable] attribute
    [UnorderedEquality]
    public Customer[] Customers { get; set; } = [];

    // Should trigger GE003: Employee element type lacks [Equatable] attribute
    [OrderedEquality]
    public List<Employee> Staff { get; set; } = [];

    // Should NOT trigger GE003: primitive collections are fine
    [UnorderedEquality]
    public int[] ProductIds { get; set; } = [];
}

/// <summary>
/// This class demonstrates the proper way - collection element types
/// have [Equatable] attribute.
/// </summary>
[Equatable]
public partial class StoreWithEquatableCollections
{
    // Should NOT trigger GE003: EquatableProduct has [Equatable] attribute
    [OrderedEquality]
    public List<EquatableProduct> Products { get; set; } = [];

    // Should NOT trigger GE003: EquatableCustomer has [Equatable] attribute
    [UnorderedEquality]
    public EquatableCustomer[] Customers { get; set; } = [];

    // Should NOT trigger GE003: EquatableEmployee has [Equatable] attribute
    [OrderedEquality]
    public List<EquatableEmployee> Staff { get; set; } = [];
}

// =============================================================================
// Complex Object Types WITHOUT [Equatable] - These cause GE002/GE003
// =============================================================================

public class Customer
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public Address Address { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class Product
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
}

public class Employee
{
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Salary { get; set; }
}

// =============================================================================
// Complex Object Types WITH [Equatable] - These prevent GE002/GE003
// =============================================================================

[Equatable]
public partial class EquatableCustomer
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public EquatableAddress Address { get; set; } = new();
}

[Equatable]
public partial class EquatableAddress
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

[Equatable]
public partial class EquatableProduct
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Category { get; set; } = "";
}

[Equatable]
public partial class EquatableEmployee
{
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Salary { get; set; }
}

// =============================================================================
// Mixed Scenarios - Some properties/collections trigger, others don't
// =============================================================================

/// <summary>
/// This class demonstrates mixed scenarios with both problematic and correct usage.
/// </summary>
[Equatable]
public partial class MixedScenarioClass
{
    // GE002: Should trigger - Customer lacks [Equatable]
    public Customer ProblemCustomer { get; set; } = new();

    // NO diagnostic: EquatableCustomer has [Equatable]
    public EquatableCustomer GoodCustomer { get; set; } = new();

    // GE003: Should trigger - Product element type lacks [Equatable]
    [OrderedEquality]
    public List<Product> ProblemProducts { get; set; } = [];

    // NO diagnostic: EquatableProduct has [Equatable]
    [UnorderedEquality]
    public EquatableProduct[] GoodProducts { get; set; } = [];

    // NO diagnostic: primitives are fine
    public string Name { get; set; } = "";
    public int Id { get; set; }

    // NO diagnostic: primitive collections are fine
    [UnorderedEquality]
    public string[] Tags { get; set; } = [];
}

// =============================================================================
// Edge Cases and Complex Nested Scenarios
// =============================================================================

/// <summary>
/// Demonstrates nested complex objects and collections.
/// </summary>
[Equatable]
public partial class NestedComplexScenario
{
    // GE002: Should trigger - Order lacks [Equatable] (which itself has complex properties)
    public Order NestedOrder { get; set; } = new();

    // GE003: Should trigger - Order element type lacks [Equatable]
    [OrderedEquality]
    public List<Order> OrderHistory { get; set; } = [];

    // GE003: Should trigger for nested collections - Order element type lacks [Equatable]
    [UnorderedEquality]
    public List<List<Order>> GroupedOrders { get; set; } = [];
}

/// <summary>
/// A complex order type that itself contains complex objects.
/// </summary>
public class Order
{
    public Customer Customer { get; set; } = new();
    public List<Product> Items { get; set; } = [];
    public Address ShippingAddress { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
}
