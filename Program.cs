using Microsoft.Data.SqlClient;

// Nightly finance reconciliation export. Same BookingDB as booking-api and
// fare-calc-batch, same VM as fare-calc-batch, different crontab entry.
// Query here is read-only, but it joins across Bookings and Payments
// directly — if either table's schema changes (e.g. during the Aurora
// PostgreSQL migration), this breaks silently since there's no monitoring
// on this job beyond someone in Finance noticing the report didn't land.

var host = Environment.GetEnvironmentVariable("RDS_HOSTNAME") ?? "localhost";
var port = Environment.GetEnvironmentVariable("RDS_PORT") ?? "1433";
var db = Environment.GetEnvironmentVariable("RDS_DB_NAME") ?? "BookingDB";
var user = Environment.GetEnvironmentVariable("RDS_USERNAME") ?? "sa";
var password = Environment.GetEnvironmentVariable("RDS_PASSWORD") ?? "changeme";

var connectionString = $"Server={host},{port};Database={db};User Id={user};Password={password};TrustServerCertificate=True;";

var outputPath = Environment.GetEnvironmentVariable("REPORT_OUTPUT_PATH") ?? "./reconciliation-report.csv";

Console.WriteLine($"[{DateTime.UtcNow:o}] reporting-svc starting, output={outputPath}");

using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();

using var cmd = new SqlCommand(@"
    SELECT b.BookingId, b.PnrCode, b.FlightNumber, b.FareAmount, p.PaymentStatus, p.Amount
    FROM Bookings b
    LEFT JOIN Payments p ON p.BookingId = b.BookingId
    WHERE b.CreatedAtUtc >= DATEADD(day, -1, SYSUTCDATETIME())", conn);

using var reader = await cmd.ExecuteReaderAsync();
using var writer = new StreamWriter(outputPath);
await writer.WriteLineAsync("BookingId,PnrCode,FlightNumber,FareAmount,PaymentStatus,PaymentAmount");

var rowCount = 0;
while (await reader.ReadAsync())
{
    await writer.WriteLineAsync(string.Join(",",
        reader["BookingId"],
        reader["PnrCode"],
        reader["FlightNumber"],
        reader["FareAmount"],
        reader["PaymentStatus"],
        reader["Amount"]));
    rowCount++;
}

Console.WriteLine($"[{DateTime.UtcNow:o}] reporting-svc complete. {rowCount} rows written to {outputPath}.");
