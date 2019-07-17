namespace SLang.NET.Test
{
    public enum Status
    {
        /// <summary>
        /// Default status is unknown.
        /// </summary>
        Invalid,
        /// <summary>
        /// Test is currently running.
        /// </summary>
        Running,
        /// <summary>
        /// Test successfully passed.
        /// </summary>
        Passed,
        /// <summary>
        /// Test failed.
        /// </summary>
        Failed,
        /// <summary>
        /// Test will not be executed because it was marked as skipped.
        /// </summary>
        Skipped,
    }
}