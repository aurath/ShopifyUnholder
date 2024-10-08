﻿using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ShopifyUnhold.Exceptions;
using System.Reflection;
using System.Text.Json;

namespace ShopifyUnhold.Cli;

public static class Program
{
    /// <summary>
    /// Removes manual holds on shopify orders.
    /// </summary>
    /// <param name="args">The order names, seperated by spaces, ranges supported with '-'</param>
    /// <param name="consoleLogLevel">The logging level to print to the console</param>
    public static async Task Main(string[] args, LogLevel consoleLogLevel = LogLevel.None)
    {
        var root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        // Create new log file in the logging directory
        using var logFileWriter = SetupLoggingFile(root);
        using ILoggerFactory factory = LoggerFactory.Create(builder =>
            builder
                .AddSimpleConsole(configure => configure.SingleLine = true)
                .AddFilter<ConsoleLoggerProvider>("ShopifyUnhold", consoleLogLevel)
                .AddProvider(new FileLoggerProvider(logFileWriter))
                .AddFilter<FileLoggerProvider>("ShopifyUnhold", LogLevel.Debug));

        var logger = factory.CreateLogger("ShopifyUnhold");

        try
        {
            var config = ReadConfig(root);
            logger.LogInformation("Read config file");

            // Parse ranges of input order names
            var names = Range.Parse(args).ToList();
            logger.LogInformation("Parsed {namesCount} order names:", names.Count);
            logger.LogInformation("{names}", JsonSerializer.Serialize(names));

            if (names.Count is 0)
            {
                Console.WriteLine("No orders input, exiting");
                return;
            }
            Console.WriteLine($"Total of {names.Count} orders input");
            
            // Configure GraphQL
            var http = new HttpClient();
            http.SetShopifyToken(config.Token);

            var endpoint = $"https://{config.Store}.myshopify.com/admin/api/2024-07/graphql.json";
            var client = new GraphQLHttpClient(endpoint, new SystemTextJsonSerializer(), http);
            logger.LogInformation("Built GraphQL client at endpoint {endpoint}", endpoint);

            try
            {
                // Find fulfillment orders based on input order names
                var finder = new FulfillmentOrderFinder(client, factory.CreateLogger<FulfillmentOrderFinder>());
                var results = await finder.Find(names, config.Location);
                var orderCount = results.FulfillmentOrders.Count;
                
                Console.WriteLine($"Found {orderCount} fulfillment orders");
                if (results.MissingNames.Count > 0)
                {
                    Console.WriteLine("Some order numbers could not be correlated with fulfillment orders:");
                    foreach(var name in results.MissingNames) Console.WriteLine(name);
                }

                // Request to unhold the found orders
                var unholder = new Unholder(client, factory.CreateLogger<Unholder>());
                await unholder.Unhold(results.FulfillmentOrders);

                Console.WriteLine($"Removed hold on {orderCount} orders");
            }
            catch (UserErrorsException e)
            {
                Console.WriteLine(e.Message + ":");
                foreach (var error in e.Errors)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled Exception! {e}", e);
            Console.WriteLine($"Unhandled Exception! {e.Message}");
        }
    }

    private static Config ReadConfig(string root)
    {
        var file = new FileInfo(Path.Combine(root, "config.json"));

        using var stream = file.OpenRead();
        return System.Text.Json.JsonSerializer.Deserialize<Config>(stream) 
            ?? throw new InvalidOperationException("Failed to read config file");
    }

    private static StreamWriter SetupLoggingFile(string root)
    {
        var logDir = Path.Combine(root, "logs");
        if (Directory.Exists(logDir) is false) Directory.CreateDirectory(logDir);

        var logFileName = $"{DateTime.Now:yyyy-M-dd--HH-mm-ss}.log";
        var logFilePath = Path.Combine(logDir, logFileName);
        return new StreamWriter(logFilePath, false);
    }
}