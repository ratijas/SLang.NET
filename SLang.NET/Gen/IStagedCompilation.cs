namespace SLang.NET.Gen
{
    public interface IStagedCompilation
    {
        void Stage1CompileStubs();
        void Stage2CompileBody();
    }
}