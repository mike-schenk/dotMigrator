using System;
using dotMigrator.SqlServer;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace dotMigrator.SqlServer2012.SmoScriptRunner
{
	/// <summary>
	/// Uses SQL Server Managment Objects to run SQL scripts that may include multiple batches separated by "GO" statements.
	/// Scripts run by this runner should <i>not</i> have a "USE [db]" statement, since this runner will switch to the 
	/// indicated target database before running each script.
	/// </summary>
	public class ScriptRunner : IScriptRunner, IDisposable
	{
		private readonly ConnectionProperties _connectionProperties;
		private Database _database;
		private Server _server;

		/// <summary>
		/// Constructs a ScriptRunner that can run scripts in the database identified in the given connectionProperties
		/// </summary>
		/// <param name="connectionProperties"></param>
		public ScriptRunner(
			ConnectionProperties connectionProperties)
		{
			_connectionProperties = connectionProperties;
		}

		/// <summary>
		/// Opens a connection to the target database and verifies its existence so that it's ready to execute scripts
		/// </summary>
		public void Open()
		{
			ServerConnection serverConnection =
				_connectionProperties.UseWindowsIntegratedSecurity
					? new ServerConnection(_connectionProperties.ServerInstance)
					: new ServerConnection(_connectionProperties.ServerInstance, _connectionProperties.SqlUserName, _connectionProperties.SqlUserPassword);

			_server = new Server(serverConnection);
			_server.ConnectionContext.Connect();

			_database = _server.Databases[_connectionProperties.TargetDatabaseName];
			if (_database == null)
			{
				throw new Exception($"Database {_connectionProperties.TargetDatabaseName} does not exist on server {_connectionProperties.ServerInstance}");
			}
		}

		/// <summary>
		/// Executes the given SQL script in the target database. The scriptToRun should <i>not</i> have a "USE [db]" statement.
		/// </summary>
		/// <param name="scriptToRun"></param>
		public void Run(string scriptToRun)
		{
			// we'll always run the script in the prescribed database
			_database.ExecuteNonQuery(scriptToRun);
		}

		/// <summary>
		/// Disconnects from the SQL Server
		/// </summary>
		public void Dispose()
		{
			_server.ConnectionContext.Disconnect();
		}
	}
}
