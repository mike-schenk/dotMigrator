using System.Collections.Generic;

namespace dotMigrator
{
	/// <summary>
	/// The interface for classes that gather all known migrations and stored code definitions that might need to be applied during a deployment
	/// </summary>
	public interface IMigrationsProvider
	{
		/// <summary>
		/// Returns all of the known migrations in order by migration number
		/// </summary>
		/// <returns></returns>
		IReadOnlyList<Migration> GatherMigrations();
		
		/// <summary>
		/// Returns all of the known stored code definitions in the order in which they should be applied to the target data store
		/// </summary>
		/// <returns></returns>
		IReadOnlyList<StoredCodeDefinition> GatherStoredCodeDefinitions();
	}
}