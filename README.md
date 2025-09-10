# reporting-svc

Nightly reconciliation report for Finance. Reads booking + payment totals
directly from BookingDB and writes a CSV to a shared network drive
(`\\anytravel-fs01\finance\reports` on the original Windows setup — path is
now just logged, the actual file share mount was never migrated when this
was containerized... it wasn't containerized. It still runs as a scheduled
task on `anytravel-batch-vm-01`, same box as fare-calc-batch).

Nobody on the current Finance-facing dev rotation wrote this originally.
Treat changes carefully — there's no test coverage and no staging
environment to validate against.
