using System.Collections.Generic;

namespace dotMigrator
{
	/// <summary>
	/// The result of <see cref="Migrator.Plan"/>
	/// </summary>
	public class MigrationPlan
	{
		public MigrationPlan(
			string offlineErrorMessage, 
			string onlineErrorMessage, 
			IReadOnlyList<Migration> offlineMigrations, 
			IReadOnlyList<StoredCodeDefinition> storedCodeDefinitions, 
			IReadOnlyList<Migration> onlineMigrations, 
			int lastCompletedMigrationNumber)
		{
			OfflineErrorMessage = offlineErrorMessage;
			OnlineErrorMessage = onlineErrorMessage;
			OfflineMigrations = offlineMigrations;
			StoredCodeDefinitions = storedCodeDefinitions;
			OnlineMigrations = onlineMigrations;
			LastCompletedMigrationNumber = lastCompletedMigrationNumber;
		}

		public bool HasOfflineMigrations => OfflineMigrations.Count > 0;
		public bool HasStoredCodeChanges => StoredCodeDefinitions.Count > 0;
		public bool HasOnlineMigrations => OnlineMigrations.Count > 0;
		public string OfflineErrorMessage { get; }
		public string OnlineErrorMessage { get; }

		internal IReadOnlyList<Migration> OfflineMigrations { get; }
		internal IReadOnlyList<StoredCodeDefinition> StoredCodeDefinitions { get; }
		internal IReadOnlyList<Migration> OnlineMigrations { get; }
		internal int LastCompletedMigrationNumber { get; }
	}
}