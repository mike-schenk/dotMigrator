using System;
using System.Collections.Generic;

namespace dotMigrator
{
	public class ConsoleProgressReporter : IProgressReporter
	{
		private readonly Stack<string> _blockNames = new Stack<string>();

		public void Report(string message)
		{
			Console.Write(new string(' ', _blockNames.Count * 3));
			Console.WriteLine(message);
		}

		public void BeginBlock(string name)
		{
			Console.Write(new string(' ', _blockNames.Count * 3));
			Console.WriteLine("Begin " + name);
			_blockNames.Push(name);
		}

		public void EndBlock(string name)
		{
			//now we pop the block stack until we find the block we're looking for
			var undo = new Stack<string>();
			while (_blockNames.Count > 0)
			{
				var lastBlock = _blockNames.Pop();
				if (lastBlock == name)
					return;
			}
			// if we didn't find a matching block name, put all of them back
			while(undo.Count > 0)
				_blockNames.Push(undo.Pop());

			Console.Write(new string(' ', _blockNames.Count * 3));
			Console.WriteLine("End " + name);
		}
	}

	public class TeamCityProgressReporter : IProgressReporter
	{
		public void Report(string message)
		{
			Console.WriteLine($"##teamcity[message text='{Escape(message)}']");
		}

		public void BeginBlock(string name)
		{
			Console.WriteLine($"##teamcity[blockOpened name='{Escape(name)}']");
		}

		public void EndBlock(string name)
		{
			Console.WriteLine($"##teamcity[blockClosed name='{Escape(name)}']");
		}

		private string Escape(string value)
		{
			return value
				.Replace("'", "|'")
				.Replace("\n", "|n")
				.Replace("\r", "|r")
				.Replace("[", "|[")
				.Replace("]", "|]")
				.Replace("|", "||");
		}
	}
}