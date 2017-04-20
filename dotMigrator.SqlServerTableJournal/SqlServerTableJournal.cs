using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotMigrator.SqlServerTableJournal
{
    public class SqlServerTableJournal : IJournal
    {
	    public void CreateJournal()
	    {
		    throw new NotImplementedException();
	    }

	    public void SetBaseline(string lastMigrationToBaseline, IMigrationsProvider migrationsProvider)
	    {
		    throw new NotImplementedException();
	    }

	    public void RecordStartMigration(Migration migrationToRun)
	    {
		    throw new NotImplementedException();
	    }

	    public void RecordCompleteMigration(Migration migrationToRun)
	    {
		    throw new NotImplementedException();
	    }

	    public void RecordStoredCodeDefinition(StoredCodeDefinition storedCodeDefinition, int lastMigrationNumber)
	    {
		    throw new NotImplementedException();
	    }

	    public IReadOnlyList<DeployedMigration> GetDeployedMigrations()
	    {
		    throw new NotImplementedException();
	    }

	    public IReadOnlyList<DeployedStoredCodeDefinition> GetDeployedStoredCodeDefinitions()
	    {
		    throw new NotImplementedException();
	    }
    }
}
