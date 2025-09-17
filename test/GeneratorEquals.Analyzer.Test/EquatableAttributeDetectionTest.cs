using System.Reflection;
using Basic.Reference.Assemblies;
using Generator.Equals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GeneratorEquals.Analyzer.Test;

public class EquatableAttributeDetectionTest
{
    [Fact]
    public void AnalyzeClassDeclaration_WithEquatableAttribute_ShouldDetect()
    {
        var source =
            @"
using Generator.Equals;

[Equatable]
public class TestClass
{
    public List<string> Items { get; set; }
}";

        VerifyAnalyzerFindsEquatableClass(source);
    }

    [Fact]
    public void AnalyzeClassDeclaration_WithEquatableAttributeSuffix_ShouldDetect()
    {
        var source =
            @"
using Generator.Equals;

[EquatableAttribute]
public class TestClass
{
    public string[] Items { get; set; }
}";

        VerifyAnalyzerFindsEquatableClass(source);
    }

    [Fact]
    public void AnalyzeClassDeclaration_WithFullyQualifiedAttribute_ShouldDetect()
    {
        var source =
            @"
[Generator.Equals.Equatable]
public class TestClass
{
    public System.Collections.Generic.List<int> Numbers { get; set; }
}";

        VerifyAnalyzerFindsEquatableClass(source);
    }

    [Fact]
    public void AnalyzeClassDeclaration_WithoutEquatableAttribute_ShouldNotDetect()
    {
        var source =
            @"
public class TestClass
{
    public List<string> Items { get; set; }
}";

        VerifyAnalyzerDoesNotFindEquatableClass(source);
    }

    [Fact]
    public void AnalyzeClassDeclaration_WithOtherAttributes_ShouldNotDetect()
    {
        var source =
            @"
using System;

[Serializable]
[Obsolete]
public class TestClass
{
    public string[] Items { get; set; }
}";

        VerifyAnalyzerDoesNotFindEquatableClass(source);
    }

    [Fact]
    public void AnalyzeClassDeclaration_WithInheritedEquatable_ShouldDetect()
    {
        var source =
            @"
using Generator.Equals;

[Equatable]
public class BaseClass
{
}

public class DerivedClass : BaseClass
{
    public List<string> Items { get; set; }
}";

        VerifyAnalyzerFindsEquatableClass(source, "DerivedClass");
    }

    [Fact]
    public void AnalyzeClassDeclaration_WithMultipleInheritanceLevels_ShouldDetect()
    {
        var source =
            @"
using Generator.Equals;

[Equatable]
public class GrandParentClass
{
}

public class ParentClass : GrandParentClass
{
}

public class ChildClass : ParentClass
{
    public Dictionary<string, int> Data { get; set; }
}";

        VerifyAnalyzerFindsEquatableClass(source, "ChildClass");
    }

    private static void VerifyAnalyzerFindsEquatableClass(
        string source,
        string? targetClassName = null
    )
    {
        var compilation = CreateCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var classDeclarations = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();

        bool foundEquatableClass = false;
        foreach (var classDecl in classDeclarations)
        {
            // Skip if looking for a specific class name
            if (targetClassName != null && classDecl.Identifier.ValueText != targetClassName)
                continue;

            // Use reflection to call the private HasEquatableAttribute method
            var method = typeof(EquatableCollectionAnalyzer).GetMethod(
                "HasEquatableAttribute",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            if (method != null)
            {
                var result = method.Invoke(null, [classDecl, semanticModel]);
                if (result is bool boolResult && boolResult)
                {
                    foundEquatableClass = true;
                    break;
                }
            }
        }

        Assert.True(foundEquatableClass, "Analyzer should detect Equatable attribute");
    }

    private static void VerifyAnalyzerDoesNotFindEquatableClass(string source)
    {
        var compilation = CreateCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var classDeclarations = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();

        bool foundEquatableClass = false;
        foreach (var classDecl in classDeclarations)
        {
            // Use reflection to call the private HasEquatableAttribute method
            var method = typeof(EquatableCollectionAnalyzer).GetMethod(
                "HasEquatableAttribute",
                BindingFlags.NonPublic | BindingFlags.Static
            );

            if (method != null)
            {
                var result = method.Invoke(null, [classDecl, semanticModel]);
                if (result is bool boolResult && boolResult)
                {
                    foundEquatableClass = true;
                    break;
                }
            }
        }

        Assert.False(foundEquatableClass, "Analyzer should not detect Equatable attribute");
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Use Basic.Reference.Assemblies for complete reference set
        var references = new List<MetadataReference>(Net80.References.All);
        references.Add(
            MetadataReference.CreateFromFile(typeof(EquatableAttribute).Assembly.Location)
        );

        return CSharpCompilation.Create(
            "TestCompilation",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}
