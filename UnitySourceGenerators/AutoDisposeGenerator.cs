using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace UnitySourceGenerators
{
    [Generator]
    public class AutoDisposePersistentNativeCollections : ISourceGenerator
    {
        private static readonly string[] _nativeCollections = { "NativeArray", "NativeList" };

        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            IEnumerable<ClassDeclarationSyntax> allClasses = context.Compilation.SyntaxTrees
                .SelectMany(static syntaxTree => syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>());
            IEnumerable<ClassDeclarationSyntax> systemBaseInheritedClasses = allClasses
                .Where(static classDeclarationSyntax => classDeclarationSyntax.BaseList is not null && classDeclarationSyntax.BaseList.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(static l => l.Identifier.ToString() == "SystemBase"));

            IEnumerable<GenericNameSyntax> nativeVariableDeclarations = systemBaseInheritedClasses
                .SelectMany(static classDeclaration => classDeclaration.DescendantNodes().OfType<GenericNameSyntax>())
                .Where(static genericNameSyntax => MultiCompare(genericNameSyntax.Identifier.ToString()));
            IEnumerable<string> nativeCollections = nativeVariableDeclarations
                .SelectMany(static genericNameSyntax => genericNameSyntax.Parent.DescendantNodes().OfType<VariableDeclaratorSyntax>())
                .Select(static variableDeclaratorSyntax => variableDeclaratorSyntax.Identifier.ToString());

            foreach (ClassDeclarationSyntax cl in systemBaseInheritedClasses)
            {
                context.AddSource("AutoDispose", SourceText.From(Templates.AutoDispose(nativeCollections, cl.Identifier.ToString()), Encoding.UTF8));
            }
        }

        private static bool MultiCompare(string toCompare)
        {
            foreach (string item in _nativeCollections)
            {
                if (toCompare == item)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static class Templates
    {
        public static string AutoDispose(IEnumerable<string> persistentNativeCollections, string className)
        {
            StringBuilder sb = new($@"
public partial class {className}
{{

    protected override void OnDestroy()
    {{
");
            foreach (string nativeCollection in persistentNativeCollections)
            {
                sb.AppendLine($"        {nativeCollection}.Dispose();");
            }

            sb.Append(@"
        base.OnDestroy();
    }
}");

            return sb.ToString();
        }

        public static string SimpleTest(string className)
        {
            StringBuilder sb = new($@"
public partial class {className}
{{
    public static string Test {{ get; set; }} = ""Test"";
}}");

            return sb.ToString();
        }
    }
}
