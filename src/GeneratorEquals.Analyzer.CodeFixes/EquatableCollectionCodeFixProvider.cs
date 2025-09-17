using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GeneratorEquals.Analyzer.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EquatableCollectionCodeFixProvider)), Shared]
    public class EquatableCollectionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(GeneratorEquals.Analyzer.EquatableCollectionAnalyzer.GE001.Id);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.FirstOrDefault(d => FixableDiagnosticIds.Contains(d.Id));
            if (diagnostic == null)
                return;

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the property declaration that triggered the diagnostic
            var propertyDeclaration = root?.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyDeclaration == null || root == null)
                return;

            // Get semantic model to analyze the property type
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return;

            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken) as IPropertySymbol;
            if (propertySymbol == null)
                return;

            var propertyType = propertySymbol.Type;

            // Register collection-specific code actions based on the actual property type
            if (IsDictionaryType(propertyType))
            {
                RegisterCodeAction(context, root, propertyDeclaration, "Add [DictionaryEquality]", "DictionaryEquality", "dictionary_equality");
            }
            else if (IsSetType(propertyType))
            {
                RegisterCodeAction(context, root, propertyDeclaration, "Add [SetEquality]", "SetEquality", "set_equality");
            }
            else if (IsCollectionType(propertyType))
            {
                // For general collections, offer both ordered and unordered options
                RegisterCodeAction(context, root, propertyDeclaration, "Add [OrderedEquality]", "OrderedEquality", "ordered_equality");
                RegisterCodeAction(context, root, propertyDeclaration, "Add [UnorderedEquality]", "UnorderedEquality", "unordered_equality");
            }
        }

        private static void RegisterCodeAction(CodeFixContext context, SyntaxNode root, PropertyDeclarationSyntax propertyDeclaration, string title, string attributeName, string equivalenceKey)
        {
            var action = CodeAction.Create(
                title: title,
                createChangedDocument: c => AddEqualityAttributeAsync(context.Document, root, propertyDeclaration, attributeName, c),
                equivalenceKey: equivalenceKey);

            context.RegisterCodeFix(action, context.Diagnostics);
        }

        private static Task<Document> AddEqualityAttributeAsync(Document document, SyntaxNode root, PropertyDeclarationSyntax propertyDeclaration, string attributeName, CancellationToken cancellationToken)
        {
            // Create the attribute syntax without arguments for most attributes
            // Most Generator.Equals attributes don't require parameters
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName));

            var attributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(attribute))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            // Add the attribute to the property, preserving existing attributes
            var newPropertyDeclaration = propertyDeclaration.AddAttributeLists(attributeList);

            // Replace the old property declaration with the new one in the syntax tree
            var newRoot = root.ReplaceNode(propertyDeclaration, newPropertyDeclaration);

            // Return the updated document
            var newDocument = document.WithSyntaxRoot(newRoot);
            return Task.FromResult(newDocument);
        }

        private static bool IsDictionaryType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
                return false;

            // Check for Dictionary<TKey, TValue> or IDictionary<TKey, TValue>
            var typeName = namedType.Name;
            var namespaceName = namedType.ContainingNamespace?.ToDisplayString();

            return (typeName == "Dictionary" || typeName == "IDictionary") &&
                   namespaceName == "System.Collections.Generic" &&
                   namedType.TypeArguments.Length == 2;
        }

        private static bool IsSetType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
                return false;

            // Check for HashSet<T>, ISet<T>, or SortedSet<T>
            var typeName = namedType.Name;
            var namespaceName = namedType.ContainingNamespace?.ToDisplayString();

            return (typeName == "HashSet" || typeName == "ISet" || typeName == "SortedSet") &&
                   namespaceName == "System.Collections.Generic" &&
                   namedType.TypeArguments.Length == 1;
        }

        private static bool IsCollectionType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType)
                return false;

            // Check for List<T>, IList<T>, ICollection<T>, IEnumerable<T>, arrays, etc.
            var typeName = namedType.Name;
            var namespaceName = namedType.ContainingNamespace?.ToDisplayString();

            // Arrays
            if (typeSymbol.TypeKind == TypeKind.Array)
                return true;

            // Generic collections
            return namespaceName == "System.Collections.Generic" &&
                   (typeName == "List" || typeName == "IList" || typeName == "ICollection" ||
                    typeName == "IEnumerable" || typeName == "Collection") &&
                   namedType.TypeArguments.Length == 1;
        }
    }
}
