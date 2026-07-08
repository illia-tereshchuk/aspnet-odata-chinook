using ChinookOData.Models;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace ChinookOData.Odata;

// The EDM (Entity Data Model) describes what the OData service exposes. It is
// shared by two callers: the OData route in Program.cs, and the /explain
// endpoint, which needs the same model to re-parse query options by hand.
public static class EdmModel
{
    public static IEdmModel Get()
    {
        // EDM model differs from EF model
        // EDM is for exposing to HTTP
        var b = new ODataConventionModelBuilder();
        b.EntitySet<Artist>("Artists"); // POCO classes
        b.EntitySet<Album>("Albums");
        b.EntitySet<Track>("Tracks");
        b.EntitySet<Genre>("Genres");
        b.EntitySet<MediaType>("MediaTypes");
        b.EntitySet<Customer>("Customers");
        b.EntitySet<Invoice>("Invoices");
        b.EntitySet<InvoiceLine>("InvoiceLines");
        return b.GetEdmModel();
    }
}
