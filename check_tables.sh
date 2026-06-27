docker exec -i microservices-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P Anand@raj12345 -d InventoryDb -C -Q "SELECT name FROM sys.tables WHERE name LIKE '%Quotation%'"
