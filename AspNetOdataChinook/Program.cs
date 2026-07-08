using ChinookOData.Data;
using ChinookOData.Odata;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Chinook")
    ?? "Host=localhost;Port=5432;Database=chinook;Username=chinook;Password=chinook";

builder.Services.AddDbContext<ChinookContext>(o => o.UseNpgsql(connectionString));

var edmModel = EdmModel.Get();
builder.Services.AddSingleton(edmModel);   // shared with the /explain endpoint

builder.Services.AddControllers()
    .AddOData(options => options
        .Select().Filter().OrderBy().Expand().Count().SetMaxTop(200) // What is allowed
        .AddRouteComponents("odata", edmModel));

var app = builder.Build();

app.UseDefaultFiles();   // serve wwwroot/index.html at "/"
app.UseStaticFiles();
app.MapControllers();

app.Run();
