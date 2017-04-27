using System;

namespace dotMigrator
{
	/// <summary>
	/// Holds a representation of executable code that is stored in the data store.
	/// The Apply method knows how to create or replace that executable code.
	/// </summary>
	public class StoredCodeDefinition
	{
		private readonly Action<IProgressReporter> _applyAction;

		/// <summary>
		/// Constructs this object
		/// </summary>
		/// <param name="name">A case-insensitive name to uniquely identify this definition. Typically a filename.</param>
		/// <param name="fingerprint">A string that can be used to compare the content between deployments. Typically a cryptographic hash.</param>
		/// <param name="applyAction">The delegate that can apply the new definition to the data store.</param>
		/// <param name="dependencyLevel">Used to identify the sequence this needs to be applied relative to other stored code it might depend upon. Lower numbers are applied before higher numbers.</param>
		public StoredCodeDefinition(
			string name,
			string fingerprint,
			Action<IProgressReporter> applyAction,
			int dependencyLevel
		)
		{
			_applyAction = applyAction;
			Name = name;
			Fingerprint = fingerprint;
			DependencyLevel = dependencyLevel;
		}

		/// <summary>
		/// Uniquely identifies this definition within the journal associated with this data store
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Uniquely identifies the <i>content</i> of this stored code definition to determine what has changed between deployments. Should be kept short. Typically a cryptographic hash.
		/// </summary>
		public string Fingerprint { get; }

		/// <summary>
		/// The level of this definition in a dependency tree. Definitions with a lower dependency level will be applied before those with a higher level.
		/// </summary>
		public int DependencyLevel { get; }

		/// <summary>
		/// Creates or replaces the stored code definition in the target data store
		/// </summary>
		/// <param name="progressReporter"></param>
		public void Apply(IProgressReporter progressReporter)
		{
			_applyAction.Invoke(progressReporter);
		}
	}
}