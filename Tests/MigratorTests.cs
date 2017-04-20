using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using dotMigrator;
using Moq;
using NUnit.Framework;

namespace Tests
{
	[TestFixture]
	public class MigratorTests
	{
		private readonly Migration _onlineMigration = new Migration(2, "Online Migration", "1234567890", true, pr => { });
		private readonly Migration _offlineMigration = new Migration(1, "Offline Migration", "1234567890", false, pr => { });

		[Test]
		public void EnsureJournal_ShouldCallCreateJournal()
		{
			var mockJournal = new FakeJournal();
			var subject = new Migrator(mockJournal, Mock.Of<IMigrationsProvider>(), Mock.Of<IProgressReporter>());

			subject.EnsureJournal();

			Assert.That(mockJournal.IsCreated);
		}

		[Test]
		public void EnsureBaseline_ShouldCallSetBaseline()
		{
			var mockJournal = new Mock<IJournal>();
			var migrationsProvider = new FakeMigrationsProvider();
			var subject = new Migrator(mockJournal.Object, migrationsProvider, Mock.Of<IProgressReporter>());

			subject.EnsureBaseline("SomeMigration");

			mockJournal.Verify(m => m.SetBaseline("SomeMigration", migrationsProvider), Times.Once);
		}

		[Test]
		public void Plan_ShouldNotReturnAnyError_WhenThereAreNoMigrations()
		{
			// return empty lists
			var mockProvider = new FakeMigrationsProvider();
			var emptyJournal = new FakeJournal().Created();

			var subject = new Migrator(emptyJournal, mockProvider, Mock.Of<IProgressReporter>());

			var planResult = subject.Plan();
			
			Assert.That(planResult.OnlineErrorMessage, Is.Null);
			Assert.That(planResult.OfflineErrorMessage, Is.Null);
			Assert.That(planResult.HasOfflineMigrations, Is.False);
			Assert.That(planResult.HasOnlineMigrations, Is.False);
			Assert.That(planResult.HasStoredCodeChanges, Is.False);
		}

		//TODO: test that when there are offline migrations that need to follow online migrations, we get an incompatibility error.
		//TODO: test that when we plan to run online migrations during offline, that we don't get the incompatibility error.

		[Test]
		public void Plan_ShouldReturnAnError_WhenTheLastDeployedMigrationIsAnIncompleteOfflineMigration()
		{
			var mockProvider = new FakeMigrationsProvider().WithMigrations(new[] { _offlineMigration });
			var journal = new FakeJournal().Created().WithMigrations(new[] { _offlineMigration.AsDeployedMigration(false) });

			// even if we tell it to include online migrations during an offline migration.
			var subject = new Migrator(journal, mockProvider, Mock.Of<IProgressReporter>(), true);

			var planResult = subject.Plan();

			Assert.That(planResult.OfflineErrorMessage, Is.Not.Null.And.Contains("incomplete prior offline migration."));
			Assert.That(planResult.OnlineErrorMessage, Is.EqualTo(planResult.OfflineErrorMessage));
		}


		[Test]
		public void Plan_ShouldNotReturnAnyError_WhenTheLastDeployedMigrationIsAnIncompleteOnlineMigration_AndThereAreNoOfflineMigrationsToRun()
		{
			var mockProvider = new FakeMigrationsProvider().WithMigrations(new [] {_onlineMigration});
			var journal = new FakeJournal().Created().WithMigrations(new [] {_onlineMigration.AsDeployedMigration(false)});

			var subject = new Migrator(journal, mockProvider, Mock.Of<IProgressReporter>());

			var planResult = subject.Plan();

			Assert.That(planResult.OnlineErrorMessage, Is.Null);
			Assert.That(planResult.OfflineErrorMessage, Is.Null);
			Assert.That(planResult.HasOfflineMigrations, Is.False);
			Assert.That(planResult.HasOnlineMigrations, Is.True, "there should be an online migration that needs to resume");
			Assert.That(planResult.HasStoredCodeChanges, Is.False);
		}

	}
}
