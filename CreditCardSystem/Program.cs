using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Text;

class Program
{
    static string connectionString = "Data Source=creditcard.db";
    static void Main()
    {
        CreateDatabase();
        ShowMenu();
    }

    static void CreateDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
        @"
        CREATE TABLE IF NOT EXISTS persons (
            id INTEGER PRIMARY KEY,
            first_name TEXT NOT NULL,
            last_name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS mock_data (
            id INTEGER PRIMARY KEY,
            first_name TEXT,
            last_name TEXT
        );
        ";

        command.ExecuteNonQuery();
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("\n==== CREDIT CARD SYSTEM ====");
            Console.WriteLine("1. Import mock data (run once)");
            Console.WriteLine("2. Generate persons");
            Console.WriteLine("0. Exit");
            Console.Write("Choice: ");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    ImportMockData();
                    break;
                case "2":
                    GeneratePersonsMenu();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }
    }

    static void ImportMockData()
    {
        if (!File.Exists("mockdata.sql"))
        {
            Console.WriteLine("mockdata.sql not found!");
            return;
        }
        var stopwatch = Stopwatch.StartNew();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        string[] lines = File.ReadAllLines("mockdata.sql");

        int count = 0;

        foreach (var line in lines)
        {
            var command = connection.CreateCommand();
            command.CommandText = line;
            command.ExecuteNonQuery();
            count++;
        }
        transaction.Commit();
        stopwatch.Stop();

        Console.WriteLine($"Imported {count} rows.");
        Console.WriteLine($"Time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    static (List<string>, List<string>) LoadNames()
    {
        var firstNames = new List<string>();
        var lastNames = new List<string>();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT first_name, last_name FROM mock_data;";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            firstNames.Add(reader.GetString(0));
            lastNames.Add(reader.GetString(1));
        }

        return (firstNames, lastNames);
    }

    static void GeneratePersonsMenu()
    {
        Console.Write("How many persons? (default 10 000 000): ");
        var input = Console.ReadLine();

        int amount = 10000000;
        var stopwatch = Stopwatch.StartNew();

        if (!string.IsNullOrWhiteSpace(input))
        {
            int.TryParse(input, out amount);
        }

        GeneratePersons(amount);
        stopwatch.Stop();
        Console.WriteLine($"Time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
    }

    static void GeneratePersons(int amount)
    {
        var (firstNames, lastNames) = LoadNames();

        if (firstNames.Count == 0)
        {
            Console.WriteLine("No mock data found. Import mock data first!");
            return;
        }

        var random = new Random();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var pragma = connection.CreateCommand();
        pragma.CommandText = @"
            PRAGMA journal_mode = OFF;
            PRAGMA synchronous = OFF;
            PRAGMA temp_store = MEMORY;
            PRAGMA cache_size = 1000000;
        ";
        pragma.ExecuteNonQuery();

        using var transaction = connection.BeginTransaction();

        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO persons (first_name, last_name) VALUES ($first, $last);";
        var firstParam = command.CreateParameter();
        firstParam.ParameterName = "$first";
        command.Parameters.Add(firstParam);
        var lastParam = command.CreateParameter();
        lastParam.ParameterName = "$last";
        command.Parameters.Add(lastParam);

        for (int i = 0; i < amount; i++)
        {
            firstParam.Value = firstNames[random.Next(firstNames.Count)];
            lastParam.Value = lastNames[random.Next(lastNames.Count)];
            command.ExecuteNonQuery();
        }

        transaction.Commit();

        Console.WriteLine($"{amount} persons generated!");
    }
}