using System;
using System.Collections.Generic;

namespace dotMigrator
{
	public static class Extensions
	{
		public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Predicate<T> predicate)
		{
			foreach (T el in source)
			{
				yield return el;
				if (predicate(el))
					yield break;
			}
		}
	}
}