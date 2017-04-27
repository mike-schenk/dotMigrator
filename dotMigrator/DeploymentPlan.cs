using System.Collections.Generic;

namespace dotMigrator
{
	/// <summary>
	/// The result of <see cref="Migrator.Plan"/>
	/// </summary>
	public class DeploymentPlan
	{
		internal DeploymentPlan(
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

		/// <summary>
		/// Whether the plan includes offline migrations
		/// </summary>
		public bool HasOfflineMigrations => OfflineMigrations.Count > 0;

		/// <summary>
		/// Whether the plan includes stored code changes
		/// </summary>
		public bool HasStoredCodeChanges => StoredCodeDefinitions.Count > 0;

		/// <summary>
		/// Whether the plan includes online migrations
		/// </summary>
		public bool HasOnlineMigrations => OnlineMigrations.Count > 0;

		/// <summary>
		/// If dotMigrator cannot perform an offline deployment, this contains the error message
		/// </summary>
		public string OfflineErrorMessage { get; }

		/// <summary>
		/// If dotMigrator cannot perform an online deployment, this contains the error message
		/// </summary>
		public string OnlineErrorMessage { get; }

		internal IReadOnlyList<Migration> OfflineMigrations { get; }
		internal IReadOnlyList<StoredCodeDefinition> StoredCodeDefinitions { get; }
		internal IReadOnlyList<Migration> OnlineMigrations { get; }
		internal int LastCompletedMigrationNumber { get; }
	}
}