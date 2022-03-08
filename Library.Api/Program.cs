using FluentValidation;
using Library.Api.Auth;
using Library.Api.Data;
using Library.Api.EndPoints;

var builder = WebApplication.CreateBuilder(args);

/* Configure */
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Sqlite");
builder.Services.AddSingleton<IDbConnectionFactory>(_ =>
    new SqliteConnectionFactory(connectionString));
builder.Services.AddSingleton<DatabaseInitializer>();

builder.Services.AddLibraryEndPoints();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// API Key-Based Authentication as a Custom Auth Scheme
builder.Services.AddAuthentication(ApiKeySchemeConstants.SchemeName)
    .AddScheme<ApiKeyAuthSchemeOptions, ApiKeyAuthHandler>(ApiKeySchemeConstants.SchemeName, _ => {});
builder.Services.AddAuthorization();
/* End Configure */

var app = builder.Build();

/* Middleware */
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();

app.UseLibraryEndPoints();


// Db init here
var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeAsync();

app.Run();
