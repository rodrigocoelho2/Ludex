using Ludex.WebApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLudex(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseLudex();
await app.RunAsync();
