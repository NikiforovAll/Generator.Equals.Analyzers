using System.Collections.Immutable;
using Basic.Reference.Assemblies;
using Generator.Equals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GeneratorEquals.Analyzer.Test;

public class EquatableCollectionAnalyzerTest
{
    private static readonly MetadataReference s_generatorEqualsReference =
        MetadataReference.CreateFromFile(typeof(EquatableAttribute).Assembly.Location);

    [Fact]
    public void Analyzer_HasCorrectDiagnosticDescriptors()
    {
        // Arrange
        var analyzer = new EquatableCollectionAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;

        // Assert
        Assert.Equal(3, supportedDiagnostics.Length);

        // Verify GE001 is present
        var ge001 = supportedDiagnostics.First(d => d.Id == "GE001");
        Assert.Equal(Resources.GE001Title, ge001.Title.ToString());
        Assert.Equal(Resources.GE001Category, ge001.Category);
        Assert.Equal(DiagnosticSeverity.Warning, ge001.DefaultSeverity);
        Assert.True(ge001.IsEnabledByDefault);

        // Verify GE002 is present
        var ge002 = supportedDiagnostics.First(d => d.Id == "GE002");
        Assert.Equal(
            "Complex object property type lacks Equatable attribute",
            ge002.Title.ToString()
        );
        Assert.Equal("Generator.Equals.Usage", ge002.Category);
        Assert.Equal(DiagnosticSeverity.Warning, ge002.DefaultSeverity);
        Assert.True(ge002.IsEnabledByDefault);

        // Verify GE003 is present
        var ge003 = supportedDiagnostics.First(d => d.Id == "GE003");
        Assert.Equal("Collection element type requires Equatable attribute", ge003.Title.ToString());
        Assert.Equal("Generator.Equals.Usage", ge003.Category);
        Assert.Equal(DiagnosticSeverity.Warning, ge003.DefaultSeverity);
        Assert.True(ge003.IsEnabledByDefault);
    }

    [Fact]
    public void GE001_StaticDescriptor_IsCorrectlyConfigured()
    {
        // Act
        var descriptor = EquatableCollectionAnalyzer.GE001;

        // Assert
        Assert.Equal("GE001", descriptor.Id);
        Assert.Equal(Resources.GE001Title, descriptor.Title.ToString());
        Assert.Equal(Resources.GE001MessageFormat, descriptor.MessageFormat.ToString());
        Assert.Equal(Resources.GE001Category, descriptor.Category);
        Assert.Equal(Resources.GE001Description, descriptor.Description.ToString());
        Assert.Equal(DiagnosticSeverity.Warning, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
    }

    [Fact]
    public void Analyzer_CanBeConstructed()
    {
        // Act & Assert
        var exception = Record.Exception(() => new EquatableCollectionAnalyzer());
        Assert.Null(exception);
    }

    [Fact]
    public void EquatableClass_WithArrayProperty_WithoutEqualityAttribute_ShouldReportDiagnostic()
    {
        var source =
            @"
using Generator.Equals;

[Equatable]
public class TestClass
{
    public string[] Items { get; set; }
}";

        VerifyDiagnostic(source, "Items");
    }

    [Fact]
    public void EquatableClass_WithListProperty_WithoutEqualityAttribute_ShouldReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    public List<int> Numbers { get; set; }
}";

        VerifyDiagnostic(source, "Numbers");
    }



    [Fact]
    public void EquatableClass_WithCollectionProperty_WithIgnoreEqualityAttribute_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    [IgnoreEquality]
    public List<string> Items { get; set; }
}";

        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void EquatableClass_WithCollectionProperty_WithDefaultEqualityAttribute_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    [DefaultEquality]
    public string[] Items { get; set; }
}";

        VerifyNoDiagnostic(source);
    }


    [Fact]
    public void EquatableClass_WithCollectionProperty_WithOrderedEqualityAttribute_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    [OrderedEquality]
    public List<string> Items { get; set; }
}";

        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void EquatableClass_WithArrayProperty_WithUnorderedEqualityAttribute_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using Generator.Equals;

