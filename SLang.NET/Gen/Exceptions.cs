using System;

namespace SLang.NET.Gen
{
    public class CompilerException : Exception
    {
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
}