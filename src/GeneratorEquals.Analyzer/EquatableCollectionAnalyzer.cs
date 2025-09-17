using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GeneratorEquals.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EquatableCollectionAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor GE001 = new(
            id: "GE001",
            title: Resources.GE001Title,
            messageFormat: Resources.GE001MessageFormat,
            category: Resources.GE001Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Resources.GE001Description
        );

        public static readonly DiagnosticDescriptor GE002 = new(
            id: "GE002",
            title: "Complex object property type lacks Equatable attribute",
            messageFormat: "Property '{0}' of type '{1}' in Equatable class '{2}' requires that type '{1}' has [Equatable] attribute for proper equality comparison",
            category: "Generator.Equals.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Properties of complex object types in Equatable classes require that the property type also has the [Equatable] attribute to ensure proper equality comparison."
        );

        public static readonly DiagnosticDescriptor GE003 = new(
            id: "GE003",
            title: "Collection element type requires Equatable attribute",
            messageFormat: "Collection property '{0}' contains elements of type '{1}' which should have [Equatable] attribute in Equatable class '{2}'",
            category: "Generator.Equals.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Collection elements in Equatable classes should have the [Equatable] attribute on their element type to ensure proper equality comparison."
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [GE001, GE002, GE003];

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            // Check if class has [Equatable] attribute
            if (!HasEquatableAttribute(classDeclaration, context.SemanticModel))
                return;

            // Analyze only public properties (not fields)
            foreach (var member in classDeclaration.Members)
            {
                if (member is PropertyDeclarationSyntax property)
                {
                    AnalyzePropertyDeclaration(context, property);
                }
            }
        }

        private static void AnalyzePropertyDeclaration(
            SyntaxNodeAnalysisContext context,
            PropertyDeclarationSyntax property
        )
        {
            // Only analyze public properties
            if (!IsPublicProperty(property))
                return;

            var typeSymbol = context.SemanticModel.GetTypeInfo(property.Type).Type;
            if (typeSymbol == null)
                return;

            var propertyName = property.Identifier.ValueText;
            var classDecl = property.Parent as ClassDeclarationSyntax;
            var className = classDecl?.Identifier.ValueText ?? "class";

            // GE001: Check collection properties for missing equality attributes
            if (IsCollectionType(typeSymbol))
            {
                if (!HasEqualityAttribute(property.AttributeLists))
                {
                    var diagnostic = Diagnostic.Create(
                        GE001,
                        property.Type.GetLocation(),
                        propertyName,
                        className
                    );
                    context.ReportDiagnostic(diagnostic);
                }

                // GE003: Check collection element types for missing [Equatable] attribute
                var elementType = GetCollectionElementType(typeSymbol);
                if (
                    elementType != null
                    && IsComplexObjectType(elementType)
                    && !HasEquatableAttributeOnSymbol(elementType)
                )
                {
                    var elementTypeName = elementType.Name;
                    var diagnostic = Diagnostic.Create(
                        GE003,
                        property.Type.GetLocation(),
                        propertyName,
                        elementTypeName,
                        className
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
            // GE002: Check complex object properties for missing [Equatable] on their type
            else if (IsComplexObjectType(typeSymbol))
            {
                if (!HasEquatableAttributeOnSymbol(typeSymbol))
                {
                    var typeName = typeSymbol.Name;
                    var diagnostic = Diagnostic.Create(
                        GE002,
                        property.Type.GetLocation(),
                        propertyName,
                        typeName,
                        className
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsPublicProperty(PropertyDeclarationSyntax property)
        {
            // Check if the property has public modifier
            return property.Modifiers.Any(SyntaxKind.PublicKeyword);
        }

        private static bool HasEqualityAttribute(SyntaxList<AttributeListSyntax> attributeLists)
        {
            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (
                        name.Contains("IgnoreEquality")
                        || name.Contains("DefaultEquality")
                        || name.Contains("SequenceEquality")
                        || name.Contains("ReferenceEquality")
                        || name.Contains("OrderedEquality")
                        || name.Contains("UnorderedEquality")
                    )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsCollectionType(ITypeSymbol typeSymbol)
        {
            // Check for arrays (T[])
            if (typeSymbol.TypeKind == TypeKind.Array)
                return true;

            // Check for generic collections
            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var fullName = namedType.ConstructedFrom.ToDisplayString();

                // Generic lists and collections
                if (
                    fullName == "System.Collections.Generic.List<T>"
                    || fullName == "System.Collections.Generic.IList<T>"
                    || fullName == "System.Collections.Generic.ICollection<T>"
                    || fullName == "System.Collections.Generic.IEnumerable<T>"
                    || fullName == "System.Collections.Generic.Collection<T>"
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasEquatableAttribute(
            ClassDeclarationSyntax classDeclaration,
            SemanticModel semanticModel
        )
        {
            // Check direct attributes on the class
            foreach (var attributeList in classDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (IsEquatableAttribute(attribute, semanticModel))
                        return true;
                }
            }

            // Check inheritance hierarchy for Equatable attribute
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol != null)
            {
                var baseType = classSymbol.BaseType;
                while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
                {
                    if (HasEquatableAttributeOnSymbol(baseType))
                        return true;
                    baseType = baseType.BaseType;
                }
            }

            return false;
        }

        private static bool IsEquatableAttribute(
            AttributeSyntax attribute,
            SemanticModel semanticModel
        )
        {
            var attributeSymbol = semanticModel.GetSymbolInfo(attribute).Symbol;

            if (attributeSymbol is IMethodSymbol constructor)
            {
                var attributeClass = constructor.ContainingType;
                var fullName = attributeClass.ToDisplayString();

                // Check for Generator.Equals.Equatable attribute
                return fullName == "Generator.Equals.EquatableAttribute"
                    || fullName == "Generator.Equals.Equatable"
                    || attributeClass.Name == "EquatableAttribute"
                    || attributeClass.Name == "Equatable";
            }

            return false;
        }

        private static bool HasEquatableAttributeOnSymbol(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                return false;

            foreach (var attribute in namedTypeSymbol.GetAttributes())
            {
                var attributeClass = attribute.AttributeClass;
                if (attributeClass != null)
                {
                    var fullName = attributeClass.ToDisplayString();
                    if (
                        fullName == "Generator.Equals.EquatableAttribute"
                        || fullName == "Generator.Equals.Equatable"
                        || attributeClass.Name == "EquatableAttribute"
                        || attributeClass.Name == "Equatable"
                    )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsComplexObjectType(ITypeSymbol typeSymbol)
        {
            // Skip primitive types
            if (typeSymbol.SpecialType != SpecialType.None)
                return false;

            // Skip nullable value types - check underlying type
            if (
                typeSymbol is INamedTypeSymbol namedType
                && namedType.Name == "Nullable"
                && namedType.ContainingNamespace?.ToDisplayString() == "System"
            )
            {
                var underlyingType = namedType.TypeArguments.FirstOrDefault();
                if (underlyingType != null)
                    return IsComplexObjectType(underlyingType);
            }

            // Skip common system types
            var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
            if (
                namespaceName != null
                && (
                    namespaceName.StartsWith("System")
                    || namespaceName == "Microsoft.Extensions.Logging"
                    || namespaceName.StartsWith("System.Collections")
                )
            )
                return false;

            // Skip enums
            if (typeSymbol.TypeKind == TypeKind.Enum)
                return false;

            // Skip collections (handled by GE001)
            if (IsCollectionType(typeSymbol))
                return false;

            // Skip arrays (handled by GE001)
            if (typeSymbol.TypeKind == TypeKind.Array)
                return false;

            // It's a complex object if it's a class or struct from user code
            return typeSymbol.TypeKind == TypeKind.Class || typeSymbol.TypeKind == TypeKind.Struct;
        }

        private static ITypeSymbol? GetCollectionElementType(ITypeSymbol typeSymbol)
        {
            // Handle arrays - get element type
            if (typeSymbol.TypeKind == TypeKind.Array && typeSymbol is IArrayTypeSymbol arrayType)
            {
                return arrayType.ElementType;
            }

            // Handle generic collections - get first type argument (element type)
            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var fullName = namedType.ConstructedFrom.ToDisplayString();

                // For generic collections, get the first type argument (element type)
                if (
                    fullName == "System.Collections.Generic.List<T>"
                    || fullName == "System.Collections.Generic.IList<T>"
                    || fullName == "System.Collections.Generic.ICollection<T>"
                    || fullName == "System.Collections.Generic.IEnumerable<T>"
                    || fullName == "System.Collections.Generic.Collection<T>"
                )
                {
                    return namedType.TypeArguments.Length >= 1 ? namedType.TypeArguments[0] : null;
                }
            }

            return null;
        }
    }
}
