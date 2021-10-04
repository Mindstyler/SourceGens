namespace UnitySourceGenerators;

[Generator]
internal class EnumFastStringGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {

    }

    public void Execute(GeneratorExecutionContext context)
    {
        Dictionary<string, (EnumDeclarationSyntax declaration, string modifier, string genericTypeParameters)> enumInfo = new();

        foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
        {
            IEnumerable<EnumDeclarationSyntax> enums = syntaxTree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>()
                //this generator can only deal with public and internal enums because it writes extension methods
                .Where(static @enum => @enum.Modifiers.Any(static modifier =>
                    modifier.IsKind(SyntaxKind.PublicKeyword)
                    || modifier.IsKind(SyntaxKind.InternalKeyword)));

            foreach (EnumDeclarationSyntax @enum in enums)
            {
                IEnumerable<SyntaxNode> ancestorTree = @enum.Ancestors()
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

                modifiers.Add(@enum.Modifiers.Single(static modifier =>
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
                        ? typeDeclaration.Identifier.Text + ConcatGenericTypeParametersAndReturnNeededOnesForType(typeDeclaration)
                        : ((NamespaceDeclarationSyntax)syntaxNode).Name.ToString())
                    .Reverse()
                    .Concat(new[] { @enum.Identifier.Text })
                    .ToArray(); //this needs to be a call to ToArray() because it needs to be evaluated to populate 'genericTypeParameters'

                string ConcatGenericTypeParametersAndReturnNeededOnesForType(TypeDeclarationSyntax typeDeclaration)
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

                enumInfo.Add($"{string.Join(".", fullPath)}", (@enum, modifier, genericTypeParameters));
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
                foreach (EnumMemberDeclarationSyntax name in enumInfo[fullType].declaration.Members.Distinct(new EnumMemberComparer(context, enumInfo[fullType].declaration.Members)))
                {
                    sb.Append($"\n                {fullType}.{name.Identifier.Text} => nameof({fullType}.{name.Identifier.Text}),");
                }
                
                sb.Append("\n                _ => throw new System.ArgumentOutOfRangeException(nameof(@enum), @enum, null)\n            };\n        }");
            }

            sb.Append("\n    }\n}");
            
            return sb.ToString();
        }

        private sealed class EnumMemberComparer : IEqualityComparer<EnumMemberDeclarationSyntax>
        {
            private readonly GeneratorExecutionContext _context;
            private readonly SeparatedSyntaxList<EnumMemberDeclarationSyntax> _allEnumMembers;

            internal EnumMemberComparer(GeneratorExecutionContext context, SeparatedSyntaxList<EnumMemberDeclarationSyntax> allEnumMembers)
            {
                _context = context;
                _allEnumMembers = allEnumMembers;
            }

            public bool Equals(EnumMemberDeclarationSyntax left, EnumMemberDeclarationSyntax right) => (left.EqualsValue is not null || right.EqualsValue is not null) && (GetValue(left) == GetValue(right));

            public int GetHashCode(EnumMemberDeclarationSyntax enumMemberDeclarationSyntax) => GetValue(enumMemberDeclarationSyntax);

            private int GetValue(EnumMemberDeclarationSyntax enumMemberDeclarationSyntax)
            {
                return (enumMemberDeclarationSyntax.EqualsValue is not null)
                    ? int.Parse(GetPossibleOperations().ConstantValue.Value.ToString())
                    : GetValueThroughRecursion();

                int GetValueThroughRecursion()
                {
                    EnumMemberDeclarationSyntax? previous = _allEnumMembers.TakeWhile(current => current != enumMemberDeclarationSyntax).LastOrDefault();

                    return (previous is not null) ? GetValue(previous) + 1 : 0;
                }

                IOperation GetPossibleOperations()
                {
                    return _context.Compilation
                        .GetSemanticModel(enumMemberDeclarationSyntax.SyntaxTree)
                        .GetOperation(enumMemberDeclarationSyntax.ChildNodes().OfType<EqualsValueClauseSyntax>().Single())!.Children.Single();
                }
            }
        }
    }
}
