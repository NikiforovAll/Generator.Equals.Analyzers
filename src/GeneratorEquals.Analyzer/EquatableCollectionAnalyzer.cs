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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [GE001];

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
            if (typeSymbol != null && IsCollectionType(typeSymbol))
            {
                if (!HasEqualityAttribute(property.AttributeLists))
                {
                    var propertyName = property.Identifier.ValueText;
                    var classDecl = property.Parent as ClassDeclarationSyntax;
                    var className = classDecl?.Identifier.ValueText ?? "class";

                    var diagnostic = Diagnostic.Create(
                        GE001,
                        property.Type.GetLocation(),
                        propertyName,
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
                        || name.Contains("SetEquality")
                        || name.Contains("SequenceEquality")
                        || name.Contains("ReferenceEquality")
                        || name.Contains("OrderedEquality")
                        || name.Contains("UnorderedEquality")
                        || name.Contains("DictionaryEquality")
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
                    || fullName == "System.Collections.ObjectModel.ObservableCollection<T>"
                )
                {
                    return true;
                }

                // Dictionaries
                if (
                    fullName == "System.Collections.Generic.Dictionary<TKey, TValue>"
                    || fullName == "System.Collections.Generic.IDictionary<TKey, TValue>"
                    || fullName == "System.Collections.Generic.SortedDictionary<TKey, TValue>"
                    || fullName
                        == "System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>"
                )
                {
                    return true;
                }

                // Sets
                if (
                    fullName == "System.Collections.Generic.HashSet<T>"
                    || fullName == "System.Collections.Generic.ISet<T>"
                    || fullName == "System.Collections.Generic.SortedSet<T>"
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

        private static bool HasEquatableAttributeOnSymbol(INamedTypeSymbol typeSymbol)
        {
            foreach (var attribute in typeSymbol.GetAttributes())
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
    }
}
