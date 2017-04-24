using System.Collections.Generic;

namespace dotMigrator
{
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