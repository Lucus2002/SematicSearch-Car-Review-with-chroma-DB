using CarReviewAPI.models;
using CarReviewAPI.Services;
using ChromaDB.Client;
using Microsoft.Extensions.AI;
using System.Net.Http;

namespace CarReview.Search.Api.Services
{
    public class CarReviewService : ICarReviewService
    {
        private List<CarReviewResult> _reviewsCollection = new List<CarReviewResult>();
        private IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

        public async Task<List<CarReviewResult>> GetCarReviewsAsync(string? searchText = null, int limit = 5)
        {
            var configOptions = new ChromaConfigurationOptions(uri: "http://localhost:8000/api/v1/");

            using var httpClient = new HttpClient();
            var client = new ChromaClient(configOptions, httpClient);

            _reviewsCollection = new List<CarReviewResult>();

            var collection = await client.GetOrCreateCollection("CarReviews");
            var collectionClient = new ChromaCollectionClient(collection, configOptions, httpClient);

            var generator = new OllamaEmbeddingGenerator(new Uri("http://localhost:11434"), modelId: "all-minilm");
            _embeddingGenerator = generator;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return _reviewsCollection;
            }

            var queryVector = await _embeddingGenerator.GenerateEmbeddingVectorAsync(searchText);

            var result = await collectionClient.Query(queryVector, limit,
                include: ChromaQueryInclude.Metadatas | ChromaQueryInclude.Distances);

            foreach (var item in result)
            {
                var reviewResult = new CarReviewResult();
                reviewResult.Ids = item.Id;
                reviewResult.distance = 1 - item.Distance;

                if (item.Metadata != null)
                {
                    reviewResult.Vehicle_Model = item.Metadata["vehicle_model"]?.ToString();
                    reviewResult.Vehicle_Year = item.Metadata["vehicle_year"]?.ToString();
                    reviewResult.Rating = item.Metadata["rating"]?.ToString();
                    reviewResult.Review_Title = item.Metadata["review_title"]?.ToString();
                    reviewResult.Car_Review = item.Metadata["car_review"]?.ToString();
                }

                _reviewsCollection.Add(reviewResult);
            }

            return _reviewsCollection;
        }
    }
}