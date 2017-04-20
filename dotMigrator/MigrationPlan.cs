using System.Collections.Generic;

namespace dotMigrator
{
	public class MigrationPlan
	{
		private int _lastMigrationNumber;
		public bool HasOfflineMigrations { get; }
		public bool HasStoredCodeChanges { get; }
		public bool HasOnlineMigrations { get; }
		public string ErrorMessage { get; }

		internal IReadOnlyList<Migration> OfflineMigrations { get; }
		internal IReadOnlyList<Migration> OnlineMigrations { get; }
		internal IReadOnlyList<StoredCodeDefinition> StoredCodeDefinitions { get; }
		internal int LastMigrationNumber { get; }
	}
}