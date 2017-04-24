using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace dotMigrator.SqlServer2012.SmoScriptRunner
{
	public class ScriptRunner : IScriptRunner, IDisposable
	{
		private readonly string _serverInstance;
		private readonly string _sqlUserName;
		private readonly string _sqlUserPassword;
		private readonly bool _useWindowsIntegratedSecurity;
		private readonly string _targetDatabaseName;
		private Database _database;
		private Server _server;

		public ScriptRunner(
			string serverInstance,
			string targetDatabaseName,
			string sqlUserName,
			string sqlUserPassword,
			bool useWindowsIntegratedSecurity)
		{
			_serverInstance = serverInstance;
			_sqlUserName = sqlUserName;
			_sqlUserPassword = sqlUserPassword;
			_useWindowsIntegratedSecurity = useWindowsIntegratedSecurity;
			_targetDatabaseName = targetDatabaseName;
		}


		public void Open()
		{
			ServerConnection serverConnection =
				_useWindowsIntegratedSecurity
					? new ServerConnection(_serverInstance)
					: new ServerConnection(_serverInstance, _sqlUserName, _sqlUserPassword);

			_server = new Server(serverConnection);
			_server.ConnectionContext.Connect();

			_database = _server.Databases[_targetDatabaseName];
			if (_database == null)
			{
				throw new Exception($"Database {_targetDatabaseName} does not exist at target {_serverInstance}");
			}
		}

		public void Run(string scriptToRun)
		{
			// we'll always run the script in the prescribed database
			_database.ExecuteNonQuery(scriptToRun);
		}

		public void Dispose()
		{
			_server.ConnectionContext.Disconnect();
		}
	}
}
