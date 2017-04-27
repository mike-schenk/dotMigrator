using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace dotMigrator.SqlServer
{
	/// <summary>
	/// Holds the journal for both migrations and stored code definitions in a single table
	/// </summary>
	public class SingleTableJournal : IJournal, IDisposable
	{
		private readonly ConnectionProperties _connectionProperties;
		private readonly IProgressReporter _progressReporter;

		private SqlConnection _connection;
		private SqlCommand _selectCommand;
		private SqlCommand _insertCommand;
		private SqlCommand _upsertCommand;
		private SqlCommand _setCompleteCommand;

		/// <summary>
		/// Constructs the journal object which will use the supplied connection properties and holds the progressReporter without connecting to the database
		/// </summary>
		/// <param name="connectionProperties"></param>
		/// <param name="progressReporter"></param>
		public SingleTableJournal(
			ConnectionProperties connectionProperties,
			IProgressReporter progressReporter)
		{
			_connectionProperties = connectionProperties;
			_progressReporter = progressReporter;
		}

		/// <summary>
		/// Connects to the target database and prepares to read and write from the journal table "_DeployedScripts"
		/// </summary>
		public void Open()
		{
			if (_connection != null)
				return;

			_connection = _connectionProperties.OpenConnection();

			_selectCommand = _connection.CreateCommand();
			_selectCommand.CommandText =
				"SELECT MigrationNumber, Name, Complete, Fingerprint " +
				"FROM _DeployedScripts " +
				"WHERE Repeatable = @Repeatable " +
				"ORDER BY MigrationNumber";
			_selectCommand.Parameters.Add("@Repeatable", SqlDbType.Bit);

			_insertCommand = _connection.CreateCommand();
			_insertCommand.CommandType = CommandType.Text;
			_insertCommand.CommandText =
				"INSERT INTO _DeployedScripts (MigrationNumber, Name, Repeatable, Complete, CompletedTs, Fingerprint) " +
				"VALUES (@MigrationNumber, @Name, @Repeatable, @Complete, @CompletedTs, @Fingerprint)";
			_insertCommand.Parameters.Add("@MigrationNumber", SqlDbType.Int);
			_insertCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 260);
			_insertCommand.Parameters.Add("@Repeatable", SqlDbType.Bit);
			_insertCommand.Parameters.Add("@Complete", SqlDbType.Bit);
			_insertCommand.Parameters.Add("@CompletedTs", SqlDbType.DateTime2);
			_insertCommand.Parameters.Add("@Fingerprint", SqlDbType.NVarChar, 50);

			_upsertCommand = _connection.CreateCommand();
			_upsertCommand.CommandType = CommandType.Text;
			_upsertCommand.CommandText =
				"UPDATE _DeployedScripts " +
				"SET " +
				"	MigrationNumber = @MigrationNumber, " +
				" Complete = @Complete, " +
				"	CompletedTs = @CompletedTs, " +
				"	Fingerprint = @Fingerprint " +
				"WHERE Name = @Name " +
				"IF @@ROWCOUNT = 0 " +
				"BEGIN " +
				"	INSERT INTO _DeployedScripts (MigrationNumber, Name, Repeatable, Complete, CompletedTs, Fingerprint) " +
				" VALUES(@MigrationNumber, @Name, @Repeatable, @Complete, @CompletedTs, @Fingerprint) " +
				"END";
			_upsertCommand.Parameters.Add("@MigrationNumber", SqlDbType.Int);
			_upsertCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 260);
			_upsertCommand.Parameters.Add("@Repeatable", SqlDbType.Bit);
			_upsertCommand.Parameters.Add("@Complete", SqlDbType.Bit);
			_upsertCommand.Parameters.Add("@CompletedTs", SqlDbType.DateTime2);
			_upsertCommand.Parameters.Add("@Fingerprint", SqlDbType.NVarChar, 50);

			_setCompleteCommand = _connection.CreateCommand();
			_setCompleteCommand.CommandType = CommandType.Text;
			_setCompleteCommand.CommandText =
				"UPDATE _DeployedScripts " +
				"  Complete = 1 " +
				"  CompletedTs = @CompletedTs " +
				"WHERE Name = @Name";
			_setCompleteCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 260);
			_setCompleteCommand.Parameters.Add("@CompletedTs", SqlDbType.DateTime2);
		}

		/// <summary>
		/// Ensures the journal table "[dbo].[_DeployedScripts]" is present in the target database, creating it if necessary.
		/// </summary>
		public void CreateJournal()
		{
			var findTableCommand = new SqlCommand("SELECT OBJECT_ID('_DeployedScripts')", _connection);
			if (findTableCommand.ExecuteScalar() == DBNull.Value)
			{
				// then we need to create it.
				var createTableCommand = new SqlCommand(
					@"CREATE TABLE [dbo].[_DeployedScripts](
	[MigrationNumber] int NOT NULL, 
	[Name] [nvarchar](260) NOT NULL PRIMARY KEY, 
	[Repeatable] bit NOT NULL, 
	[Complete] bit NOT NULL, 
	[CompletedTs] datetime2 NULL,
	[Fingerprint] [nvarchar](50) NOT NULL
)",
					_connection);

				_progressReporter.Report($"Preparing database \"{_connectionProperties.TargetDatabaseName}\" for future deployments...");
				createTableCommand.ExecuteNonQuery();
				_progressReporter.Report("Done");
			}
		}

		/// <summary>
		/// Records that a series of migrations has already been completed in a target data store.
		/// This is used when an existing data store is being put under management by dotMigrator
		/// </summary>
		/// <param name="baselineMigrations"></param>
		/// <returns></returns>
		public IReadOnlyList<DeployedMigration> SetBaseline(IEnumerable<Migration> baselineMigrations)
		{
			// first we'll call CreateJournal to ensure the table is already set up.
			CreateJournal();

			var toReturn = new List<DeployedMigration>();

			foreach (var migration in baselineMigrations)
			{
				// insert into our table
				_insertCommand.Parameters["@MigrationNumber"].Value = migration.MigrationNumber;
				_insertCommand.Parameters["@Name"].Value = migration.Name;
				_insertCommand.Parameters["@Repeatable"].Value = false;
				_insertCommand.Parameters["@Complete"].Value = true;
				_insertCommand.Parameters["@CompletedTs"].Value = DateTime.Now;
				_insertCommand.Parameters["@Fingerprint"].Value = migration.Fingerprint;
				_insertCommand.ExecuteNonQuery();
				toReturn.Add(new DeployedMigration(migration.MigrationNumber, migration.Name, migration.Fingerprint, true));
			}
			return toReturn.OrderBy(m => m.MigrationNumber).ToList();
		}

		/// <summary>
		/// Insert or update the migration identified by its name in the journal.
		/// It will be recorded as an incomplete migration.
		/// </summary>
		/// <param name="migration"></param>
		public void RecordStartMigration(Migration migration)
		{
			// insert or update the fingerprint of a migration.
			_upsertCommand.Parameters["@MigrationNumber"].Value = migration.MigrationNumber;
			_upsertCommand.Parameters["@Name"].Value = migration.Name;
			_upsertCommand.Parameters["@Repeatable"].Value = false;
			_upsertCommand.Parameters["@Complete"].Value = false;
			_upsertCommand.Parameters["@CompletedTs"].Value = DateTime.Now;
			_upsertCommand.Parameters["@Fingerprint"].Value = migration.Fingerprint;
			_upsertCommand.ExecuteNonQuery();
		}

		/// <summary>
		/// Update the identified migration in the journal as having been completed.
		/// It can be assumed that this will only be called for the most recently started mgiration.
		/// </summary>
		/// <param name="migration"></param>
		public void RecordCompleteMigration(Migration migration)
		{
			_setCompleteCommand.Parameters["@Name"].Value = migration.Name;
			_setCompleteCommand.Parameters["@CompletedTs"].Value = DateTime.Now;
			_setCompleteCommand.ExecuteNonQuery();
		}

		/// <summary>
		/// Insert a record to the journal that a new stored code definition has been completely applied, 
		/// or update an existing record with the new fingerprint of a stored code definition that has 
		/// just been applied.
		/// </summary>
		/// <param name="storedCodeDefinition"></param>
		/// <param name="lastMigrationNumber"></param>
		public void RecordStoredCodeDefinition(StoredCodeDefinition storedCodeDefinition, int lastMigrationNumber)
		{
			// insert or update the fingerprint of a stored code definition.
			_upsertCommand.Parameters["@MigrationNumber"].Value = lastMigrationNumber;
			_upsertCommand.Parameters["@Name"].Value = storedCodeDefinition.Name;
			_upsertCommand.Parameters["@Repeatable"].Value = true;
			_upsertCommand.Parameters["@Complete"].Value = true;
			_upsertCommand.Parameters["@CompletedTs"].Value = DateTime.Now;
			_upsertCommand.Parameters["@Fingerprint"].Value = storedCodeDefinition.Fingerprint;
			_upsertCommand.ExecuteNonQuery();
		}

		/// <summary>
		/// Returns the sequenced list of offline and online migrations that have been recorded in the journal
		/// </summary>
		public IReadOnlyList<DeployedMigration> GetDeployedMigrations()
		{
			var toReturn = new List<DeployedMigration>();
			_selectCommand.Parameters["@Repeatable"].Value = false;
			using (var rdr = _selectCommand.ExecuteReader())
			{
				while (rdr.Read())
				{
					var migrationNumber = rdr.GetInt32(0);
					var name = rdr.GetString(1);
					var complete = rdr.GetBoolean(2);
					var fingerprint = rdr.GetString(3);
					var deployedScript = new DeployedMigration(migrationNumber, name, fingerprint, complete);
					toReturn.Add(deployedScript);
				}
			}
			return toReturn;
		}

		/// <summary>
		/// Returns all of the stored code definitions that have been previously applied as recorded in this journal.
		/// The order of the definitions does not matter since the names are used to match them up with the available ones
		/// that the MigrationsProvider finds.
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<DeployedStoredCodeDefinition> GetDeployedStoredCodeDefinitions()
		{
			var toReturn = new List<DeployedStoredCodeDefinition>();
			_selectCommand.Parameters["@Repeatable"].Value = true;
			using (var rdr = _selectCommand.ExecuteReader())
			{
				while (rdr.Read())
				{
					//var migrationNumber = rdr.GetInt32(0);
					var name = rdr.GetString(1);
					//var complete = rdr.GetBoolean(2);
					var fingerprint = rdr.GetString(3);
					var deployedScript = new DeployedStoredCodeDefinition(name, fingerprint);
					toReturn.Add(deployedScript);
				}
			}
			return toReturn;
		}

		/// <summary>
		/// Close the connection to the database
		/// </summary>
		public void Dispose()
		{
			_connection?.Dispose();
			_connection = null;
			_selectCommand?.Dispose();
			_insertCommand?.Dispose();
			_upsertCommand?.Dispose();
			_setCompleteCommand?.Dispose();
		}
	}
}
