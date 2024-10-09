var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient(); // HttpClient remains transient
builder.Services.AddSingleton<MilvusService, MilvusService>();// Milvus address
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "corsPolicy",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors("corsPolicy");
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();