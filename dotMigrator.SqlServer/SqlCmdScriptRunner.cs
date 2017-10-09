using System;
using System.Diagnostics;
using System.Text;

namespace dotMigrator.SqlServer
{
    public class SqlCmdScriptRunner : IScriptFileRunner
    {
        private readonly ConnectionProperties _localConnectionProperties;

        public SqlCmdScriptRunner(ConnectionProperties localConnectionProperties)
        {
            _localConnectionProperties = localConnectionProperties;
        }

        public void Run(string absoluteScriptFileName)
        {
            /* Notes:
             * It's possible to pass a whole list of script filenames at once. Would that be a big benefit?
             * -b causes the script execution to stop and sqlcmd to exit with return value 1 as soon as it encounters an error with severity level > 10
             * -r0 causes messages with severity level > 10 to be written to standard error
             * -e causes all the input to be echoed back to stdout
             * 
             * When a script has multiple batches (separated by "GO") and there is an error, the reported line number is relative to the batch. so:
             *  - we probably want to have sqlcmd echo back the lines so we can hold on to them and include the text of the batch that failed if there's an error... 
             *    however we don't get the "GO" statements in the echoed output, so that isn't as useful... 
             *    I think the best we can do is just include the last X number of lines before an error was encountered.
             *  - we could consider pre-processing the file to print out some identifier at the beginning of each batch
             */

            //TODO: establish the authentication properties correctly
            var psi = new ProcessStartInfo("sqlcmd.exe", $"-d {_localConnectionProperties.TargetDatabaseName} -E -S (local) -b -r0 -e -i \"{absoluteScriptFileName}\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var process = new Process {StartInfo = psi, EnableRaisingEvents = true};
            var stdError = new StringBuilder();
            var stdOut = new StringBuilder();
            // pass-through anything written to StandardError
            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    stdError.AppendLine(args.Data);
                    Console.Error.WriteLine(args.Data);
                }
            };
            // capture everything written to StandardOutput
            process.OutputDataReceived += (sender, args) =>
            {
                if(args.Data != null)
                    stdOut.AppendLine(args.Data);
                // Do we want to output to the progress reporter?
                // Do we want to swallow all output?
                // Console.Out.WriteLine(args.Data);
            };
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            Console.Out.WriteLine("\r\n\r\n" + stdOut);

            if (process.ExitCode != 0)
                throw new Exception("Script failed.\r\n" + stdError);
        }
    }
}