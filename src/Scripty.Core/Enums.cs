namespace Scripty.Core
{
    using Output;

    /// <summary>
    ///      How to handle output attached to the executing script
    /// </summary>
    public enum OutputBehavior
    {
        /// <summary>
        ///      If script execution produces errors, the output should be left unaltered. This is the default.
        /// </summary>
        DontOverwriteIfEvaluationFails,
        
        /// <summary>
        ///     The script controls what to do with output by using the 
        /// <see cref=" OutputFile.KeepOutput"/> value
        /// </summary>
        ScriptControlsOutput,

        /// <summary>
        ///     Saving script ouput is never attempted. 
        /// </summary>
        /// <remarks>
        ///     May be useful when using scripty in a macro or an easy ide extensibility fashion
        /// </remarks>
        NeverGenerateOutput
    }
}
