using System;
using System.Collections.Generic;
using System.Linq;

namespace dotMigrator
{
	/// <summary>
	/// The main entrypoint class to dotMigrator.
	/// Each instance of this class can be used to Plan and apply a set of migrations once.
	/// </summary>
	public class Migrator
	{
		private readonly IJournal _journal;
		private readonly IMigrationsProvider _migrationsProvider;
		private readonly IProgressReporter _progressReporter;
		private readonly bool _includeOnlineMigrationsDuringOffline;

		private MigrationPlan _migrationPlan;
		private IReadOnlyList<DeployedMigration> _deployedMigrations;
		private IReadOnlyList<Migration> _availableMigrations;

		public Migrator(
			IJournal journal, 
			IMigrationsProvider migrationsProvider, 
			IProgressReporter progressReporter,
			bool includeOnlineMigrationsDuringOffline = false)
		{
			_journal = journal;
			_migrationsProvider = migrationsProvider;
			_progressReporter = progressReporter;
			_includeOnlineMigrationsDuringOffline = includeOnlineMigrationsDuringOffline;
		}

		/// <summary>
		/// Instructs the journal to prepare its storage space if needed.
		/// </summary>
		public void EnsureJournal()
		{
			_journal.CreateJournal();
		}

		/// <summary>
		/// Populates the journal with the migrations that have already been applied 
		/// to the target database before dotMigrator was in use.
		/// </summary>
		/// <param name="baselineMigrationName"></param>
		public void EnsureBaseline(string baselineMigrationName)
		{
			var deployedMigrations = GetDeployedMigrations();
			if (deployedMigrations.Count == 0)
			{
				var baselineMigrations =
					GetAvailableMigrations()
						.TakeUntil(m => m.Name.Equals(baselineMigrationName, StringComparison.OrdinalIgnoreCase));
				_journal.SetBaseline(baselineMigrations);
			}
			/* otherwise, the subset of available migrations up to the baselineMigrationName must 
			 * match the first deployed migrations.. but that will be checked when calling Plan()
			 */
		}

		/// <summary>
		/// Determines if the target data store is compatible with the migrations, and 
		/// if so, which of them need to run to bring it up-to-date
		/// </summary>
		/// <returns></returns>
		public MigrationPlan Plan()
		{
			return _migrationPlan ?? (_migrationPlan = CreateMigrationPlan());
		}

		/// <summary>
		/// Applies all of the necessary offline migrations followed by the 
		/// changed stored code definitions
		/// </summary>
		public void MigrateOffline()
		{
			Plan();
			if (_migrationPlan.OfflineErrorMessage != null)
				throw new Exception(_migrationPlan.OfflineErrorMessage);

			var migrationNumberForStoredObjects = _migrationPlan.LastCompletedMigrationNumber;

			if (_migrationPlan.OfflineMigrations.Any())
			{
				_progressReporter.BeginBlock("Running offline migration scripts...");
				foreach (var migrationToRun in _migrationPlan.OfflineMigrations)
				{
					_progressReporter.Report($"Running {migrationToRun.Name} ...");
					_journal.RecordStartMigration(migrationToRun);
					migrationToRun.Execute(_progressReporter);
					migrationNumberForStoredObjects = migrationToRun.MigrationNumber;
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
					scriptToRun.Execute(_progressReporter);

					// Record the fact that we ran that script.
					_journal.RecordStoredCodeDefinition(scriptToRun, migrationNumberForStoredObjects);
					_progressReporter.Report("Done.");
				}
				_progressReporter.EndBlock("Done.");
			}
			else
			{
				_progressReporter.Report("No stored code definitions to update.");
			}
		}

