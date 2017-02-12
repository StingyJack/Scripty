namespace Scripty.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Resolvers;

    public class ScriptSource
    {
        //TODO:This may require either a linked list or some other tree
        // structure so that related scripts can all be better identified.

        public string FilePath { get; }
        public string Code { get; }
        public List<ScriptDirective> Directives { get; private set; } = new List<ScriptDirective>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ScriptSource"/> class.
        /// </summary>
        /// <remarks>
        ///     Script source needs to have file pathing in addition to the code in 
        /// order to report diagnostics correctly.
        /// </remarks>
        /// <param name="filePath">The file path.</param>
        /// <param name="code">The code.</param>
        /// <exception cref="ArgumentException">
        ///     Value cannot be null or empty - filePath
        ///         or
        ///     The file path must be rooted - filePath
        /// </exception>
        /// <exception cref="ArgumentNullException">code</exception>
        public ScriptSource(string filePath, string code)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(filePath));
            }

            if (Path.IsPathRooted(filePath) == false)
            {
                //The Pathesolver may be able to locate this root, but the caller could use it too
                throw new ArgumentException("The file path must be rooted", nameof(filePath));
            }
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            FilePath = filePath;
            Code = code;
        }
    }
}