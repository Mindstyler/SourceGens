using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace UnitySourceGenerators.Tests;

[TestClass]
public class GeneratorTests
{
    [TestMethod]
    public void AutoDisposeTest()
    {
        Compilation inputCompilation = CreateCompilation(
@"namespace SourceGeneratorTests
{
    public partial class UnitTests : SystemBase
    {
        private NativeArray<float> coll;
        public NativeList<int> dis;

        protected override void OnCreate()
        {

        }

        protected override void OnUpdate()
        {

        }

        protected override void OnDestroy()
        {

        }
    }

    public class SystemBase
    {
        protected virtual void OnCreate()
        {

        }

        protected virtual void OnUpdate()
        {

        }

        protected virtual void OnDestroy()
        {

        }
    }

    public class NativeArray<T>
    {
        public void Dispose()
        {

        }
    }

    public class NativeList<T>
    {
        public void Dispose()
        {

        }
    }
}");

        AutoDisposePersistentNativeCollections generator = new();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();

        string s = string.Empty;

        foreach (SyntaxTree tree in outputCompilation.SyntaxTrees)
        {
            s += tree.GetText().ToString();
        }

        File.WriteAllText(@"C:\Users\flott\Desktop\GeneratorResult.cs", s);

        //Debug.Assert(diagnostics.IsEmpty);
        //Debug.Assert(outputCompilation.SyntaxTrees.Count() == 2);
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("compilation",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
    }
}
