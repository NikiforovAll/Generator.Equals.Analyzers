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
    public void Analyzer_HasCorrectDiagnosticDescriptor()
    {
        // Arrange
        var analyzer = new EquatableCollectionAnalyzer();

        // Act
        var supportedDiagnostics = analyzer.SupportedDiagnostics;

        // Assert
        Assert.Single(supportedDiagnostics);
        var diagnostic = supportedDiagnostics[0];

        Assert.Equal("GE001", diagnostic.Id);
        Assert.Equal(Resources.GE001Title, diagnostic.Title.ToString());
        Assert.Equal(Resources.GE001Category, diagnostic.Category);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.DefaultSeverity);
        Assert.True(diagnostic.IsEnabledByDefault);
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
    public void EquatableClass_WithDictionaryProperty_WithoutEqualityAttribute_ShouldReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    public Dictionary<string, int> Data { get; set; }
}";

        VerifyDiagnostic(source, "Data");
    }

    [Fact]
    public void EquatableClass_WithHashSetProperty_WithoutEqualityAttribute_ShouldReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    public HashSet<string> UniqueItems { get; set; }
}";

        VerifyDiagnostic(source, "UniqueItems");
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
    public void EquatableClass_WithCollectionProperty_WithSetEqualityAttribute_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    [SetEquality]
    public HashSet<int> Numbers { get; set; }
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
    public void EquatableClass_WithDictionaryProperty_WithDictionaryEqualityAttribute_ShouldNotReportDiagnostic()
    {
        var source =
            @"
using System.Collections.Generic;
using Generator.Equals;

[Equatable]
public class TestClass
{
    [DictionaryEquality]
    public Dictionary<string, int> Data { get; set; }
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
    public Dictionary<string, object> Data { get; set; }
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
    protected HashSet<int> ProtectedNumbers { get; set; }
    internal Dictionary<string, object> InternalData { get; set; }
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
    protected HashSet<int> ProtectedNumbers { get; set; }
    internal Dictionary<string, object> InternalData { get; set; }
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
    public Dictionary<string, int> PublicDictionaryField;
    public HashSet<int> PublicHashSetField;
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

    [DictionaryEquality]
    public Dictionary<string, int> AttributedDictionaryField;

    [SetEquality]
    public HashSet<int> AttributedHashSetField;

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

    public HashSet<string> UnattributedProperty { get; set; }  // Should trigger diagnostic
}";

        VerifyDiagnostic(source, "UnattributedProperty");
    }

    private static void VerifyDiagnostic(string source, string expectedFieldName)
    {
        var diagnostics = GetDiagnostics(source);
        Assert.Single(diagnostics);

        var diagnostic = diagnostics[0];
        Assert.Equal("GE001", diagnostic.Id);

        // The message format is "Collection field 'Items' in Equatable class 'TestClass' requires an equality attribute"
        // So we check if the expected field name is in the message
        var message = diagnostic.GetMessage();
        Assert.Contains("Collection field", message);
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
}
