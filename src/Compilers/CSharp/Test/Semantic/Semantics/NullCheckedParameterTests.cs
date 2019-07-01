﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) null-checked variables and lambdas.
    /// </summary>
    public class NullCheckedParameterTests : CompilingTestBase
    {
        [Fact]
        public void NullCheckedDelegateDeclaration()
        {
            var source = @"
delegate void Del(int x!, int y);
class C
{
    Del d = delegate(int k!, int j) { /* ... */ };
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (2,23): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    // delegate void Del(int x!, int y);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(2, 23));
        }

        [Fact]
        public void NullCheckedAbstractMethod()
        {
            var source = @"
abstract class C
{
    abstract public int M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,31): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     abstract public int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 31));
        }

        [Fact]
        public void NullCheckedInterfaceMethod()
        {
            var source = @"
interface C
{
    public int M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,22): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 22));
        }

        [ConditionalFact(typeof(MonoOrCoreClrOnly))]
        public void NullCheckedInterfaceMethod2()
        {
            var source = @"
interface C
{
    public void M(int x!) { }
}";
            var compilation = CreateCompilation(source, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetStandardLatest,
                                                         parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedAutoProperty()
        {
            var source = @"
abstract class C
{
    string FirstName! { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,12): warning CS0169: The field 'C.FirstName' is never used
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.WRN_UnreferencedField, "FirstName").WithArguments("C.FirstName").WithLocation(4, 12),
                    // (4,21): error CS1003: Syntax error, ',' expected
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(4, 21),
                    // (4,25): error CS1002: ; expected
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, "get").WithLocation(4, 25),
                    // (4,28): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28),
                    // (4,28): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 28),
                    // (4,33): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 33),
                    // (4,33): error CS1519: Invalid token ';' in class, struct, or interface member declaration
                    //     string FirstName! { get; set; }
                    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(4, 33),
                    // (5,1): error CS1022: Type or namespace definition, or end-of-file expected
                    // }
                    Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(5, 1));
        }

        [Fact]
        public void NullCheckedPartialMethod()
        {
            var source = @"
partial class C
{
    partial void M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,24): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     partial void M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(4, 24));
        }

        [Fact]
        public void NullCheckedInterfaceProperty()
        {
            var source = @"
interface C
{
    public string this[int index!] { get; set; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,28): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public string this[int index!] { get; set; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 28));
        }

        [Fact]
        public void NullCheckedAbstractProperty()
        {
            var source = @"
abstract class C
{
    public abstract string this[int index!] { get; }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (4,37): error CS8714: Parameter 'index' can only have exclamation-point null checking in implementation methods.
                    //     public abstract string this[int index!] { get; }
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "index").WithArguments("index").WithLocation(4, 37));
        }

        [Fact]
        public void NullCheckedIndexedProperty()
        {
            var source = @"
class C
{
    public string this[int index!] => null;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourcePropertySymbol>("this[]");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedExternMethod()
        {
            var source = @"
using System.Runtime.InteropServices;
class C
{
    [DllImport(""User32.dll"")]
    public static extern int M(int x!);
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (6,36): error CS8714: Parameter 'x' can only have exclamation-point null checking in implementation methods.
                    //     public static extern int M(int x!);
                    Diagnostic(ErrorCode.ERR_MustNullCheckInImplementation, "x").WithArguments("x").WithLocation(6, 36));
        }

        [Fact]
        public void NullCheckedMethodDeclaration()
        {
            var source = @"
class C
{
    void M(string name!) { }
    void M2(string x) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);

            var m2 = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M2");
            Assert.False(((SourceParameterSymbol)m2.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void FailingNullCheckedArgList()
        {
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1, 'M'));
    }
    void M2(__arglist!)
    {
    }
}";
            CreateCompilation(source).VerifyDiagnostics( 
                    // (8,22): error CS1003: Syntax error, ',' expected
                    //     void M2(__arglist!)
                    Diagnostic(ErrorCode.ERR_SyntaxError, "!").WithArguments(",", "!").WithLocation(8, 22));
        }

        [Fact]
        public void NullCheckedArgList()
        {
            var source = @"
class C
{
    void M()
    {
        M2(__arglist(1!, 'M'));
    }
    void M2(__arglist)
    {
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalNullParameter()
        {
            var source = @"
class C
{
    void M(string name! = null) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            // Warning attached in emit diagnostics.
            comp.VerifyEmitDiagnostics(
                    // (4,19): error CS8719: Parameter 'string' is null-checked but is null by default.
                    //     void M(string name! = null) { }
                    Diagnostic(ErrorCode.WRN_NullCheckedHasDefaultNull, "name").WithArguments("string").WithLocation(4, 19));

            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalStringLiteralParameter()
        {
            var source = @"
class C
{
    void M(string name! = ""rose"") { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedMethodValidationWithOptionalParameterSplitBySpace()
        {
            var source = @"
class C
{
    void M(string name! =null) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>("M");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedConstructor()
        {
            var source = @"
class C
{
    public C(string name!) { }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("C").GetMember<SourceMethodSymbol>(".ctor");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedOperator()
        {
            var source = @"
class Box 
{ 
    public static int operator+ (Box b!, Box c)  
    { 
        return 2;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var m = comp.GlobalNamespace.GetTypeMember("Box").GetMember<SourceUserDefinedOperatorSymbol>("op_Addition");
            Assert.True(((SourceParameterSymbol)m.Parameters[0]).IsNullChecked);
            Assert.False(((SourceParameterSymbol)m.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaParameter()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<string, string> func1 = x! => x + ""1"";
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            SimpleLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                    .GetMember<SourceMethodSymbol>("M")
                                                                    .GetNonNullSyntaxNode()
                                                                    .DescendantNodes()
                                                                    .OfType<SimpleLambdaExpressionSyntax>()
                                                                    .Single();
            var lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaWithMultipleParameters()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int, bool> func1 = (x!, y) => x == y;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            var lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
            Assert.False(((SourceParameterSymbol)lambdaSymbol.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaSingleParameterInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int> func1 = (x!) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            Syntax.ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                                  .GetMember<SourceMethodSymbol>("M")
                                                                                  .GetNonNullSyntaxNode()
                                                                                  .DescendantNodes()
                                                                                  .OfType<Syntax.ParenthesizedLambdaExpressionSyntax>()
                                                                                  .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaSingleParameterNoSpaces()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int> func1 = x!=> x;
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                    // (7,33): error CS8713: Space required between '!' and '=' here.
                    //         Func<int, int> func1 = x!=> x;
                    Diagnostic(ErrorCode.ERR_NeedSpaceBetweenExclamationAndEquals, "!=").WithLocation(7, 33));
        }

        [Fact]
        public void NullCheckedLambdaSingleTypedParameterInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int> func1 = (int x!) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedLambdaManyTypedParametersInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int, int> func1 = (int x!, int y) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
            Assert.False(((SourceParameterSymbol)lambdaSymbol.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void LambdaManyNullCheckedParametersInParentheses()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int, int> func1 = (int x!, int y!) => x;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                           .GetMember<SourceMethodSymbol>("M")
                                                           .GetNonNullSyntaxNode()
                                                           .DescendantNodes()
                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[1]).IsNullChecked);
        }

        [Fact]
        public void NullCheckedDiscard()
        {
            var source = @"
using System;
class C
{
    public void M()
    {
        Func<int, int> func1 = (_!) => 42;
    }
}";
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics();

            CSharpSemanticModel model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ParenthesizedLambdaExpressionSyntax node = comp.GlobalNamespace.GetTypeMember("C")
                                                                           .GetMember<SourceMethodSymbol>("M")
                                                                           .GetNonNullSyntaxNode()
                                                                           .DescendantNodes()
                                                                           .OfType<ParenthesizedLambdaExpressionSyntax>()
                                                                           .Single();
            LambdaSymbol lambdaSymbol = (LambdaSymbol)model.GetSymbolInfo(node).Symbol;
            Assert.True(((SourceParameterSymbol)lambdaSymbol.Parameters[0]).IsNullChecked);
        }
    }
}
