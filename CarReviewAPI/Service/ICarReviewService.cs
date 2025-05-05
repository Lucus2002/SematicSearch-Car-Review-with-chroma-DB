using CarReviewAPI.models;

namespace CarReview.Search.Api.Services
{
    public interface ICarReviewService
    {
        Task<List<CarReviewResult>> GetCarReviewsAsync(string? searchText = null, int limit = 5);
    }
}