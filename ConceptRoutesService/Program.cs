using Azure;
using Azure.Maps.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string mapsKey = "QrWwW2blyz6v8C577UTBiysZn-fSgVTc2FxZDyoqymk";
builder.Services.AddSingleton(new MapsRoutingClient(new AzureKeyCredential(mapsKey)));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("ORS", client =>
{
    client.BaseAddress = new Uri("https://api.openrouteservice.org/");
});

builder.Services.AddHttpClient("URLExpander", client =>
{
    // Aquí puedes configurar cosas globales como el User-Agent si fuera necesario
    client.DefaultRequestHeaders.Add("User-Agent", "HttpClient-LinkExpander");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
