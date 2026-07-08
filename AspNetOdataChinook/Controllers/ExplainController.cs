using ChinookOData.Data;
using ChinookOData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Extensions;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;

namespace ChinookOData.Controllers;

// Given the *same* query string
// (?$filter=…&$expand=…&$apply=…), it rebuilds the exact IQueryable that OData
// would run and reports the SQL EF Core produces via IQueryable.ToQueryString()
// — WITHOUT executing it against the database.
[ApiController]
[Route("explain")]
public class ExplainController(ChinookContext db, IEdmModel model) : ControllerBase // Not "ODataController" here
{
    [HttpGet("{entitySet}")]
    public IActionResult Explain(string entitySet)
    {
        var resolved = Resolve(entitySet);

        if (resolved is null)
            return NotFound(new { error = $"Unknown entity set '{entitySet}'." });

        var (clrType, source) = resolved.Value;

        // Make the model available to the OData query parser, then parse the
        // query options straight off this request's query string.
        Request.ODataFeature().Model = model;
        var context = new ODataQueryContext(model, clrType, path: null);

        // The same as OData makes under the hood, but here - explicitly
        var options = new ODataQueryOptions(context, Request); // "Request" is "?$filter=..."

        IQueryable shaped;
        try
        {
            // apply parsed options to (for example) db.Tracks
            shaped = options.ApplyTo(source, new ODataQuerySettings());
        }
        catch (Exception ex)
        {
            return Ok(new ExplainResult(sql: "-- OData could not apply these options:\n-- " + ex.Message));
        }

        string sql;
        try
        {
            sql = shaped.ToQueryString();
        }
        catch (Exception ex)
        {
            // $expand / $apply can produce shapes EF Core splits into several
            // statements or can't express as one string — say so plainly.
            sql = "-- EF Core could not render this as a single SQL string:\n-- "
                + ex.Message.Split('\n')[0];
        }

        return Ok(new ExplainResult(sql));
    }

    private (Type clrType, IQueryable source)? Resolve(string name) => name switch
    {
        "Artists" => (typeof(Artist), db.Artists),
        "Albums" => (typeof(Album), db.Albums),
        "Tracks" => (typeof(Track), db.Tracks),
        "Genres" => (typeof(Genre), db.Genres),
        "MediaTypes" => (typeof(MediaType), db.MediaTypes),
        "Customers" => (typeof(Customer), db.Customers),
        "Invoices" => (typeof(Invoice), db.Invoices),
        "InvoiceLines" => (typeof(InvoiceLine), db.InvoiceLines),
        _ => null,
    };

    private record ExplainResult(string? sql);
}
