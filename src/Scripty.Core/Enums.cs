namespace Scripty.Core
{

    /// <summary>
    ///      How to handle output attached to the executing script
    /// </summary>
    public enum OnScriptGenerateOutputBehavior
    {
        /// <summary>
        ///     is always discarded and/or overwritten
        /// </summary>
        AlwaysOverwriteOutput,

        // not compiling yet
        //DontOverwriteIfCompilationFails, 

        /// <summary>
        ///     If script execution produces errors, the output should be left unaltered
        /// </summary>
        DontOverwriteIfExecutionFails,

        /// <summary>
        ///     The script controls what to do with output by using the 
        /// <see cref=" ScriptEngine.OutputBehavior"/>
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
    ///     If <see cref="OnScriptGenerateOutputBehavior.ScriptControlsOutput"/> is set, this
    /// is how the script controls it 
    /// </summary>
    public enum ScriptOutput
    {
        Keep,
        Ignore
    }

}
