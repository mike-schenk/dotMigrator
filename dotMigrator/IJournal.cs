using System.Collections.Generic;

namespace dotMigrator
{
	/// <summary>
	/// The interface for classes that track the series of migrations and the versions of stored code definitions that have been deployed to a target data store.
	/// </summary>
	public interface IJournal
	{
		/// <summary>
		/// Creates a table, file, collection etc.
		/// This should be a no-op if the journal already exists.
		/// </summary>
		void CreateJournal();

		/// <summary>
		/// Records that a series of migrations has already been completed in a target data store.
		/// This is used when an existing data store is being put under management by dotMigrator
		/// </summary>
		/// <param name="baselineMigrations"></param>
		/// <returns></returns>
		IReadOnlyList<DeployedMigration> SetBaseline(IEnumerable<Migration> baselineMigrations);


		/// <summary>
		/// Insert or update the migration identified by its name in the journal.
		/// It will be recorded as an incomplete migration.
		/// </summary>
		/// <param name="migration"></param>
		void RecordStartMigration(Migration migration);

		/// <summary>
		/// Update the identified migration in the journal as having been completed.
		/// It can be assumed that this will only be called for the most recently started mgiration.
		/// </summary>
		/// <param name="migration"></param>
		void RecordCompleteMigration(Migration migration);

		/// <summary>
		/// Insert a record to the journal that a new stored code definition has been completely applied, 
		/// or update an existing record with the new fingerprint of a stored code definition that has 
		/// just been applied.
		/// </summary>
		/// <param name="storedCodeDefinition"></param>
		/// <param name="lastMigrationNumber">The number of the last migration to have completed when this stored code definition was applied</param>
		void RecordStoredCodeDefinition(StoredCodeDefinition storedCodeDefinition, int lastMigrationNumber);

		/// <summary>
		/// Returns the sequenced list of offline and online migrations that have been recorded in the journal
		/// </summary>
		IReadOnlyList<DeployedMigration> GetDeployedMigrations();

		/// <summary>
		/// Returns all of the stored code definitions that have been previously applied as recorded in this journal.
		/// The order of the definitions does not matter since the names are used to match them up with the available ones
		/// that the MigrationsProvider finds.
		/// </summary>
		/// <returns></returns>
		IReadOnlyList<DeployedStoredCodeDefinition> GetDeployedStoredCodeDefinitions();
	}
}