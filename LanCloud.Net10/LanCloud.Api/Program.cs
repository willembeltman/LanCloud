using gAPI.Generated;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAutoWss("Console app");

var app = builder.Build();
app.MapAutoWss();
app.UseHttpsRedirection();
app.Run();
