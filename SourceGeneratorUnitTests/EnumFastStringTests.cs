using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitySourceGenerators.Tests;

[TestClass]
public class EnumFastStringGeneratorTests
{
    [TestMethod]
    public void EnumFastStringTest()
    {
        Compilation inputCompilation = CreateCompilation(
@"");

        EnumFastStringGenerator generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        //File.WriteAllText(@"C:\Users\flott\Desktop\DriverResult.cs", runResult.Results.Where(e => e.Generator.GetType() == typeof(EnumFastStringGenerator)).First().GeneratedSources.First().ToString());

        string s = string.Empty;

        foreach (SyntaxTree tree in outputCompilation.SyntaxTrees)
        {
            s += tree.GetText().ToString();
        }

        File.WriteAllText(@"C:\Users\flott\Desktop\GeneratorResult.cs", s);

        //Debug.Assert(diagnostics.IsEmpty);
        Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2);
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\GeneratedCompilation.cs") /*source*/) },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}