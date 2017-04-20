using System;

namespace dotMigrator
{
	public class Migration
	{
		private readonly Action<IProgressReporter> _executeAction;

		public Migration(
			int migrationNumber, 
			string name, 
			string fingerprint, 
			bool isOnlineMigration,
			Action<IProgressReporter> executeAction
		)
		{
			_executeAction = executeAction;
			MigrationNumber = migrationNumber;
			Name = name;
			Fingerprint = fingerprint;
			IsOnlineMigration = isOnlineMigration;
		}

		public int MigrationNumber { get; }
		public string Name { get; }
		public string Fingerprint { get; }
		public bool IsOnlineMigration { get; }

		public void Execute(IProgressReporter progressReporter)
		{
			_executeAction.Invoke(progressReporter);
		}
	}
}