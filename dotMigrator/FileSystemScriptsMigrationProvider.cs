using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace dotMigrator
{
	public class FileSystemScriptsMigrationProvider : IMigrationsProvider
	{
		private readonly string _migrationsDirectory;
		private readonly string _migrationsFileWildcard;
		private readonly string _storedCodeDefinitionsDirectory;
		private readonly string _storedCodeDefinitionFileWildcard;
		private readonly IScriptRunner _scriptRunner;

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
			_scriptRunner = scriptRunner;
		}

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

								return new Migration(migrationNumber, name.ToString(), hash, isOnline, pr => _scriptRunner.Run(contents));
							}
						}
					}
				)
				.OrderBy(m => m.MigrationNumber)
				.ToList();
		}

		public IReadOnlyCollection<StoredCodeDefinition> GatherStoredCodeDefinitions()
		{
			throw new System.NotImplementedException();
		}
	}
}