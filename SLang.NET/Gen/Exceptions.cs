using System;
using SLang.IR;

namespace SLang.NET.Gen
{
    // TODO: improve diagnostics, including call site information and unified type system.
    public class CompilerException : Exception
    {
    }

    public class LoadFromLiteralException : CompilerException
    {
        public UnitReference Unit { get; }
        public string Literal { get; }

        public LoadFromLiteralException(UnitReference unit, string literal)
        {
            Unit = unit;
            Literal = literal;
        }

        public override string Message =>
            $@"Unable to parse ""{Unit}"" literal ""{Literal}""";
    }

    public class LiteralsNotSupported : CompilerException
    {
        public UnitReference Unit { get; }
        public string Literal { get; }

        public LiteralsNotSupported(UnitReference unit, string literal)
        {
            Unit = unit;
            Literal = literal;
        }

        public override string Message =>
            $@"Literals are not supported for unit ""{Unit}"": ""{Literal}""";
    }

    public class UnitNotFoundException : CompilerException
    {
        public Context Context { get; }
        public UnitReference Unit { get; }

        public UnitNotFoundException(Context ctx, UnitReference unit)
        {
            Context = ctx;
            Unit = unit;
        }

        public override string Message =>
            $"Unit not found: {Unit}";
    }

    public class RoutineNotFoundException : CompilerException
    {
        public Context Context { get; }
        public RoutineReference Routine { get; }

        public RoutineNotFoundException(Context ctx, RoutineReference routine)
        {
            Context = ctx;
            Routine = routine;
        }

        public override string Message =>
            $"Routine not found: {Routine}";
    }

    public class VariableNotFoundException : CompilerException
    {
        public Scope Scope { get; }
        public Identifier Name { get; }

        public VariableNotFoundException(Scope scope, Identifier name)
        {
            Scope = scope;
            Name = name;
        }

        public override string Message =>
            $"Variable {Name} not found in scope {Scope.Name}";
    }

    public class CompilationStageException : CompilerException
    {
        public Context Context { get; }
        public RoutineDefinition Routine { get; }
        public int Stage { get; }

        public CompilationStageException(Context ctx, RoutineDefinition routine, int stage = 0)
        {
            Context = ctx;
            Routine = routine;
            Stage = stage;
        }
    }

    public class ArityMismatchException : CompilerException
    {
        public RoutineDefinition Routine;
        public int Expected => Routine.SignatureReference.Parameters.Length;
        public int Actual { get; }

        public ArityMismatchException(RoutineDefinition routine, int actual)
        {
            Routine = routine;
            Actual = actual;
        }

        public override string Message =>
            $"{nameof(ArityMismatchException)} at {Routine} (expected: {Expected}, actual: {Actual})";
    }

    public class TypeMismatchException : CompilerException
    {
        public IHasType Expected { get; }

        public IHasType Actual { get; }

        public TypeMismatchException(IHasType expected, IHasType actual)
        {
            Expected = expected;
            Actual = actual;
        }

        public override string Message =>
            $"{nameof(TypeMismatchException)} (expected: {Expected}, actual: {Actual})";
    }

    public class UnresolvedReferenceException : CompilerException
    {
        public Reference Reference { get; }

        public UnresolvedReferenceException(Reference reference)
        {
            Reference = reference;
        }

        public override string Message =>
            $"{nameof(UnresolvedReferenceException)}: {Reference.Name}";
    }

    public class EmptyConditionalsException : CompilerException
    {
        public If Conditionals { get; }

        public EmptyConditionalsException(If conditionals)
        {
            Conditionals = conditionals;
        }

        public override string Message
            => "Conditional statement must have at least one condition/action pair";
    }
}