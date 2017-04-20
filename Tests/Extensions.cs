using System;
using System.Collections.Generic;
using dotMigrator;

namespace Tests
{
	public static class Extensions
	{
		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Predicate<T> predicate)
		{
			foreach (T el in source)
			{
				yield return el;
				if(predicate(el))
					yield break;
			}
		}

		public static DeployedMigration AsDeployedMigration(this Migration migration, bool complete)
		{
			return new DeployedMigration(migration.MigrationNumber, migration.Name, migration.Fingerprint, complete);
		}
	}
}