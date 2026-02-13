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
        command.CommandText = @"
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

        CREATE TABLE IF NOT EXISTS credit_cards (
            id INTEGER PRIMARY KEY,
            person_id INTEGER NOT NULL,
            card_number TEXT NOT NULL,
            FOREIGN KEY(person_id) REFERENCES persons(id)
        );
        ";
        command.ExecuteNonQuery();
    }

    static void GenerateCreditCards()
    {
        var stopwatch = Stopwatch.StartNew();

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
        command.CommandText = "SELECT id FROM persons;";
        var personIds = new List<int>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                personIds.Add(reader.GetInt32(0));
            }
        }

        var random = new Random();
        int batchSize = 1000;
        var sb = new StringBuilder();
        int count = 0;

        foreach (var personId in personIds)
        {
            int numCards;

            double p = random.NextDouble();
            if (p < 0.7) numCards = 1;
            else if (p < 0.9) numCards = 2;
            else numCards = random.Next(3, 11);

            for (int c = 0; c < numCards; c++)
            {
                string cardNumber = GenerateFakeCardNumber(random);
                sb.Append($"({personId}, '{cardNumber}')");

                if (++count % batchSize == 0)
                {
                    var insertCmd = connection.CreateCommand();
                    insertCmd.CommandText = "INSERT INTO credit_cards (person_id, card_number) VALUES " + sb.ToString() + ";";
                    insertCmd.ExecuteNonQuery();
                    sb.Clear();
                }
                else
                {
                    sb.Append(",");
                }
            }
        }

        if (sb.Length > 0)
        {
            var insertCmd = connection.CreateCommand();
            insertCmd.CommandText = "INSERT INTO credit_cards (person_id, card_number) VALUES " + sb.ToString() + ";";
            insertCmd.ExecuteNonQuery();
        }

        transaction.Commit();
        stopwatch.Stop();

        Console.WriteLine($"Credit cards generated for {personIds.Count} persons.");
        Console.WriteLine($"Time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
}

    static string GenerateFakeCardNumber(Random random)
    {
        var digits = new int[16];
        for (int i = 0; i < 16; i++)
        {
            digits[i] = random.Next(0, 10);
        }
        return string.Join("", digits[0..4]) + " " +
            string.Join("", digits[4..8]) + " " +
            string.Join("", digits[8..12]) + " " +
            string.Join("", digits[12..16]);
    }

    static void ShowPersonsWithCards()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT p.first_name, p.last_name, c.card_number
            FROM persons p
            JOIN credit_cards c ON p.id = c.person_id
            LIMIT 20;
        ";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine($"{reader.GetString(0)} {reader.GetString(1)} - {reader.GetString(2)}");
        }
    }

    static void ShowMenu()
    {
        while (true)
        {
            Console.WriteLine("\n==== CREDIT CARD SYSTEM ====");
            Console.WriteLine("1. Import mock data (run once)");
            Console.WriteLine("2. Generate persons");
            Console.WriteLine("3. Generate credit cards");
            Console.WriteLine("4. Show sample persons with cards");
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
                case "3":
                    GenerateCreditCards();
                    break;
                case "4":
                    ShowPersonsWithCards();
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