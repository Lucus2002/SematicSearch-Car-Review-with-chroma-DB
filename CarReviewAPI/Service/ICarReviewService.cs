// ICarReviewService.cs
using CarReviewAPI.models;

namespace CarReviewAPI.Services
{
    public interface ICarReviewService
    {
        Task<List<CarReviewResult>> GetCarReviewsAsync(string? searchText = null, int limit = 5);
    }
}