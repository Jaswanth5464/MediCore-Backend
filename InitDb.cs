using System;
using Microsoft.Data.SqlClient;
using System.IO;

var connectionString = "Server=medicoredb-instance.csl86qiwq40l.us-east-1.rds.amazonaws.com;Database=master;User Id=admin;Password=9293405122!Aa;TrustServerCertificate=True";
var scriptPath = @"MediCore.API\script.sql";

try
{
    Console.WriteLine("Connecting to RDS...");
    using var connection = new SqlConnection(connectionString);
    connection.Open();

    Console.WriteLine("Creating MediCoreDB database if it doesn't exist...");
    using (var cmd = new SqlCommand("IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'MediCoreDB') CREATE DATABASE MediCoreDB", connection))
    {
        cmd.ExecuteNonQuery();
    }

    Console.WriteLine("Switching to MediCoreDB...");
    connection.ChangeDatabase("MediCoreDB");

    Console.WriteLine("Reading script.sql...");
    string script = File.ReadAllText(scriptPath);

    Console.WriteLine("Executing schema initialization (this may take a minute)...");
    
    // Split script by GO if necessary, but for now try executing whole
    var commands = script.Split(new[] { "GO\r\n", "GO\n", "GO\r" }, StringSplitOptions.RemoveEmptyEntries);
    
    foreach (var commandText in commands)
    {
        if (string.IsNullOrWhiteSpace(commandText)) continue;
        using var command = new SqlCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    Console.WriteLine("Successfully initialized MediCoreDB!");
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