[Equatable]
public class TestClass
{
    [UnorderedEquality]
    public string[] Items { get; set; }
}";

        VerifyNoDiagnostic(source);
    }


    [Fact]
    public void EquatableClass_WithCollectionProperty_WithReferenceEqualityAttribute_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    [ReferenceEquality]
    public List<object> Items { get; set; }
}";

        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void NonEquatableClass_WithCollections_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

public class TestClass
{
    public string[] Items; // Field - should be ignored
    public List<int> Numbers { get; set; } // Property - should be ignored (no [Equatable] attribute)
    public List<object> Data { get; set; } = []
}";

        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void EquatableClass_WithNonCollectionMembers_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using Generator.Equals;

[Equatable]
public class TestClass
{
    public string Name { get; set; }
    public int Age;
    public bool IsActive { get; set; }
}";

        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void EquatableClass_WithNonPublicCollectionProperties_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    private List<string> PrivateItems { get; set; }
    protected List<int> ProtectedNumbers { get; set; } = []
    internal List<object> InternalData { get; set; } = []
}";

        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void EquatableClass_WithMixedAccessibilityCollectionProperties_OnlyPublicShouldReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    private List<string> PrivateItems { get; set; }
    protected List<int> ProtectedNumbers { get; set; } = []
    internal List<object> InternalData { get; set; } = []
    public string[] PublicItems { get; set; }
}";

        VerifyDiagnostic(source, "PublicItems");
    }

    [Fact]
    public void EquatableClass_WithPublicCollectionFields_WithoutAttributes_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    public List<string> PublicListField;
    public string[] PublicArrayField;
    public List<int> PublicListField2;
    public string[] PublicArrayField2;
}";

        // Fields should not trigger diagnostics - only public properties should
        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void EquatableClass_WithPublicCollectionFields_WithAttributes_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    [OrderedEquality]
    public List<string> AttributedListField;

    [UnorderedEquality]
    public string[] AttributedArrayField;

    [OrderedEquality]
    public List<int> AttributedListField2;

    [UnorderedEquality]
    public string[] AttributedArrayField2;

    [IgnoreEquality]
    public List<object> IgnoredListField;
}";

        // Even attributed fields should not trigger diagnostics - analyzer only checks properties
        VerifyNoDiagnostic(source);
    }

    [Fact]
    public void EquatableClass_WithMixedFieldsAndProperties_OnlyPublicPropertiesAnalyzed()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    // These fields should be ignored (no diagnostics)
    public List<string> PublicListField;
    private string[] PrivateArrayField;

    // These properties should be analyzed
    [OrderedEquality]
    public List<int> AttributedProperty { get; set; }

    public List<string> UnattributedProperty { get; set; } = []  // Should trigger diagnostic
}";

        VerifyDiagnostic(source, "UnattributedProperty");
    }

    private static void VerifyDiagnostic(string source, string expectedFieldName)
    {
        var diagnostics = GetDiagnostics(source);
        Assert.Single(diagnostics);

        var diagnostic = diagnostics[0];
        Assert.Equal("GE001", diagnostic.Id);

        // The message format is "Collection property 'Items' in Equatable class 'TestClass' requires an equality attribute"
        // So we check if the expected property name is in the message
        var message = diagnostic.GetMessage();
        Assert.Contains("Collection property", message);
        Assert.Contains(expectedFieldName, message);
        Assert.Contains("TestClass", message);
    }

    private static void VerifyNoDiagnostic(string source)
    {
        var diagnostics = GetDiagnostics(source);
        Assert.Empty(diagnostics);
    }

    private static Diagnostic[] GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Use Basic.Reference.Assemblies for complete reference set
        var references = new List<MetadataReference>(Net80.References.All);
        references.Add(s_generatorEqualsReference);

        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var analyzer = new EquatableCollectionAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)
        );

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return diagnostics.Where(d => d.Id == "GE001").ToArray();
    }

    #region GE002 Tests - Complex Object Properties

    [Fact]
    public void EquatableClass_WithComplexObjectProperty_WithoutEquatableOnType_ShouldReportGE002()
    {
        var source = """
            using Generator.Equals;

            namespace TestNamespace
            {
                public class Address
                {
                    public string Street { get; set; } = "";
                }

                [Equatable]
                public partial class Person
                {
                    public Address HomeAddress { get; set; } = new();
                }
            }
            """;

        var diagnostics = GetGE002Diagnostics(source);

        Assert.Single(diagnostics);
        Assert.Equal("GE002", diagnostics[0].Id);
        Assert.Contains("HomeAddress", diagnostics[0].GetMessage());
        Assert.Contains("Address", diagnostics[0].GetMessage());
        Assert.Contains("Person", diagnostics[0].GetMessage());
    }

    [Fact]
    public void EquatableClass_WithComplexObjectProperty_WithEquatableOnType_ShouldNotReportGE002()
    {
        var source = """
            using Generator.Equals;

            namespace TestNamespace
            {
                [Equatable]
                public partial class Address
                {
                    public string Street { get; set; } = "";
                }

                [Equatable]
                public partial class Person
                {
                    public Address HomeAddress { get; set; } = new();
                }
            }
            """;

        var diagnostics = GetGE002Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EquatableClass_WithPrimitiveProperties_ShouldNotReportGE002()
    {
        var source = """
            using Generator.Equals;
            using System;

            namespace TestNamespace
            {
                [Equatable]
                public partial class Person
                {
                    public string Name { get; set; } = "";
                    public int Age { get; set; }
                    public DateTime Created { get; set; }
                    public bool IsActive { get; set; }
                    public decimal Salary { get; set; }
                }
            }
            """;

        var diagnostics = GetGE002Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EquatableClass_WithSystemTypeProperties_ShouldNotReportGE002()
    {
        var source = """
            using Generator.Equals;
            using System;

            namespace TestNamespace
            {
                [Equatable]
                public partial class Person
                {
                    public DateTime Created { get; set; }
                    public TimeSpan Duration { get; set; }
                    public Guid Id { get; set; }
                }
            }
            """;

        var diagnostics = GetGE002Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EquatableClass_WithEnumProperty_ShouldNotReportGE002()
    {
        var source = """
            using Generator.Equals;

            namespace TestNamespace
            {
                public enum Status
                {
                    Active,
                    Inactive
                }

                [Equatable]
                public partial class Person
                {
                    public Status CurrentStatus { get; set; }
                }
            }
            """;

        var diagnostics = GetGE002Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NonEquatableClass_WithComplexObjectProperty_ShouldNotReportGE002()
    {
        var source = """
            using Generator.Equals;

            namespace TestNamespace
            {
                public class Address
                {
                    public string Street { get; set; } = "";
                }

                public class Person
                {
                    public Address HomeAddress { get; set; } = new();
                }
            }
            """;

        var diagnostics = GetGE002Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    private static Diagnostic[] GetGE002Diagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>(Net80.References.All);
        references.Add(s_generatorEqualsReference);

        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var analyzer = new EquatableCollectionAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)
        );

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return diagnostics.Where(d => d.Id == "GE002").ToArray();
    }

    #endregion

    #region GE003 Tests - Collection Element Types

    [Fact]
    public void EquatableClass_WithListOfComplexObjects_WithoutEquatableOnElementType_ShouldReportGE003()
    {
        var source = """
            using System.Collections.Generic;
            using Generator.Equals;

            namespace TestNamespace
            {
                public class Customer
                {
                    public string Name { get; set; } = "";
                }

                [Equatable]
                public partial class Order
                {
                    [OrderedEquality]
                    public List<Customer> Customers { get; set; } = new();
                }
            }
            """;

        var diagnostics = GetGE003Diagnostics(source);

        Assert.Single(diagnostics);
        Assert.Equal("GE003", diagnostics[0].Id);
        Assert.Contains("Customers", diagnostics[0].GetMessage());
        Assert.Contains("Customer", diagnostics[0].GetMessage());
        Assert.Contains("Order", diagnostics[0].GetMessage());
    }

    [Fact]
    public void EquatableClass_WithArrayOfComplexObjects_WithoutEquatableOnElementType_ShouldReportGE003()
    {
        var source = """
            using Generator.Equals;

            namespace TestNamespace
            {
                public class Product
                {
                    public string Name { get; set; } = "";
                }

                [Equatable]
                public partial class Store
                {
                    [UnorderedEquality]
                    public Product[] Products { get; set; } = [];
                }
            }
            """;

        var diagnostics = GetGE003Diagnostics(source);

        Assert.Single(diagnostics);
        Assert.Equal("GE003", diagnostics[0].Id);
        Assert.Contains("Products", diagnostics[0].GetMessage());
        Assert.Contains("Product", diagnostics[0].GetMessage());
        Assert.Contains("Store", diagnostics[0].GetMessage());
    }


    [Fact]
    public void EquatableClass_WithListOfComplexObjects_WithEquatableOnElementType_ShouldNotReportGE003()
    {
        var source = """
            using System.Collections.Generic;
            using Generator.Equals;

            namespace TestNamespace
            {
                [Equatable]
                public partial class Customer
                {
                    public string Name { get; set; } = "";
                }

                [Equatable]
                public partial class Order
                {
                    [OrderedEquality]
                    public List<Customer> Customers { get; set; } = new();
                }
            }
            """;

        var diagnostics = GetGE003Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EquatableClass_WithCollectionOfPrimitives_ShouldNotReportGE003()
    {
        var source = """
            using System.Collections.Generic;
            using Generator.Equals;

            namespace TestNamespace
            {
                [Equatable]
                public partial class Numbers
                {
                    [OrderedEquality]
                    public List<int> Values { get; set; } = new();

                    [UnorderedEquality]
                    public string[] Names { get; set; } = [];

                    [OrderedEquality]
                    public decimal[] Prices { get; set; } = [];
                }
            }
            """;

        var diagnostics = GetGE003Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EquatableClass_WithCollectionOfSystemTypes_ShouldNotReportGE003()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using Generator.Equals;

            namespace TestNamespace
            {
                [Equatable]
                public partial class DateContainer
                {
                    [OrderedEquality]
                    public List<DateTime> Dates { get; set; } = new();

                    [UnorderedEquality]
                    public Guid[] Ids { get; set; } = [];

                    [DefaultEquality]
                    public List<TimeSpan> Durations { get; set; } = new();
                }
            }
            """;

        var diagnostics = GetGE003Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void EquatableClass_WithCollectionOfEnums_ShouldNotReportGE003()
    {
        var source = """
            using System.Collections.Generic;
            using Generator.Equals;

            namespace TestNamespace
            {
                public enum Status
                {
                    Active,
                    Inactive
                }

                [Equatable]
                public partial class StatusContainer
                {
                    [UnorderedEquality]
                    public Status[] Statuses { get; set; } = [];
                }
            }
            """;

        var diagnostics = GetGE003Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NonEquatableClass_WithCollectionOfComplexObjects_ShouldNotReportGE003()
    {
        var source = """
            using System.Collections.Generic;
            using Generator.Equals;

            namespace TestNamespace
            {
                public class Customer
                {
                    public string Name { get; set; } = "";
                }

                public class Order
                {
                    public List<Customer> Customers { get; set; } = new();
                }
            }
            """;

        var diagnostics = GetGE003Diagnostics(source);

        Assert.Empty(diagnostics);
    }

    private static Diagnostic[] GetGE003Diagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>(Net80.References.All);
        references.Add(s_generatorEqualsReference);

        var compilation = CSharpCompilation.Create(
            "TestCompilation",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var analyzer = new EquatableCollectionAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)
        );

        var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
        return diagnostics.Where(d => d.Id == "GE003").ToArray();
    }

    #endregion
}
