using System;

namespace Scripty.Core.Tests.TestCs
{
    using System.Data;

    /// <summary>
    ///     This example class is for testing the <see cref="Resolvers.CsRewriter"/>
    /// </summary>
    public class ReferencedClass
    {
        //do these need to be renamed to avoid conflicts?
        private string _someString;
        public Object _EwPublicVariable;

        /// <summary>
        ///     Some way to tag this or another like it as "These are specifically for scripty
        ///     to remove also" may be needed.
        /// </summary>
        public ScriptContext Context { get; set; }

        public ReferencedClass()
        {
            _someString = "Value";
        }

        public int AutoPropertySomewhereElse { get; set; }

        /*
         Odd block of text in between members
             
             */

        public void Owl(string message)
        {
            Context.Output.WriteLine($"{message} - {_someString}");
        }

        public int PropertyWithBackingField
        {
            /*
         Odd block of text inside member
             
             */
            get
            {
                return _backingField;
            }
            set { _backingField = value; }
        }

        private int _backingField;

        /// <summary>
        ///     A private class
        /// </summary>
        private class privClass
        {
            public int MyProperty { get; set; }
        }

        public DataSet GetDataSet()
        {
            return new DataSet();
        }

    }
}
