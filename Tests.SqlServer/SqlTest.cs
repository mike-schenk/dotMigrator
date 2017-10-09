using System;
using System.Data.SqlClient;
using dotMigrator.SqlServer;

namespace Tests.SqlServer
{
    public static class SqlTest
    {
        public static IDisposable EnsureNewDatabase(ConnectionProperties localConnectionProperties)
        {
            SqlConnectionStringBuilder bldr = new SqlConnectionStringBuilder
            {
                DataSource = localConnectionProperties.ServerInstance,
                IntegratedSecurity = true
            };
            using (SqlConnection conn = new SqlConnection(bldr.ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                var dbName = localConnectionProperties.TargetDatabaseName;
                cmd.CommandText = $@"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{dbName}';
USE [master];
if db_id('{dbName}') is not null
begin
	ALTER DATABASE [{dbName}] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [{dbName}];
end
CREATE DATABASE [{dbName}];
";
                cmd.ExecuteNonQuery();
            }
            return new Disposer(() => DropDb(localConnectionProperties));
        }

        public static void DropDb(ConnectionProperties localConnectionProperties)
        {
            SqlConnectionStringBuilder bldr = new SqlConnectionStringBuilder
            {
                DataSource = localConnectionProperties.ServerInstance,
                IntegratedSecurity = true
            };
            using (SqlConnection conn = new SqlConnection(bldr.ConnectionString))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                var dbName = localConnectionProperties.TargetDatabaseName;
                cmd.CommandText = $@"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{dbName}';
USE [master];
if db_id('{dbName}') is not null
begin
	ALTER DATABASE [{dbName}] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [{dbName}];
end
";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public class Disposer : IDisposable
    {
        private readonly Action _onDispose;

        public Disposer(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }
}