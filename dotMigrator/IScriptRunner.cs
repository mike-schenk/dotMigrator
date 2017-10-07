namespace dotMigrator
{
	/// <summary>
	/// Interface for classes that know how to run text-based scripts in a target data store
	/// </summary>
	public interface IScriptRunner
	{
		/// <summary>
		/// Runs the given script in the target data store
		/// </summary>
		/// <param name="scriptContents"></param>
		void Run(string scriptContents);
	}

    public interface IScriptFileRunner
    {
        /// <summary>
        /// Runs the identified script in the target data store
        /// </summary>
        /// <param name="absoluteScriptFileName">The full file path of the script to run</param>
        void Run(string absoluteScriptFileName);
    }
}