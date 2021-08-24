using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnitySourceGenerators
{
    [Generator]
    public class EnumFastStringGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            IEnumerable<EnumDeclarationSyntax> enums = Enumerable.Empty<EnumDeclarationSyntax>();
            IEnumerable<NamespaceDeclarationSyntax> namespaces = Enumerable.Empty<NamespaceDeclarationSyntax>();

            foreach (SyntaxTree? syntaxTree in context.Compilation.SyntaxTrees)
            {
                enums = enums.Concat(syntaxTree.GetRoot().DescendantNodes().OfType<EnumDeclarationSyntax>());
                namespaces = namespaces.Concat(enums.SelectMany(e => e.Ancestors().OfType<NamespaceDeclarationSyntax>()));
                //string n = string.Join(".", namespaces.Select(l => l.Name.ToString()));
            }

            enums = enums.Distinct(new Comp());
            namespaces = namespaces.Distinct(new CompNamespace());

            context.AddSource("EnumFastString", SourceText.From(Template.GetCode(enums, namespaces), Encoding.UTF8));
        }

        private class Comp : IEqualityComparer<EnumDeclarationSyntax>
        {
            public bool Equals(EnumDeclarationSyntax x, EnumDeclarationSyntax y) => x.Identifier.Text == y.Identifier.Text;

            public int GetHashCode(EnumDeclarationSyntax obj)
            {
                throw new NotImplementedException();
            }
        }

        private class CompNamespace : IEqualityComparer<NamespaceDeclarationSyntax>
        {
            public bool Equals(NamespaceDeclarationSyntax x, NamespaceDeclarationSyntax y) => x.Name == y.Name;

            public int GetHashCode(NamespaceDeclarationSyntax obj)
            {
                throw new NotImplementedException();
            }
        }

        private static class Template
        {
            internal static string GetCode(IEnumerable<EnumDeclarationSyntax> enums, IEnumerable<NamespaceDeclarationSyntax> namespaces)
            {
                StringBuilder sb = new();

                foreach (NamespaceDeclarationSyntax @namespace in namespaces)
                {
                    sb.Append($"\nusing {@namespace.Name};");
                }

                sb.Append("\n");
                sb.Append($@"
namespace SourceGenerated
{{
    public static class EnumExtensions
    {{");
                foreach (EnumDeclarationSyntax @enum in enums)
                {
                    sb.Append($@"
        public static string ToFastString(this {@enum.Identifier.Text} @enum)
        {{
            return @enum switch
            {{");
                    foreach (EnumMemberDeclarationSyntax name in @enum.Members)
                    {
                        sb.Append($"\n                {@enum.Identifier.Text}.{name.Identifier.Text} => nameof({@enum.Identifier.Text}.{name.Identifier.Text}),");
                    }

                    sb.Append("\n                _ => throw new ArgumentOutOfRangeException(nameof(@enum), @enum, null)");
                    sb.Append("\n            };\n        }");
                }

                sb.Append("\n    }\n}");

                return sb.ToString();
            }
        }
    }
}