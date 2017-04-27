using System.Data.SqlClient;

namespace dotMigrator.SqlServer
{
	/// <summary>
	/// A class to encapsulate the properties of a Sql Server connection.
	/// </summary>
	public class ConnectionProperties
	{
		/// <summary>
		/// A connection string suitable for the SqlConnection object.
		/// </summary>
		public string ConnectionString { get; }
		
		/// <summary>
		/// The server and/or instance name of the SQL server to connect to
		/// </summary>
		public string ServerInstance { get; }
		
		/// <summary>
		/// The name of the database to connect to ("Initial Catalog")
		/// </summary>
		public string TargetDatabaseName { get; }

		/// <summary>
		/// The SQL authentication user name if not using windows integrated security
		/// </summary>
		public string SqlUserName { get; }

		/// <summary>
		/// The SQL authentication password if not using windows integrated security
		/// </summary>
		public string SqlUserPassword { get; }

		/// <summary>
		/// Indicates whether to use windows integrated security. When this is true, the SqlUserName and SqlUserPassword are ignored.
		/// </summary>
		public bool UseWindowsIntegratedSecurity { get; }

		/// <summary>
		/// Constructor to use when the individual connection property values are available.
		/// </summary>
		/// <param name="serverInstance"></param>
		/// <param name="targetDatabaseName"></param>
		/// <param name="sqlUserName"></param>
		/// <param name="sqlUserPassword"></param>
		/// <param name="useWindowsIntegratedSecurity"></param>
		public ConnectionProperties(
			string serverInstance,
			string targetDatabaseName,
			string sqlUserName = null,
			string sqlUserPassword = null,
			bool useWindowsIntegratedSecurity = true)
		{
			ServerInstance = serverInstance;
			TargetDatabaseName = targetDatabaseName;
			SqlUserName = sqlUserName;
			SqlUserPassword = sqlUserPassword;
			UseWindowsIntegratedSecurity = useWindowsIntegratedSecurity;

			var connectionBuilder = new SqlConnectionStringBuilder
			{
				DataSource = ServerInstance,
				ApplicationName = "dotMigrator",
				InitialCatalog = TargetDatabaseName,
			};
			if (UseWindowsIntegratedSecurity)
			{
				connectionBuilder.IntegratedSecurity = true;
			}
			else
			{
				connectionBuilder.UserID = SqlUserName;
				connectionBuilder.Password = SqlUserPassword;
			}
			ConnectionString = connectionBuilder.ConnectionString;
		}

		/// <summary>
		/// Constructor to use when specific connection string property values are needed, or when 
		/// a connection string is more easily available than the individual connection properties
		/// </summary>
		/// <param name="connectionString"></param>
		public ConnectionProperties(string connectionString)
		{
			ConnectionString = connectionString;
			var bldr = new SqlConnectionStringBuilder(connectionString);
			ServerInstance = bldr.DataSource;
			TargetDatabaseName = bldr.InitialCatalog;
			SqlUserName = bldr.UserID;
			SqlUserPassword = bldr.Password;
			UseWindowsIntegratedSecurity = bldr.IntegratedSecurity;
		}

		/// <summary>
		/// Creates and opens a new SqlConnection to the target database
		/// </summary>
		/// <returns></returns>
		public SqlConnection OpenConnection()
		{
			var connection = new SqlConnection(ConnectionString);
			connection.Open();
			return connection;

		}
	}
}