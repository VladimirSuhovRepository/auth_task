using AuthApp.CompositionRoot;
using AuthApp.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

DbInitializer.EnsureSeedData(builder.Services.BuildServiceProvider());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable middleware to serve generated Swagger as JSON endpoint.
    app.UseSwagger();

    // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
    // specifying the Swagger JSON endpoint.
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        c.RoutePrefix = string.Empty; // Serve the UI at app root (https://localhost:<port>/)
    });
}

app.UseHttpsRedirection();

// Enable CORS using the defined policy BEFORE authentication so preflight and requests succeed.
app.UseCors(DependencyInjection.LocalCorsPolicy);

// Add authentication middleware before authorization.
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
