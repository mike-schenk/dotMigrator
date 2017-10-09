using System;
using System.IO;
using System.Text;
using dotMigrator.SqlServer;
using Moq;
using NUnit.Framework;

namespace Tests.SqlServer
{
    [TestFixture]
    public class SqlCmdScriptRunnerTests
    {
        [Test]
        public void HappyPath()
        {
            // Arrange

            // using a random database name to ensure the SUT uses the same DB
            var localConnectionProperties = new ConnectionProperties("(local)", "dotMigratorTests" + new Random().Next());
            using (SqlTest.EnsureNewDatabase(localConnectionProperties))
            {

                var guid = Guid.NewGuid().ToString();
                var sampleScriptContent = $@"
CREATE TABLE [dbo].[SqlCmdScriptRunner] (
    id int not null identity primary key,
    value varchar(36) not null
)
GO
INSERT INTO [dbo].[SqlCmdScriptRunner] (value) VALUES ('{guid}')
GO";
                string sampleScriptFilePath = Path.GetTempFileName();
                File.WriteAllText(sampleScriptFilePath, sampleScriptContent, Encoding.ASCII);

                var sut = new SqlCmdScriptRunner(localConnectionProperties);

                // Act
                sut.Run(sampleScriptFilePath);

                // Assert
                var assertionConnection = localConnectionProperties.OpenConnection();
                var checkCommand = assertionConnection.CreateCommand();
                checkCommand.CommandText = "SELECT * FROM [dbo].[SqlCmdScriptRunner]";
                var rdr = checkCommand.ExecuteReader();
                Assert.That(rdr.Read(), Is.True);
                Assert.That(rdr.GetString(1), Is.EqualTo(guid));
            }
        }

        [Test]
        public void ShouldUseCredentialsFromConnectionProperties()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void ShouldIncludeDetailsInExceptions()
        {
            // we want the error message itself from SQL Server as well as the last n lines from the script (which have been echoed out)
            // Arrange

            // using a random database name to ensure the SUT uses the same DB
            var localConnectionProperties = new ConnectionProperties("(local)", "dotMigratorTests" + new Random().Next());
            using (SqlTest.EnsureNewDatabase(localConnectionProperties))
            {

                var guid = Guid.NewGuid().ToString();
                var sampleScriptContent = $@"
CREATE TABLE [dbo].[SqlCmdScriptRunner] (
    id int not null identity primary key,
    stmt varchar(2) not null,
    value varchar(36) not null
)
GO
--BEGIN TRY
INSERT INTO [dbo].[SqlCmdScriptRunner] (value, stmt) VALUES ('{guid}', 1)
INSERT INTO [dbo].[SqlCmdScriptRunner] (value, stmt) VALUES ('{guid}b', 2)
INSERT INTO [dbo].[SqlCmdScriptRunner] (value, stmt) VALUES ('{guid}c', 3)
--END TRY
--BEGIN CATCH
--    THROW
--END CATCH
GO
INSERT INTO [dbo].[SqlCmdScriptRunner] (value, stmt) VALUES ('{guid}d', 4)
INSERT INTO [dbo].[SqlCmdScriptRunner] (value, stmt) VALUES ('{guid}e', 5)
GO";
                string sampleScriptFilePath = Path.GetTempFileName();
                File.WriteAllText(sampleScriptFilePath, sampleScriptContent, Encoding.ASCII);

                var sut = new SqlCmdScriptRunner(localConnectionProperties);

                // Act
                sut.Run(sampleScriptFilePath);

                // we want to see that statements 1, 2, and 3 are included in the error result because 2 failed and 3 is in its same batch
                // statements 4 and 5 should _not_ be included in the error result
            }
            throw new NotImplementedException();
        }
    }
}