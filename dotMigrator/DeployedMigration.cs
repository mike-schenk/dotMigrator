namespace dotMigrator
{
	public class DeployedMigration
	{
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

		public int MigrationNumber { get; }
		public string Name { get; }
		public string Fingerprint { get; }
		public bool Complete { get; }
	}
}