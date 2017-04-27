namespace dotMigrator
{
	/// <summary>
	/// Represents a migration that has already been deployed to the data store and recorded in its journal
	/// </summary>
	public class DeployedMigration
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="migrationNumber">Same as <see cref="Migration.MigrationNumber"/></param>
		/// <param name="name">Same as <see cref="Migration.Name"/></param>
		/// <param name="fingerprint">Same as <see cref="Migration.Fingerprint"/></param>
		/// <param name="complete">Whether the migration has already run to completion</param>
		public DeployedMigration(
			int migrationNumber, 
			string name, 
			string fingerprint, 
			bool complete)
		{
			MigrationNumber = migrationNumber;
			Name = name;
			Fingerprint = fingerprint;
			Complete = complete;
		}

		/// <summary>
		/// A unique non-negative integer that puts this migration in the sequence of all migrations for the data store
		/// Same as <see cref="Migration.MigrationNumber"/>
		/// </summary>
		public int MigrationNumber { get; }

		/// <summary>
		/// Uniquely identifies this migration in the available migrations from the <see cref="IMigrationsProvider"/> and also in the journal
		/// Same as <see cref="Migration.Name"/>
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Uniquely identifies the <i>content</i> of this migration in order to detect that it has changed bewteen deployments, so its outcome might be different
		/// Same as <see cref="Migration.Fingerprint"/>
		/// </summary>
		public string Fingerprint { get; }

		/// <summary>
		/// Whether the migration ran to completion.
		/// </summary>
		public bool Complete { get; }
	}
}