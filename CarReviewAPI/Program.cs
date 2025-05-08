using CarReview.Search.Api.Services;
using CarReviewAPI;
using CarReviewAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICarReviewService, CarReviewService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());


app.MapGet("/api/car_review", async (ICarReviewService carReviewService, string? term = null, int limit = 5) =>
{
    var symptoms = await carReviewService.GetCarReviewsAsync(term, limit);

    return Results.Ok(symptoms);
});
app.UseAuthorization();

app.MapControllers();

app.Run();
