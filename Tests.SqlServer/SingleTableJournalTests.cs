using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dotMigrator;
using dotMigrator.SqlServer;
using Moq;
using NUnit.Framework;

namespace Tests.SqlServer
{
    [TestFixture]
    public class SingleTableJournalTests
    {
        [Test]
        public void HappyPath()
        {
            var localConnectionProperties = new ConnectionProperties("(local)", "dotMigratorTests");
            SqlTest.EnsureNewDatabase(localConnectionProperties);

            using (var sut = new SingleTableJournal(localConnectionProperties, Mock.Of<IProgressReporter>()))
            {

                sut.Open();

                sut.CreateJournal();

                var migs = sut.GetDeployedMigrations();
                Assert.That(() => migs.Count == 0);

                var objs = sut.GetDeployedStoredCodeDefinitions();
                Assert.That(() => objs.Count == 0);

                //sut.RecordStartMigration();
            }
        }
    }
}
