using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace TabulaLuma;

public static class Compiler
{
    /// <summary>
    /// Compiles a new ProgramBase-derived class with the given class name, id, and RunImpl body.
    /// Returns an instance of the compiled class as ProgramBase.
    /// </summary>
    public static object? CompileProgramBase(string className, int id, string runImplBodyRef, out Tuple<int,string>[] errors)
    {
        // Compose the full class source code
        string code = $$"""
using System;
using System.Collections.Generic;
using OpenCvSharp;
using Hexa.NET.SDL3;
using System.Drawing;
using TabulaLuma;

public class {{className}} : ProgramBase
{
    public override int Id => {{id}};
    public override string SourceCodeRef => @"{{runImplBodyRef}}";
    protected override void RunImpl()
    {
        {{string.Join("\n", Reference.Get<string[]>( runImplBodyRef))}}
        Claim($"({Id}) has codeRef '{SourceCodeRef}'");
    }
}
""";
        // Parse the code
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        // Gather all required references
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        // Ensure System.Drawing.Common is included
        var drawingPath = typeof(System.Drawing.Point).Assembly.Location;
        if (!references.Any(r => r.Display == drawingPath))
            references.Add(MetadataReference.CreateFromFile(drawingPath));

        var sharedPath = typeof(TabulaLuma.Reference).Assembly.Location;
        if (!references.Any(r => r.Display == sharedPath))
            references.Add(MetadataReference.CreateFromFile(sharedPath));

        // Compile
        var compilation = CSharpCompilation.Create(
            $"Dynamic_{className}_Assembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
    
        /// err begins with 2 numbers in brackets separated by a comma, representing line and column
        /// this function should retun the err string with the line number decremented by 'decrement'
        Tuple<int,string> AdjustLineNo(string err)
        {
            int decrement = 13;
            int startIdx = err.IndexOf('(');
            int commaIdx = err.IndexOf(',', startIdx);
            int endIdx = err.IndexOf(')', commaIdx);
            if (startIdx >= 0 && commaIdx > startIdx && endIdx > commaIdx)
            {
                string lineStr = err.Substring(startIdx + 1, commaIdx - startIdx - 1);
                if (int.TryParse(lineStr, out int lineNo))
                {
                    int newLineNo = lineNo - decrement;
                    return new Tuple<int,string> (newLineNo, err.Substring(0, startIdx + 1) + newLineNo.ToString() + err.Substring(commaIdx));
                }
            }
            return new Tuple<int,string>(-1, err);
        }

        if (!result.Success)
        {
            errors = result.Diagnostics.Select(d => AdjustLineNo(d.ToString())).ToArray();
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        var type = assembly.GetType(className);
        if (type == null)
            throw new Exception($"Type '{className}' not found in compiled assembly.");

        var instance = Activator.CreateInstance(type);
        if (instance == null)
            throw new Exception($"Could not create instance of '{className}'.");

        errors = new Tuple<int, string>[0];
        return instance;
    }

    public static bool Decompile(ProgramBase program, out string[] types, out string[] body, out string error)
    {
        body = [];
        types = [];
        error = string.Empty;
        var type = program.GetType();
        var assemblyPath = type.Assembly.Location;

        var resolver = new UniversalAssemblyResolver(
            assemblyPath,
            false,
            null
        );
        resolver.AddSearchDirectory(Assembly.GetExecutingAssembly().Location); 
        
        // Create the decompiler for the assembly
        var decompiler = new CSharpDecompiler(assemblyPath, resolver, new DecompilerSettings() { });
        // Find the RunImpl method using reflection
        var methodInfo = type.GetMethod("RunImpl", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (methodInfo == null)
        {
            error  = "RunImpl method not found.";
            return false;
        }
        // Get the metadata token for the method
        int metadataToken = methodInfo.MetadataToken;

        var handle = MetadataTokens.EntityHandle(metadataToken);

        if (handle == null)
        {
            error = "Could not find MethodDefinitionHandle for RunImpl.";
            return false;
        }

        // Decompile the method using the handle
        var text = decompiler.DecompileAsString(handle);

        TextReader reader = new StringReader(text);
        List<string> bodyLines = new List<string>();
        string? line;
        while((line = reader.ReadLine()) != null)
        {
            if(line.StartsWith("using"))
            {
                // collect using lines
                types = types.Append(line.Substring(6).TrimEnd(';')).ToArray();
            } 
            else if(line.TrimStart().StartsWith("protected override void RunImpl()"))
            {
                // next lines are the body
                // skip the method signature line
                line = reader.ReadLine(); // should be '{'
                while((line = reader.ReadLine()) != null)
                {
                    bodyLines.Add(line);
                }
                // remove the last line with the closing '}'
                bodyLines.RemoveAt(bodyLines.Count - 1);
               
                break;
            }
        }
        body = bodyLines.ToArray();
        return true;
    }
}