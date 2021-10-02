namespace UnitySourceGenerators;

[Generator]
public class SimpleTestSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        /*IEnumerable<ClassDeclarationSyntax> allClasses = context.Compilation.SyntaxTrees
            .SelectMany(static syntaxTree => syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>());
        IEnumerable<ClassDeclarationSyntax> generatorTestScriptClass = allClasses.Where(static cl => cl.Identifier.ToString() == "GeneratorTestScript");*/

        /*foreach (ClassDeclarationSyntax cl in generatorTestScriptClass)
        {*/
            //context.AddSource("SimpleTest", SourceText.From(Templates.SimpleTest(), Encoding.UTF8));
        //}

        SyntaxReceiver sr = (SyntaxReceiver)context.SyntaxReceiver!;

        foreach (string s in sr.ClassNames)
        {
            context.AddSource("SimpleTest", SourceText.From(Templates.SimpleTest(s), Encoding.UTF8));
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }
}

internal class SyntaxReceiver : ISyntaxReceiver
{
    public List<string> ClassNames { get; private set; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax cl && cl.Identifier.ToString() == "GeneratorTestScript")
        {
            ClassNames.Add(cl.Identifier.ToString());
        }
    }
}