		/// <summary>
		/// Applies all of the necessary online migrations
		/// </summary>
		public void MigrateOnline()
		{
			Plan();
			if (_migrationPlan.OnlineErrorMessage != null)
				throw new Exception(_migrationPlan.OnlineErrorMessage);

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

		private MigrationPlan CreateMigrationPlan()
		{
			int lastAlreadyCompletedMigrationNumber = 0;

			List<Migration> offlineMigrationsToRun = new List<Migration>();
			List<Migration> onlineMigrationsToRun = new List<Migration>();
			List<StoredCodeDefinition> storedCodeToRun = new List<StoredCodeDefinition>();

			MigrationPlan Error(string message)
			{
				return new MigrationPlan(
					message, 
					message, 
					offlineMigrationsToRun, 
					storedCodeToRun,
					onlineMigrationsToRun, 
					lastAlreadyCompletedMigrationNumber);
			}

			// the _journal might throw an exception here if it was never created for the target data store
			var migrationsAlreadyRun = GetDeployedMigrations();

			var availableMigrations = GetAvailableMigrations();

			if (availableMigrations.GroupBy(am => am.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
			{
				// we have duplicate migration names so we can't do anything
				return Error("Cannot migrate due to migration names that are not unique.");
			}
			if (availableMigrations.GroupBy(am => am.MigrationNumber).Any(g => g.Count() > 1))
			{
				// we have duplicate migration numbers so we can't do anything
				return Error("Cannot migrate due to migration numbers that are not unique.");
			}
			var migrationNumbers = availableMigrations.Select(am => am.MigrationNumber).ToArray();
			if (!migrationNumbers.SequenceEqual(migrationNumbers.OrderBy(v => v)))
			{
				// we have migration numbers out-of order so we can't do anything
				return Error("Cannot migrate due to migration numbers that are not in order.");
			}

			bool mustRestartLastMigration = false;
			for (int i = 0; i < migrationsAlreadyRun.Count; i++)
			{
				var deployedMigration = migrationsAlreadyRun[i];

				if (i == availableMigrations.Count)
				{
					// then we've just encountered the first deployed migration that is not known as an available one, so this is an attempt to migrate to an incompatible branch
					return Error(
						"Cannot migrate due to incompatible branch. " +
						$"Deployed migration ({deployedMigration.MigrationNumber}) {deployedMigration.Name} is not known."
					);
				}
				var availableMigration = availableMigrations[i];
				// if they don't match and this is a complete migration, or is the last migration and is an incomplete offline migration, this is an attempt to migrate to an incompatible branch
				if (deployedMigration.MigrationNumber == availableMigration.MigrationNumber
				    && deployedMigration.Name.Equals(availableMigration.Name, StringComparison.OrdinalIgnoreCase))
				{
					// now if the migration is complete, the fingerprints must match
					if (deployedMigration.Complete)
					{
						lastAlreadyCompletedMigrationNumber = deployedMigration.MigrationNumber;
						if (deployedMigration.Fingerprint != availableMigration.Fingerprint)
						{
							// then a completed migration has been modified, so this is an attempt to migrate to an incompatible branch
							return Error(
								"Cannot migrate due to incompatible branch. " +
								$"Deployed migration ({deployedMigration.MigrationNumber}) {deployedMigration.Name} has been modified."
							);
						}
						// otherwise, these migrations match so we'll continue in our loop.
					}
					else
					{
						// the deployed migration is incomplete. It must be the last one in the list of deployed migrations.
						if (availableMigration.IsOnlineMigration == false)
						{
							return Error(
								"Cannot migrate due to incomplete prior offline migration. " +
								$"Deployed migration ({deployedMigration.MigrationNumber}) {deployedMigration.Name} did not complete. " +
								"Restore the database from a backup or manually fix the database and mark the migration complete in the journal, or delete it from the journal to have it run the next time."
							);
						}
						// otherwise it must be an online migration so we are allowed to restart it even if its fingerprint is different
						mustRestartLastMigration = true;
					}
				}
				else
				{
					// then we just encountered a mismatch between the deployed and available migrations, so this is an attempt to migrate to an incompatible branch
					return Error(
						"Cannot migrate due to incompatible branch. " +
						$"Available migration ({availableMigration.MigrationNumber}) {availableMigration.Name} was found where deployed migration ({deployedMigration.MigrationNumber}) {deployedMigration.Name} was expected."
					);
				}
			}

			// we'll either take all the migrations after the deployed ones, or we'll take the last migration to restart it plus all the rest.
			int indexOfFirstMigrationToRun = migrationsAlreadyRun.Count;
			if (mustRestartLastMigration)
				indexOfFirstMigrationToRun -= 1;
			
			// at this point, we can take the remainder of the available migrations and verify them.
			if (_includeOnlineMigrationsDuringOffline)
			{
				offlineMigrationsToRun = availableMigrations.Skip(indexOfFirstMigrationToRun).ToList();
			}
			else
			{
				var foundOnlineMigration = false;
				for (int i = indexOfFirstMigrationToRun; i < availableMigrations.Count; i++)
				{
					var availableMigration = availableMigrations[i];
					if (availableMigration.IsOnlineMigration)
					{
						foundOnlineMigration = true;
						onlineMigrationsToRun.Add(availableMigration);
					}
					else if (foundOnlineMigration)
					{
						// then we found that there is an offline migration that follows at least one online migration therefore this version is undeployable.
						return Error(
							"Cannot migrate due to incompatible branch. " +
							$"Found offline migration ({availableMigration.MigrationNumber}) {availableMigration.Name} that follows an online migration.");
					}
					else
					{
						offlineMigrationsToRun.Add(availableMigration);
					}
				}
			}

			// determine which repeatable scripts need to run
			var repeatableScriptsAlreadyRun = _journal.GetDeployedStoredCodeDefinitions().ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);
			var availableRepeatableScripts = _migrationsProvider.GatherStoredCodeDefinitions();
			// find the differences
			storedCodeToRun = availableRepeatableScripts
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

			// determine whether online migration is possible
			var onlineErrorMessage = offlineMigrationsToRun.Count > 1
				? $"Cannot migrate online due to offline migrations that need to run first: ({offlineMigrationsToRun[0].MigrationNumber}) {offlineMigrationsToRun[0].Name}."
				: null;

			return new MigrationPlan(null, onlineErrorMessage, offlineMigrationsToRun, storedCodeToRun, onlineMigrationsToRun, lastAlreadyCompletedMigrationNumber);
		}

		private IReadOnlyList<DeployedMigration> GetDeployedMigrations()
		{
			_deployedMigrations = _deployedMigrations ?? _journal.GetDeployedMigrations();
			return _deployedMigrations;
		}

		private IReadOnlyList<Migration> GetAvailableMigrations()
		{
			_availableMigrations = _availableMigrations ?? _migrationsProvider.GatherMigrations();
			return _availableMigrations;
		}
	}
}
