namespace Scripty.CustomTool.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class DteHelperTests
    {
        [Test]
        public void TestGetDte()
        {
            var dte = new DteHelper().GetDteVs14();

            Assert.IsNotNull(dte);
        }
    }
}