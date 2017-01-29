namespace Scripty.Core
{
    /// <summary>
    ///      How to handle output attached to the executing script
    /// </summary>
    public enum OutputBehavior
    {
        /// <summary>
        ///      If script execution produces errors, the output should be left unaltered
        /// </summary>
        DontOverwriteIfEvaluationFails,

        // <summary>
        //     If script compilation produces errors, the output should be left unaltered
        // </summary> 
        //DontOverwriteIfCompilationFails, //for future

        /// <summary>
        ///     The script controls what to do with output by using the 
        /// <see cref=" ScriptOutput"/> value
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


    /// <summary>
    ///     If <see cref="OutputBehavior.ScriptControlsOutput"/> is set, this
    /// is how the script controls it 
    /// </summary>
    public enum ScriptOutput
    {
        Keep,
        Ignore
    }

}
