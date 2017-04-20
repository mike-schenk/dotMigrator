using System;
using System.Collections.Generic;
using System.Linq;

namespace dotMigrator
{
	public class Migrator
	{
		private readonly IJournal _journal;
		private readonly IMigrationsProvider _migrationsProvider;
		private readonly IProgressReporter _progressReporter;

		private MigrationPlan _migrationPlan;

		public Migrator(
			IJournal journal, 
			IMigrationsProvider migrationsProvider, 
			IProgressReporter progressReporter)
		{
			_journal = journal;
			_migrationsProvider = migrationsProvider;
			_progressReporter = progressReporter;
		}

		public void EnsureJournal()
		{
			_journal.CreateJournal();
		}

		public void EnsureBaseline(string migrationName)
		{
			_journal.SetBaseline(migrationName, _migrationsProvider);
		}

		/// <summary>
		/// Determines if the target data store is compatible with the migrations, and if so, which of them need to run to bring it up-to-date
		/// </summary>
		/// <returns></returns>
		public MigrationPlan Plan()
		{
			if (_migrationPlan != null)
				return _migrationPlan;

			int lastMigrationNumber = 0;
			bool compatible = true;

			List<Migration> migrationsToRun = new List<Migration>();

			// now compare the scripts that have been run against the scripts we have
			var migrationsAlreadyRun = _journal.GetDeployedMigrations();
			// ask the RevisedScriptsSource for a sequence of available migration scripts
			var availableMigrations = _migrationsProvider.GatherMigrations();

			using (var deployedEnumerator = migrationsAlreadyRun.GetEnumerator())
			using (var availableEnumerator = availableMigrations.GetEnumerator())
			{
				// step through the ordered list of deployed migrations and available migrations in tandem to make sure we can safely run just the "new" available migrations.
				while (deployedEnumerator.MoveNext())
				{
					var deployedMigration = deployedEnumerator.Current;
					lastMigrationNumber = deployedMigration.MigrationNumber;

					// If we have more deployed migrations than we have available migrations, there must be some unknown ones and we can't run.
					if (!availableEnumerator.MoveNext())
					{
						throw new RebuildNeededException($"There are unknown migrations already deployed, starting with {deployedMigration.MigrationNumber}:{deployedMigration.Name}");
					}
					var availableMigration = availableEnumerator.Current;

					// now make sure the pair of migrations match
					var comparison = Compare(deployedMigration, availableMigration);
					switch (comparison)
					{
						// There is either an available migration that hasn't been deployed and comes before the end of the deployed ones, 
						// or there is a deployed one that isn't present in the available ones.
						case MigrationComparison.Mismatched:
							throw new RebuildNeededException($"The set of deployed migrations and available migrations don't match, starting with Deployed: {deployedMigration.MigrationNumber}:{deployedMigration.Name} and Available: {availableMigration.MigrationNumber}:{availableMigration.Name}");
						// A migration is different than it was when it was deployed.
						case MigrationComparison.Revised:
							throw new RebuildNeededException($"A migration has been modified since it was last deployed: {deployedMigration.MigrationNumber}:{deployedMigration.Name}");
						case MigrationComparison.Equal:
							break;
					}
					// if there are any deployed offline migrations that are incomplete, we can't run.
					if (deployedMigration.Complete == false && availableMigration.IsOnlineMigration == false)
					{
						throw new RepairNeededException($"A migration script was not completed on the last run: {deployedMigration.MigrationNumber}:{deployedMigration.Name}");
					}
				}
				// if the last deployed migration is an incomplete online migration, we can't run any offline migrations

				// If we got this far, everything left over in availableEnumerator needs to be executed.
				while (availableEnumerator.MoveNext())
				{
					var availableMigration = availableEnumerator.Current;
					migrationsToRun.Add(availableMigration);
					lastMigrationNumber = availableMigration.MigrationNumber;
				}
			}
			// If there are any duplicate MigrationNumbers among the available migrations, we can't run.
			if (migrationsToRun.GroupBy(m => m.MigrationNumber).Any(g => g.Count() > 1))
				throw new CannotMigrateException("There are duplicate migration numbers among the available migrations.");
			// If there are any duplicate MigrationNames among the available migrations, we can't run.
			if (migrationsToRun.GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
				throw new CannotMigrateException("There are duplicate migration names among the available migrations.");


			// determine which repeatable scripts need to run
			var repeatableScriptsAlreadyRun = _journal.GetDeployedStoredCodeDefinitions().ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
			var availableRepeatableScripts = _migrationsProvider.GatherStoredCodeDefinitions();
			// find the differences
			var repeatableScriptsToRun = availableRepeatableScripts
				.Where(availableScript =>
				{
					DeployedStoredCodeDefinition found;
					if (repeatableScriptsAlreadyRun.TryGetValue(availableScript.Name, out found) && found.Fingerprint == availableScript.Fingerprint)
					{
						return false;
					}
					return true;
				})
				.ToList();

		}

		private MigrationComparison Compare(DeployedMigration deployed, Migration available)
		{
			if (available.MigrationNumber != deployed.MigrationNumber
			    || string.Equals(available.Name, deployed.Name, StringComparison.OrdinalIgnoreCase) == false)
				return MigrationComparison.Mismatched;
			if (available.Fingerprint != deployed.Fingerprint)
				return MigrationComparison.Revised;
			return MigrationComparison.Equal;
		}

		public void MigrateOffline()
		{
			Plan();

			if (_migrationPlan.HasOfflineMigrations)
			{
				_progressReporter.BeginBlock("Running offline migration scripts...");
				foreach (var migrationToRun in _migrationPlan.OfflineMigrations)
				{
					_progressReporter.Report($"Running {migrationToRun.Name} ...");
					_journal.RecordStartMigration(migrationToRun);
					migrationToRun.Execute(_progressReporter);
					_journal.RecordCompleteMigration(migrationToRun);
					_progressReporter.Report("Done.");
				}
				_progressReporter.EndBlock("Done.");
			}
			else
			{
				_progressReporter.Report("No offline migrations to run.");
			}

			if (_migrationPlan.HasStoredCodeChanges)
			{
				_progressReporter.BeginBlock("Running stored code definitions...");
				foreach (var scriptToRun in _migrationPlan.StoredCodeDefinitions)
				{
					_progressReporter.Report($"Running {scriptToRun.Name} ...");
					// we'll always run the script in the prescribed database
					scriptToRun.Execute(_progressReporter);

					// Record the fact that we ran that script.
					_journal.RecordStoredCodeDefinition(scriptToRun, _migrationPlan.LastMigrationNumber);
					_progressReporter.Report("Done.");
				}
				_progressReporter.EndBlock("Done.");
			}
			else
			{
				_progressReporter.Report("No stored code definitions to update.");
			}
		}

		public void MigrateOnline()
		{
			Plan();

			if (_migrationPlan.HasOnlineMigrations)
			{
				_progressReporter.BeginBlock("Running online migration scripts...");
				foreach (var migrationToRun in _migrationPlan.OnlineMigrations)
				{
					_progressReporter.Report($"Running {migrationToRun.Name} ...");
					_journal.RecordStartMigration(migrationToRun);
					migrationToRun.Execute(_progressReporter);
					_journal.RecordCompleteMigration(migrationToRun);
					_progressReporter.Report("Done.");
				}
				_progressReporter.EndBlock("Done.");
			}
			else
			{
				_progressReporter.Report("No online migration scripts to run.");
			}
		}
	}

	internal enum MigrationComparison
	{
		Mismatched,
		Revised,
		Equal
	}

	public interface IMigrationsProvider
	{
		IEnumerable<Migration> GatherMigrations();
		IEnumerable<StoredCodeDefinition> GatherStoredCodeDefinitions();
	}
}
