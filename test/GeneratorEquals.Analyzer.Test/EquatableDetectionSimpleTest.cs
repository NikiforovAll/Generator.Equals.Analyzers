using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GeneratorEquals.Analyzer.Test;

public class EquatableDetectionSimpleTest
{
    [Fact]
    public void HasEquatableAttribute_WithNamedEquatable_ShouldReturnTrue()
    {
        var source =
            @"
[Equatable]
public class TestClass
{
}

// Mock attribute definition
public class EquatableAttribute : System.Attribute
{
}";

        var (classDecl, semanticModel) = CreateTestData(source);
        var result = CallHasEquatableAttribute(classDecl, semanticModel);

        Assert.True(result);
    }

    [Fact]
    public void HasEquatableAttribute_WithEquatableAttributeSuffix_ShouldReturnTrue()
    {
        var source =
            @"
[EquatableAttribute]
public class TestClass
{
}

// Mock attribute definition
public class EquatableAttribute : System.Attribute
{
}";

        var (classDecl, semanticModel) = CreateTestData(source);
        var result = CallHasEquatableAttribute(classDecl, semanticModel);

        Assert.True(result);
    }

    [Fact]
    public void HasEquatableAttribute_WithoutAttribute_ShouldReturnFalse()
    {
        var source =
            @"
public class TestClass
{
}";

        var (classDecl, semanticModel) = CreateTestData(source);
        var result = CallHasEquatableAttribute(classDecl, semanticModel);

        Assert.False(result);
    }

    [Fact]
    public void HasEquatableAttribute_WithOtherAttribute_ShouldReturnFalse()
    {
        var source =
            @"
[System.Obsolete]
public class TestClass
{
}";

        var (classDecl, semanticModel) = CreateTestData(source);
        var result = CallHasEquatableAttribute(classDecl, semanticModel);

        Assert.False(result);
    }

    [Fact]
    public void HasEquatableAttribute_WithInheritance_ShouldReturnTrue()
    {
        var source =
            @"
[Equatable]
public class BaseClass
{
}

public class DerivedClass : BaseClass
{
}

// Mock attribute definition
public class EquatableAttribute : System.Attribute
{
}";

        var compilation = CreateCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var derivedClass = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "DerivedClass");

        var result = CallHasEquatableAttribute(derivedClass, semanticModel);

        Assert.True(result);
    }

    private static (
        Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
        SemanticModel
    ) CreateTestData(string source)
    {
        var compilation = CreateCompilation(source);
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var classDeclaration = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "TestClass");

        return (classDeclaration, semanticModel);
    }

    private static bool CallHasEquatableAttribute(
        Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel
    )
    {
        var method = typeof(EquatableCollectionAnalyzer).GetMethod(
            "HasEquatableAttribute",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static
        );

        return method?.Invoke(null, [classDecl, semanticModel]) is bool result && result;
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestCompilation",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );
    }
}
