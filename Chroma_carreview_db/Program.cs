using ChromaDB.Client;
using System.Globalization;
using CsvHelper;
using System.Net.Http;
using System.Formats.Asn1;
using Microsoft.Extensions.AI;

namespace Chroma_carreview_db
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Setting up Car Review Database...");

            // CSV file containing car reviews
            string csvFilePath = "car_reviews.csv";
            var records = ReadCsv(csvFilePath);

            Console.WriteLine($"Read {records.Count} car reviews from CSV.");

            // ChromaDB configuration
            var configOptions = new ChromaConfigurationOptions(uri: "http://localhost:8000/api/v1/");
            using var httpClient = new HttpClient();
            var client = new ChromaClient(configOptions, httpClient);

            Console.WriteLine($"ChromaDB Version: {await client.GetVersion()}");

            // Create or get collection for car reviews
            var collection = await client.GetOrCreateCollection("CarReviews");
            var collectionClient = new ChromaCollectionClient(collection, configOptions, httpClient);

            // Initialize embedding generator (using Ollama in this example)
            var generator = new OllamaEmbeddingGenerator(new Uri("http://localhost:11434"), modelId: "all-minilm");

            // Add reviews to ChromaDB
            foreach (var review in records)
            {
                // Combine relevant fields for embedding
                var textToEmbed = $"{review.Review_Title} {review.Car_Review} {review.Vehicle_Model} {review.Vehicle_Year}";

                var vector = await generator.GenerateEmbeddingVectorAsync(textToEmbed);

                // Store metadata with all review information
                var metadata = new Dictionary<string, object>

                {
                    { "review_title", review.Review_Title },
                    { "car_review", review.Car_Review },
                    { "rating", review.Rating },
                    { "vehicle_year", review.Vehicle_Year },
                    { "vehicle_model", review.Vehicle_Model }
                };

                await collectionClient.Add([review.Ids], [vector], [metadata]);
                Console.WriteLine($"Added {records.Count} reviews to ChromaDB.");

            }


            // Example query: Find similar reviews
            var queryText = "Reliable family SUV with good safety features";
            Console.WriteLine($"\nSearching for reviews similar to: '{queryText}'");

            var queryVector = await generator.GenerateEmbeddingVectorAsync(queryText);
            var results = await collectionClient.Query(queryVector, 5,
                include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances);

            // Display results
            Console.WriteLine("\nTop matching reviews:");
            foreach (var result in results)
            {
                Console.WriteLine($"\nID: {result.Id} | Similarity Score: {1 - result.Distance:F2}");

                if (result.Metadata != null)
                {
                    Console.WriteLine($"Model: {result.Metadata["vehicle_model"]} ({result.Metadata["vehicle_year"]})");
                    Console.WriteLine($"Rating: {result.Metadata["rating"]}/5");
                    Console.WriteLine($"Title: {result.Metadata["review_title"]}");
                    Console.WriteLine($"Review: {result.Metadata["car_review"]?.ToString()?.Truncate(150)}...");
                    Console.WriteLine("--------------------------------");
                }
            }

            Console.WriteLine("\nDone!");
        }

        static List<Review_Datas> ReadCsv(string csvFilePath)
        {
            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                return csv.GetRecords<Review_Datas>().ToList();
            }
        }
    }

    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}