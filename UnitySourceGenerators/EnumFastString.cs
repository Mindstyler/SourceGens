namespace UnitySourceGenerators;

[Generator]
public class EnumFastStringGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {

    }

    public void Execute(GeneratorExecutionContext context)
    {
        Dictionary<string, (EnumDeclarationSyntax declaration, string modifier, string genericTypeParameters)> enumInfo = new();

        foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
        {
            IEnumerable<EnumDeclarationSyntax> newEnums = syntaxTree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>()
                //this generator can only deal with public and internal enums because it writes extension methods
                .Where(static @enum => @enum.Modifiers.Any(static modifier =>
                    modifier.IsKind(SyntaxKind.PublicKeyword)
                    || modifier.IsKind(SyntaxKind.InternalKeyword)));

            foreach (EnumDeclarationSyntax newEnum in newEnums)
            {
                IEnumerable<SyntaxNode> ancestorTree = newEnum.Ancestors()
                    .Where(static ancestor =>
                        ancestor.IsKind(SyntaxKind.NamespaceDeclaration)
                        || ancestor.IsKind(SyntaxKind.ClassDeclaration)
                        || ancestor.IsKind(SyntaxKind.StructDeclaration)
                        || ancestor.IsKind(SyntaxKind.RecordDeclaration));

                HashSet<SyntaxToken> modifiers = new();

                foreach (SyntaxNode node in ancestorTree)
                {
                    if (node is TypeDeclarationSyntax declarationSyntax)
                    {
                        modifiers.Add(declarationSyntax.Modifiers.Single(static modifier =>
                            modifier.IsKind(SyntaxKind.PublicKeyword)
                            || modifier.IsKind(SyntaxKind.InternalKeyword)
                            || modifier.IsKind(SyntaxKind.PrivateKeyword)
                            || modifier.IsKind(SyntaxKind.ProtectedKeyword)));
                    }
                }

                modifiers.Add(newEnum.Modifiers.Single(static modifier =>
                    modifier.IsKind(SyntaxKind.PublicKeyword)
                    || modifier.IsKind(SyntaxKind.InternalKeyword)));

                if (modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PrivateKeyword) || modifier.IsKind(SyntaxKind.ProtectedKeyword)))
                {
                    continue;
                }

                string modifier = modifiers.Any(static @modifier => @modifier.IsKind(SyntaxKind.InternalKeyword)) ? "internal" : "public";

                string genericTypeParameters = string.Empty;

                string[] fullPath = ancestorTree
                    .Select(syntaxNode => syntaxNode is TypeDeclarationSyntax typeDeclaration
                        ? typeDeclaration.Identifier.Text + AddGenericTypeParametersToListAndReturnNeededOnesForType(typeDeclaration)
                        : ((NamespaceDeclarationSyntax)syntaxNode).Name.ToString())
                    .Reverse()
                    .Concat(new[] { newEnum.Identifier.Text })
                    .ToArray(); //this needs to be a call to ToArray() because it needs to be evaluated to populate 'genericTypeParameters'

                string AddGenericTypeParametersToListAndReturnNeededOnesForType(TypeDeclarationSyntax typeDeclaration)
                {
                    if (typeDeclaration.TypeParameterList is null)
                    {
                        return string.Empty;
                    }

                    genericTypeParameters += typeDeclaration.TypeParameterList.Parameters;

                    return typeDeclaration.TypeParameterList.ToString();
                }

                if (!string.IsNullOrEmpty(genericTypeParameters))
                {
                    genericTypeParameters = "<" + genericTypeParameters + ">";
                }

                try
                {
                    enumInfo.Add($"{string.Join(".", fullPath)}", (newEnum, modifier, genericTypeParameters));
                }
                catch (ArgumentException)
                {
                    // Keep the Generator from crashing should an assembly have 2 or more equal types
                }
            }
        }

        if (enumInfo.Count > 0)
        {
            //#region Debug
            //string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GeneratorResults";

            //string path;
            //int i = 1;

            //do
            //{
            //    path = $@"{folderPath}\Gen{i}.cs";
            //    ++i;
            //}
            //while (File.Exists(path));

            //try
            //{
            //    File.AppendAllText(path, SourceText.From(Template.GetCode(enumInfo, context), Encoding.UTF8).ToString());
            //}
            //catch (Exception e)
            //{
            //    File.AppendAllText(folderPath + @"\Error.cs", e.StackTrace + "\n\n");
            //    throw;
            //}
            //#endregion
            context.AddSource("EnumFastString", SourceText.From(Template.GetCode(enumInfo, context), Encoding.UTF8));
        }
    }

    private static class Template
    {
        internal static string GetCode(Dictionary<string, (EnumDeclarationSyntax declaration, string modifier, string genericTypeParameters)> enumInfo, GeneratorExecutionContext context)
        {
            StringBuilder sb = new($@"
namespace SourceGenerated
{{
    public static class EnumExtensions
    {{");
            foreach (string fullType in enumInfo.Keys)
            {
                sb.Append($@"
        {enumInfo[fullType].modifier} static string ToFastString{enumInfo[fullType].genericTypeParameters}(this {fullType} @enum)
        {{
            return @enum switch
            {{");
                foreach (EnumMemberDeclarationSyntax name in enumInfo[fullType].declaration.Members.Distinct(new EnumMemberComparer(context, GetSiblings(enumInfo[fullType].declaration))))
                {
                    sb.Append($"\n                {fullType}.{name.Identifier.Text} => nameof({fullType}.{name.Identifier.Text}),");
                }
                
                sb.Append("\n                _ => throw new System.ArgumentOutOfRangeException(nameof(@enum), @enum, null)\n            };\n        }");
            }

            sb.Append("\n    }\n}");
            
            return sb.ToString();

            static IEnumerable<(EnumMemberDeclarationSyntax, EnumMemberDeclarationSyntax)> GetSiblings(EnumDeclarationSyntax enumSyntax)
            {
                IEnumerable<EnumMemberDeclarationSyntax> childNodes = enumSyntax.ChildNodes().OfType<EnumMemberDeclarationSyntax>();

                return childNodes.Zip(childNodes.Skip(1), static (previous, current) => (previous, current));
            }
        }

        private class EnumMemberComparer : IEqualityComparer<EnumMemberDeclarationSyntax>
        {
            private readonly GeneratorExecutionContext _context;

            private readonly IEnumerable<(EnumMemberDeclarationSyntax previous, EnumMemberDeclarationSyntax current)> _siblings;

            internal EnumMemberComparer(GeneratorExecutionContext context, IEnumerable<(EnumMemberDeclarationSyntax, EnumMemberDeclarationSyntax)> siblings)
            {
                _context = context;
                _siblings = siblings;
            }

            public bool Equals(EnumMemberDeclarationSyntax x, EnumMemberDeclarationSyntax y)/* => ((x.EqualsValue is null) && (y.EqualsValue is null)) ? false : GetValue(x) == GetValue(y);*/
            {
                if ((x.EqualsValue is null) && (y.EqualsValue is null)) // if both are null, both can be used
                {
                    return false;
                }

                //File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GeneratorResults\Dup.cs", GetValue(x) + "  " + GetValue(y) + "\n");

                return GetValue(x) == GetValue(y);
            }

            public int GetHashCode(EnumMemberDeclarationSyntax obj) => GetValue(obj).GetHashCode();

            private int GetValue(EnumMemberDeclarationSyntax syntax)
            {
                return (syntax.EqualsValue is not null)
                    ? int.Parse(GetPossibleOperations().ConstantValue.Value.ToString())
                    : SingleOrDefaultHack() + 1;

                // TODO can be replaced with c# 6 version of SingleOrDefault(obj defaultValue) as soon as generators can be built for .net6
                int SingleOrDefaultHack()
                {
                    // .net6 version of the things below
                    //(EnumMemberDeclarationSyntax previous, EnumMemberDeclarationSyntax current)? e = _siblings.SingleOrDefault(siblings => siblings.current == syntax, null);

                    (EnumMemberDeclarationSyntax previous, EnumMemberDeclarationSyntax current)? e;

                    try
                    {
                        e = _siblings.Single(siblings => siblings.current == syntax);
                    }
                    catch (InvalidOperationException)
                    {
                        e = null;
                    }

                    return (e is not null) ? GetValue(e.Value.previous) : 0;
                }

                IOperation GetPossibleOperations()
                {
                    //File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GeneratorResults\op.cs", _context.Compilation.GetSemanticModel(syntax.SyntaxTree).GetOperation(syntax.ChildNodes().OfType<EqualsValueClauseSyntax>().Single()).Children.Single().ConstantValue + "\n\n");

                    return _context.Compilation
                        .GetSemanticModel(syntax.SyntaxTree)
                        .GetOperation(syntax.ChildNodes().OfType<EqualsValueClauseSyntax>().Single()).Children.Single();

                    //something like this below was possible, but i deleted it and currently it doesn't work
                    //return syntax.DescendantNodes().OfType<IOperation>().FirstOrDefault(static operation =>
                    //    operation.Kind is OperationKind.Literal
                    //    or OperationKind.Binary
                    //    or OperationKind.Conversion
                    //    or OperationKind.Unary
                    //    or OperationKind.Conditional);
                }
            }
        }
    }
}
