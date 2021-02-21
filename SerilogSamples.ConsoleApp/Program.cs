using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;

namespace SerilogSamples.ConsoleApp
{
    public class Program
    {
        private  static IConfiguration _configuration;

        public static void Main(string[] args)
        {
            try
            {
                SetupConfiguration();
                SetupSerilog();

                Log.Information("Information sample, this will be shown in both console and sql server table");
                Log.Debug("Debug sample, this will be shown only in console");
            }

            catch (Exception ex)
            {
                Log.Fatal(ex, "Exception was found.");
            }

            finally
            {
                Log.CloseAndFlush();
            }

        }

        public static void SetupConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();
        }

        private static void SetupSerilog()
        {
            var connectionString = _configuration.GetConnectionString("MyDb");

            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn> {
                    new SqlColumn { ColumnName = "Version" },
                    new SqlColumn { ColumnName = "ProcessId", PropertyName = "ProcessId", DataType = SqlDbType.BigInt}
                },

                DisableTriggers = true
            };

            columnOptions.Store.Remove(StandardColumn.MessageTemplate);
            columnOptions.Store.Remove(StandardColumn.Properties);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                // You could use literally any other sink from Nuget (ElasticSearch, Seq, etc.)

                // Logger minimum level for console is verbose
                .WriteTo.Logger
                    (l => l.MinimumLevel.Verbose().WriteTo.Console())

                // Logger minimum level for SQL Server is information
                .WriteTo.Logger
                (l => l.MinimumLevel.Information().WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "YourTableName",
                        AutoCreateSqlTable = true
                    },
                    columnOptions: columnOptions
                ))

                .Enrich.FromLogContext()
                //Optinal: if you want to enrich with PID
                .Enrich.WithProcessId()
                //Optional: if you want to enrich with property AssemblyVersion
                .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
                .CreateLogger();
        }
    }
}
