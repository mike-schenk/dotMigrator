using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace dotMigrator.SqlServerTableJournal
{
	public class SqlServerTableJournal : IJournal
	{
		private readonly string _serverInstance;
		private readonly string _targetDatabaseName;
		private readonly string _sqlUserName;
		private readonly string _sqlUserPassword;
		private readonly bool _useWindowsIntegratedSecurity;
		private readonly IProgressReporter _progressReporter;

		private SqlConnection _connection;
		private SqlCommand _selectCommand;
		private SqlCommand _insertCommand;
		private SqlCommand _upsertCommand;
		private SqlCommand _setCompleteCommand;

		public SqlServerTableJournal(
			string serverInstance,
			string targetDatabaseName,
			string sqlUserName,
			string sqlUserPassword,
			bool useWindowsIntegratedSecurity,
			IProgressReporter progressReporter)
		{
			_serverInstance = serverInstance;
			_sqlUserName = sqlUserName;
			_sqlUserPassword = sqlUserPassword;
			_useWindowsIntegratedSecurity = useWindowsIntegratedSecurity;
			_progressReporter = progressReporter;
			_targetDatabaseName = targetDatabaseName;
		}

		public void Open()
		{
			if (_connection != null)
				return;
			// open our Sql connection and look for the _DeployedScripts table
			var connectionBuilder = new SqlConnectionStringBuilder { DataSource = _serverInstance, ApplicationName = "dotMigrator", InitialCatalog = _targetDatabaseName };
			if (_useWindowsIntegratedSecurity)
			{
				connectionBuilder.IntegratedSecurity = true;
			}
			else
			{
				connectionBuilder.UserID = _sqlUserName;
				connectionBuilder.Password = _sqlUserPassword;
			}
			_connection = new SqlConnection(connectionBuilder.ConnectionString);
			_connection.Open();

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

				_progressReporter.Report($"Preparing database \"{_targetDatabaseName}\" for future deployments...");
				createTableCommand.ExecuteNonQuery();
				_progressReporter.Report("Done");
			}
		}

		public void SetBaseline(IEnumerable<Migration> baselineMigrations)
		{
			// first we'll call CreateJournal to ensure the table is already set up.
			CreateJournal();

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
			}
		}

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

		public void RecordCompleteMigration(Migration migration)
		{
			_setCompleteCommand.Parameters["@Name"].Value = migration.Name;
			_setCompleteCommand.Parameters["@CompletedTs"].Value = DateTime.Now;
			_setCompleteCommand.ExecuteNonQuery();
		}

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
	}
}
