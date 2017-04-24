using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using dotMigrator;

namespace Tests
{
	public class FakeMigrationsProvider : IMigrationsProvider
	{
		private List<Migration> _migrations = new List<Migration>();
		private List<StoredCodeDefinition> _storedCodeDefinitions = new List<StoredCodeDefinition>();

		public FakeMigrationsProvider WithMigrations(IReadOnlyList<Migration> migrations)
		{
			_migrations = migrations.ToList();
			return this;
		}

		public FakeMigrationsProvider WithStoredCodeDefinitions(
			IReadOnlyCollection<StoredCodeDefinition> storedCodeDefinitions)
		{
			_storedCodeDefinitions = storedCodeDefinitions.ToList();
			return this;
		}

		public IReadOnlyList<Migration> GatherMigrations()
		{
			return _migrations;
		}

		public IReadOnlyList<StoredCodeDefinition> GatherStoredCodeDefinitions()
		{
			return new ReadOnlyCollection<StoredCodeDefinition>(_storedCodeDefinitions);
		}
	}
}