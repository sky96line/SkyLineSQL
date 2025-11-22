using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyLineSQL.Utility
{
    public class SSMSHelper
    {
        public static void OpenQueryInSSMS(string connectionString, string query)
        {
            // Parse connection string
            var builder = new SqlConnectionStringBuilder(connectionString);

            // Build temporary .sql file
            string tempFile = Path.Combine(Path.GetTempPath(), $"SSMSQuery_{DateTime.Now:yyyyMMddHHmmss}.sql");
            File.WriteAllText(tempFile, query);

            // Default SSMS path (you can adjust if you have a custom install)
            string ssmsPath = @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 19\Common7\IDE\Ssms.exe";
            if (!File.Exists(ssmsPath))
            {
                ssmsPath = @"C:\Program Files (x86)\Microsoft SQL Server Management Studio 18\Common7\IDE\Ssms.exe";
            }

            if (!File.Exists(ssmsPath))
                throw new FileNotFoundException("SSMS executable not found. Please verify your SSMS installation path.");

            // Build SSMS arguments
            string args;

            if (builder.IntegratedSecurity)
            {
                args = $"-S \"{builder.DataSource}\" -d \"{builder.InitialCatalog}\" -E \"{tempFile}\"";
            }
            else
            {
                // SSMS 18/19 does NOT support -P anymore → it will prompt user
                args = $"-S \"{builder.DataSource}\" -d \"{builder.InitialCatalog}\" -U \"{builder.UserID}\" \"{tempFile}\"";
            }

            // Launch SSMS
            Process.Start(new ProcessStartInfo
            {
                FileName = ssmsPath,
                Arguments = args,
                UseShellExecute = true
            });
        }
    }
}
