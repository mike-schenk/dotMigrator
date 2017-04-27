using System;
using System.Collections.Generic;

namespace dotMigrator
{
	internal static class Extensions
	{
		internal static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Predicate<T> predicate)
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