using System.Collections.Generic;

namespace dotMigrator
{
	public interface IMigrationsProvider
	{
		IReadOnlyList<Migration> GatherMigrations();
		IReadOnlyCollection<StoredCodeDefinition> GatherStoredCodeDefinitions();
	}
}