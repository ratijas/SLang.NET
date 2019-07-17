namespace SLang.NET.Gen
{
    public interface IStagedCompilation
    {
        /// <summary>
        /// Compile Routine stubs.
        /// </summary>
        /// <para>
        /// Generate <see cref="RoutineDefinition"/>s and their CIL method definitions.
        /// After this stage it is possible to reference any routine, generate calls etc.
        /// </para>
        void Stage1RoutineStubs();


        /// <summary>
        /// Compile Routine body.
        /// </summary>
        /// <para>
        /// Generate CIL instructions for routine body
        /// </para>
        void Stage2RoutineBody();
    }
}