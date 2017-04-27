using System;

namespace dotMigrator
{
	/// <summary>
	/// Code or script that changes the data structure of data in the data store.
	/// </summary>
	public class Migration
	{
		private readonly Action<IProgressReporter> _runAction;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="migrationNumber">A unique non-negative integer that puts this migration in the sequence of all migrations for the data store</param>
		/// <param name="name">A case-insensitive name to uniquely identify this migration. Could be a script filename.</param>
		/// <param name="fingerprint">A string that can be used to detect changes in the content between deployments. Typically a cryptographic hash of script file contents.</param>
		/// <param name="isOnlineMigration">Indicates that the migration is written to run while the application remains online.</param>
		/// <param name="runAction">The delegate that performs the migration in the data store</param>
		public Migration(
			int migrationNumber, 
			string name, 
			string fingerprint, 
			bool isOnlineMigration,
			Action<IProgressReporter> runAction
		)
		{
			_runAction = runAction;
			MigrationNumber = migrationNumber;
			Name = name;
			Fingerprint = fingerprint;
			IsOnlineMigration = isOnlineMigration;
		}

		/// <summary>
		/// A unique non-negative integer that puts this migration in the sequence of all migrations for the data store.
		/// </summary>
		public int MigrationNumber { get; }

		/// <summary>
		/// Uniquely identifies this migration in the available migrations from the <see cref="IMigrationsProvider"/> and also in the journal.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Uniquely identifies the <i>content</i> of this migration in order to detect that it has changed between 
		/// deployments, which means its outcome might be different.
		/// </summary>
		public string Fingerprint { get; }

		/// <summary>
		/// Indicates that this migration is expected to run while the application remains online.
		/// It must be written so that it can resume where it left off in case it is interrupted and restarted.
		/// </summary>
		public bool IsOnlineMigration { get; }

		/// <summary>
		/// Performs the migration in the target data store.
		/// </summary>
		/// <param name="progressReporter"></param>
		public void Run(IProgressReporter progressReporter)
		{
			_runAction.Invoke(progressReporter);
		}
	}
}