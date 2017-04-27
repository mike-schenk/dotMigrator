using System.Collections.Generic;

namespace dotMigrator
{
	/// <summary>
	/// A Migration provider that can combine migrations from multiple other providers.
	/// </summary>
	public class MergedMigrationProvider : IMigrationsProvider
	{
		private readonly List<IMigrationsProvider> _providers = new List<IMigrationsProvider>();
		
		/// <summary>
		/// Merges an additional migration provider
		/// </summary>
		/// <param name="provider"></param>
		public void Add(IMigrationsProvider provider)
		{
			if(!_providers.Contains(provider))
				_providers.Add(provider);
		}

		/// <summary>
		/// Returns all of the known migrations in order by migration number
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<Migration> GatherMigrations()
		{
			var toReturn = new List<Migration>();
			foreach (var p in _providers)
				toReturn.AddRange(p.GatherMigrations());
			toReturn.Sort((a,b) => a.MigrationNumber - b.MigrationNumber); // returns a negative number when "a" is less than "b" meaning "a" should sort before "b"
			return toReturn;
		}

		/// <summary>
		/// Returns all of the known stored code definitions in the order in which they should be applied to the target data store
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<StoredCodeDefinition> GatherStoredCodeDefinitions()
		{
			var toReturn = new List<StoredCodeDefinition>();
			foreach (var p in _providers)
				toReturn.AddRange(p.GatherStoredCodeDefinitions());
			toReturn.Sort((a, b) => a.DependencyLevel - b.DependencyLevel); // a definition with a lower dependency level should be applied before one with a higher dependency level
			return toReturn;
		}
	}
}