namespace dotMigrator
{
	/// <summary>
	/// Interface for classes that can report progress to the user during deployments
	/// </summary>
	public interface IProgressReporter
	{
		// methods such as "begin block", "end block", (like TeamCity progress messages) "writeline", maybe ways to report progress of a single 'task' in parts on one line
		// methods that might begin and then move a progress bar (like linux boot sequence)

		void Report(string message);

		void BeginBlock(string name);
		void EndBlock(string name);
		
		//TODO: some way to report percent complete or incrementing record counts
	}
}