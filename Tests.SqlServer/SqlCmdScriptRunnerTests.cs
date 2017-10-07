using dotMigrator.SqlServer;
using NUnit.Framework;

namespace Tests.SqlServer
{
    [TestFixture]
    public class SqlCmdScriptRunnerTests
    {
        [Test]
        public void HappyPath()
        {
            string sampleScriptFilePath = null;
            var sut = new SqlCmdScriptRunner();
            sut.Run(sampleScriptFilePath);
        }
    }
}