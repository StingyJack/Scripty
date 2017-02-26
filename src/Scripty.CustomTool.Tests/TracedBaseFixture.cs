namespace Scripty.CustomTool.Tests
{
    using System.Diagnostics;
    using NUnit.Framework;

    /// <summary>
    ///     Allows tests to capture program trace data
    /// </summary>
    [TestFixture]
    public abstract class TracedBaseFixture
    {
        public static TracePlease TracePlease { get; set; } = new TracePlease();

        public abstract void OnOneTimeSetup();

        public abstract void OnOneTimeTearDown();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Trace.Listeners.Add(TracePlease);
            OnOneTimeSetup();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Trace.Listeners.Remove(TracePlease);
            OnOneTimeSetup();
        }
    }
}