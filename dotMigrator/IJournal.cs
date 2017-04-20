using System.Collections;
using System.Collections.Generic;

namespace dotMigrator
{
	public interface IJournal
	{
		/// <summary>
		/// Creates a table, file, collection etc.
		/// This should be a no-op if the journal already exists.
		/// </summary>
		void CreateJournal();

		void SetBaseline(string lastMigrationToBaseline, IMigrationsProvider migrationsProvider);


		void RecordStartMigration(Migration migrationToRun);
		void RecordCompleteMigration(Migration migrationToRun);
		void RecordStoredCodeDefinition(StoredCodeDefinition storedCodeDefinition, int lastMigrationNumber);
		IEnumerable<DeployedMigration> GetDeployedMigrations();
		IEnumerable<DeployedStoredCodeDefinition> GetDeployedStoredCodeDefinitions();
	}
}