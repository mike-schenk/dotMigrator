namespace dotMigrator
{
	public class DeployedMigration
	{
		public int MigrationNumber { get; }
		public string Name { get; }
		public string Fingerprint { get; }
		public bool Complete { get; }
	}
}