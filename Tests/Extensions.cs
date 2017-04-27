using dotMigrator;

namespace Tests
{
	public static class Extensions
	{
		public static DeployedMigration AsDeployedMigration(this Migration migration, bool complete)
		{
			return new DeployedMigration(migration.MigrationNumber, migration.Name, migration.Fingerprint, complete);
		}
	}
}