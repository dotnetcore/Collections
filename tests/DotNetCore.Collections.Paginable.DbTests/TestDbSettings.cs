using System;
using System.IO;

namespace DotNetCore.Collections.Paginable.DbTests
{
    /// <summary>
    /// Provides database connection settings for DbTests.
    /// <para>
    /// Resolution order:
    /// 1. Environment variable <c>PAGINABLE_DBTESTS_CONNECTION_STRING</c> (full connection string, highest priority).
    /// 2. Default: LocalDB attaching <c>DataSource/Samples.mdf</c> inside the test project directory
    ///    (located by walking up from the test binaries, so it works on any machine/checkout path).
    /// </para>
    /// <para>
    /// NOTE: <c>Samples.mdf</c> is not committed (gitignored). Create it once by executing
    /// <c>Scripts/TestDataScript.sql</c> against a LocalDB instance named <c>(LocalDB)\MSSQLLocalDB</c>,
    /// or set the environment variable above to point to your own database.
    /// </para>
    /// </summary>
    internal static class TestDbSettings
    {
        private const string ConnectionStringEnvVar = "PAGINABLE_DBTESTS_CONNECTION_STRING";
        private const string TestProjectFolderName = "DotNetCore.Collections.Paginable.DbTests";
        private const string DefaultDatabaseFileName = "Samples.mdf";

        /// <summary>
        /// Gets the connection string used by all DbTests.
        /// </summary>
        public static readonly string ConnectionString = ResolveConnectionString();

        private static string ResolveConnectionString()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return fromEnvironment;
            }

            return $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={ResolveDefaultMdfPath()};Integrated Security=True";
        }

        private static string ResolveDefaultMdfPath()
        {
            // Walk up from the test binaries (bin/.../) until we reach the test project folder,
            // so the default path follows the actual checkout location instead of a hardcoded one.
            var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (current != null)
            {
                if (string.Equals(current.Name, TestProjectFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.Combine(current.FullName, "DataSource", DefaultDatabaseFileName);
                }

                current = current.Parent;
            }

            // Fallback: relative to the binaries if the project folder could not be located.
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DataSource", DefaultDatabaseFileName);
        }
    }
}
