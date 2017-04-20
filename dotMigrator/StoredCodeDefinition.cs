using System;

namespace dotMigrator
{
	public class StoredCodeDefinition
	{
		private readonly Action<IProgressReporter> _executeAction;

		public StoredCodeDefinition(
			string name,
			string fingerprint,
			Action<IProgressReporter> executeAction
		)
		{
			_executeAction = executeAction;
			Name = name;
			Fingerprint = fingerprint;
		}

		public string Name { get; }
		public string Fingerprint { get; }

		public void Execute(IProgressReporter progressReporter)
		{
			_executeAction.Invoke(progressReporter);
		}
	}
}