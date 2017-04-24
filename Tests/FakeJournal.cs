using System;
using System.Collections.Generic;
using System.Linq;
using dotMigrator;

namespace Tests
{
	public class FakeJournal : IJournal
	{
		private Dictionary<string,DeployedMigration> _migrations = new Dictionary<string,DeployedMigration>(StringComparer.OrdinalIgnoreCase);
		private List<DeployedStoredCodeDefinition> _storedCodeDefinitions = new List<DeployedStoredCodeDefinition>();

		public bool IsCreated { get; private set; }

		public FakeJournal Created()
		{
			IsCreated = true;
			return this;
		}

		public FakeJournal WithMigrations(IEnumerable<DeployedMigration> migrations)
		{
			Created();
			_migrations = migrations.ToDictionary(m => m.Name);
			return this;
		}

		public FakeJournal WithStoredCodeDefinitions(IEnumerable<DeployedStoredCodeDefinition> definitions)
		{
			Created();
			_storedCodeDefinitions = definitions.ToList();
			return this;
		}

		public void CreateJournal()
		{
			IsCreated = true;
		}

		public void SetBaseline(IEnumerable<Migration> baselineMigrations)
		{
			CreateJournal();
			foreach (var migration in baselineMigrations)
			{
				_migrations.Add(migration.Name, new DeployedMigration(migration.MigrationNumber, migration.Name, migration.Fingerprint, true));
			}
		}

		public void RecordStartMigration(Migration migrationToRun)
		{
			// either update the migration whose name matches, or append it
			var newMigration = new DeployedMigration(migrationToRun.MigrationNumber, migrationToRun.Name, migrationToRun.Fingerprint, false);
			if(_migrations.ContainsKey(newMigration.Name))
			{
				_migrations[newMigration.Name] = newMigration;
			}
			else
			{
				_migrations.Add(newMigration.Name, newMigration);
			}
		}

		public void RecordCompleteMigration(Migration migration)
		{
			_migrations[migration.Name] = new DeployedMigration(migration.MigrationNumber, migration.Name, migration.Fingerprint, true);
		}

		public void RecordStoredCodeDefinition(StoredCodeDefinition storedCodeDefinition, int lastMigrationNumber)
		{
			// need to look for the matching one by name
			var newDeployedDefinition = new DeployedStoredCodeDefinition(storedCodeDefinition.Name, storedCodeDefinition.Fingerprint);
			var fooundIndex = _storedCodeDefinitions.FindIndex(
				d => d.Name.Equals(storedCodeDefinition.Name, StringComparison.OrdinalIgnoreCase));
			if (fooundIndex >= 0)
				_storedCodeDefinitions[fooundIndex] = newDeployedDefinition;
			else
				_storedCodeDefinitions.Add(newDeployedDefinition);
		}

		public IReadOnlyList<DeployedMigration> GetDeployedMigrations()
		{
			if(!IsCreated)
				throw new Exception("Journal not created");
			return _migrations.Values.OrderBy(m => m.MigrationNumber).ToList();
		}

		public IReadOnlyList<DeployedStoredCodeDefinition> GetDeployedStoredCodeDefinitions()
		{
			if(!IsCreated)
				throw new Exception("Journal not created");
			return _storedCodeDefinitions;
		}
	}
}