namespace dotMigrator
{
	public class DeployedStoredCodeDefinition
	{
		public DeployedStoredCodeDefinition(string name, string fingerprint)
		{
			Name = name;
			Fingerprint = fingerprint;
		}

		public string Name { get; }
		public string Fingerprint { get; }
	}
}