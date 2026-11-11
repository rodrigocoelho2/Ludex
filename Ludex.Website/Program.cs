using Ludex.Website;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLudexWebsite(builder.Configuration);

var app = builder.Build();
app.UseLudexWebsite();
await app.RunAsync();
