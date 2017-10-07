using System.Data.SqlClient;
using dotMigrator.SqlServer;

namespace Tests.SqlServer
{
    public static class SqlTest
    {
        public static void EnsureNewDatabase(ConnectionProperties localConnectionProperties)
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
                cmd.CommandText = @"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'dotMigratorTests';
USE [master];
if db_id('dotMigratorTests') is not null
begin
	ALTER DATABASE [dotMigratorTests] SET  SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [dotMigratorTests];
end
CREATE DATABASE [dotMigratorTests];
";
                cmd.ExecuteNonQuery();
            }
        }
    }
}