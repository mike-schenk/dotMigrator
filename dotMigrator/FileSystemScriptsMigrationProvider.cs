using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace dotMigrator
{
	/// <summary>
	/// Reads text scripts from folders in the filesystem. Migrations from one path and stored code definitions from another.
	/// Fingerprints are the MD5 hash of the UTF-8 encoding of the textual content of the files even if the files are actually stored in another encoding.
	/// </summary>
	public class FileSystemScriptsMigrationProvider : IMigrationsProvider
	{
		private readonly string _migrationsDirectory;
		private readonly string _migrationsFileWildcard;
		private readonly string _storedCodeDefinitionsDirectory;
		private readonly string _storedCodeDefinitionFileWildcard;
	    private delegate Action<IProgressReporter> RunActionBuilderFunc(string filePath, string scriptContents);
	    private readonly RunActionBuilderFunc _runActionBuilder;

		/// <summary>
		/// Construct a FileSystemScriptsMigrationProvider using the given directories and glob patterns. 
		/// </summary>
		/// <param name="migrationsDirectory">The directory containing migration scripts</param>
		/// <param name="migrationsFileWildcard">The filename pattern to identify migration scripts in the directory</param>
		/// <param name="storedCodeDefinitionsDirectory">The directory containing stored code definition scripts</param>
		/// <param name="storedCodeDefinitionFileWildcard">The filename pattern to identify the stored code definition scripts in the directory</param>
		/// <param name="scriptRunner">The script runner that has the ability to execute the text of the scripts for the target data store</param>
		public FileSystemScriptsMigrationProvider(
			string migrationsDirectory, 
			string migrationsFileWildcard, 
			string storedCodeDefinitionsDirectory, 
			string storedCodeDefinitionFileWildcard, 
			IScriptRunner scriptRunner)
		{
			_migrationsDirectory = migrationsDirectory;
			_migrationsFileWildcard = migrationsFileWildcard;
			_storedCodeDefinitionsDirectory = storedCodeDefinitionsDirectory;
			_storedCodeDefinitionFileWildcard = storedCodeDefinitionFileWildcard;

		    _runActionBuilder = (string filePath, string fileContents) =>
		    {
		        return pr => scriptRunner.Run(fileContents);
		    };
		}

        /// <summary>
        /// Construct a FileSystemScriptsMigrationProvider using the given directories and glob patterns. 
        /// </summary>
        /// <param name="migrationsDirectory">The directory containing migration scripts</param>
        /// <param name="migrationsFileWildcard">The filename pattern to identify migration scripts in the directory</param>
        /// <param name="storedCodeDefinitionsDirectory">The directory containing stored code definition scripts</param>
        /// <param name="storedCodeDefinitionFileWildcard">The filename pattern to identify the stored code definition scripts in the directory</param>
        /// <param name="scriptFileRunner">The script runner that has the ability to execute the script files for the target data store</param>
        public FileSystemScriptsMigrationProvider(
	        string migrationsDirectory,
	        string migrationsFileWildcard,
	        string storedCodeDefinitionsDirectory,
	        string storedCodeDefinitionFileWildcard,
	        IScriptFileRunner scriptFileRunner)
	    {
	        _migrationsDirectory = migrationsDirectory;
	        _migrationsFileWildcard = migrationsFileWildcard;
	        _storedCodeDefinitionsDirectory = storedCodeDefinitionsDirectory;
	        _storedCodeDefinitionFileWildcard = storedCodeDefinitionFileWildcard;

	        _runActionBuilder = (string filePath, string fileContents) =>
	        {
	            return pr => scriptFileRunner.Run(filePath);
	        };
	    }

        /// <summary>
        /// Returns all of the known migrations in order by migration number by extracting the migration number from the digits in the beginning of the filename
        /// Online migrations are identified by the string "online " following the migration number.
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<Migration> GatherMigrations()
		{
			MD5 md5 = MD5.Create();
			var files = Directory.GetFiles(_migrationsDirectory, _migrationsFileWildcard);

			return files
				.Select(
					file =>
					{
						var filename = Path.GetFileName(file);

						// extract the migration number and name separately from the filename
						int migrationNumber = 0;
						string name = "";
						if (string.IsNullOrEmpty(filename) || !Char.IsDigit(filename[0]))
							throw new Exception("Migration script filenames must begin with a digit.");

						var digits = new Stack<char>(filename.Length);
						bool isOnline = false;
						for (int i = 0; i < filename.Length; i++)
						{
							char c = filename[i];
							if (Char.IsDigit(c))
								digits.Push(c);
							else
							{
								name = filename.Substring(i).Trim();
								if (name.StartsWith("online ", StringComparison.OrdinalIgnoreCase))
								{
									isOnline = true;
									name = name.Substring(7).Trim();
								}
								break;
							}
						}
						for (int placeValue = 1; digits.Count > 0; placeValue *= 10)
						{
							int digitValue = digits.Pop() - '0';
							migrationNumber += digitValue * placeValue;
						}
						using (var fs = File.OpenRead(file))
						{
							using (var streamReader = new StreamReader(fs, Encoding.UTF8, true))
							{
								var contents = streamReader.ReadToEnd();
								var hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(contents)));

								return new Migration(migrationNumber, name.ToString(), hash, isOnline, _runActionBuilder(file, contents));
							}
						}
					}
				)
				.OrderBy(m => m.MigrationNumber)
				.ToList();
		}

		/// <summary>
		/// Returns all of the known stored code definitions in the order in which they should be applied to the target data store.
		/// Any digits from the front of the filename are used as the dependency level which deteremines the order in which they should be applied.
		/// Those digits are not included in the name stored in the journal so that an object's dependency level can change without changing which object the file represents.
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<StoredCodeDefinition> GatherStoredCodeDefinitions()
		{
			MD5 md5 = MD5.Create();
			var files = Directory.GetFiles(_storedCodeDefinitionsDirectory, _storedCodeDefinitionFileWildcard);

			return files
				.Select(
					file =>
					{
						var filename = Path.GetFileName(file);
						// extract the name from the filename while ignoring leading digits that are used only for sequencing.
						string name = "";
						if (string.IsNullOrEmpty(filename))
							throw new Exception("Stored Code Definition script needs a filename");

						var digits = new Stack<char>(filename.Length);
						for (int i = 0; i < filename.Length; i++)
						{
							char c = filename[i];

							if (Char.IsDigit(c))
								digits.Push(c);
							else
							{
								name = filename.Substring(i).Trim();
								break;
							}
						}
						int dependencyLevel = 0;
						for (int placeValue = 1; digits.Count > 0; placeValue *= 10)
						{
							int digitValue = digits.Pop() - '0';
							dependencyLevel += digitValue * placeValue;
						}

						var contents = new StreamReader(file, true).ReadToEnd();
						var hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(contents)));
						return new StoredCodeDefinition(name, hash, _runActionBuilder(file, contents), dependencyLevel);
					}
				)
				.OrderBy(c => c.DependencyLevel)
				.ToList();
		}
	}
}