namespace dotMigrator
{
	/// <summary>
	/// Represents a stored code definition that has already been applied to the data store and recorded in its journal
	/// </summary>
	public class DeployedStoredCodeDefinition
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name"></param>
		/// <param name="fingerprint"></param>
		public DeployedStoredCodeDefinition(string name, string fingerprint)
		{
			Name = name;
			Fingerprint = fingerprint;
		}

		/// <summary>
		/// Uniquely identifies this definition within the journal associated with this data store.
		/// The same as <see cref="StoredCodeDefinition.Name"/>
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Uniquely identifies the <i>content</i> of this stored code definition to determine what has changed between deployments. Should be kept short. Typically a cryptographic hash.
		/// The same as <see cref="StoredCodeDefinition.Fingerprint"/>
		/// </summary>
		public string Fingerprint { get; }
	}
}