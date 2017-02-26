namespace Scripty.CustomTool.Tests
{
    using System;
    using System.Diagnostics;

    public class TracePlease : TraceListener
    {
        public override void Write(string message)
        {
            Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}