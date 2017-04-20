using System;
using System.Collections.Generic;
using System.Linq;
using dotMigrator;

namespace Tests
{
	public class FakeJournal : IJournal
	{
		private List<DeployedMigration> _migrations = new List<DeployedMigration>();
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
			_migrations = migrations.ToList();
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

		public void SetBaseline(string lastMigrationToBaseline, IMigrationsProvider migrationsProvider)
		{
			if (_migrations.Any())
				return;

			_migrations = migrationsProvider.GatherMigrations()
				.TakeUntil(m => !m.Name.Equals(lastMigrationToBaseline, StringComparison.OrdinalIgnoreCase))
				.Select(m => new DeployedMigration(m.MigrationNumber, m.Name, m.Fingerprint, true))
				.ToList();
		}

		public void RecordStartMigration(Migration migrationToRun)
		{
			// either update the last deployed migration when the name matches, or append it to the end of the list
			var newMigration = new DeployedMigration(migrationToRun.MigrationNumber, migrationToRun.Name, migrationToRun.Fingerprint, false);
			if(_migrations.Count > 0 && _migrations[_migrations.Count - 1].Name.Equals(migrationToRun.Name, StringComparison.OrdinalIgnoreCase))
			{
				_migrations[_migrations.Count-1] = newMigration;
			}
			else
			{
				_migrations.Add(newMigration);
			}
		}

		public void RecordCompleteMigration(Migration migrationToRun)
		{
			_migrations[_migrations.Count - 1] = new DeployedMigration(migrationToRun.MigrationNumber, migrationToRun.Name, migrationToRun.Fingerprint, true);
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
			return _migrations;
		}

		public IReadOnlyList<DeployedStoredCodeDefinition> GetDeployedStoredCodeDefinitions()
		{
			if(!IsCreated)
				throw new Exception("Journal not created");
			return _storedCodeDefinitions;
		}
	}
}