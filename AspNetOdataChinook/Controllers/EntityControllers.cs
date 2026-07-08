using ChinookOData.Data;
using ChinookOData.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace ChinookOData.Controllers;

// Each controller exposes an entity set as IQueryable.
// [EnableQuery] is the whole trick:
// OData translates $filter/$orderby/$select/$expand/$apply/$top/$skip on the
// incoming URL straight into the LINQ query, which EF Core turns into SQL.
// We never write any of that filtering logic by hand.

public class ArtistsController(ChinookContext db) : ODataController
{
    [EnableQuery(MaxExpansionDepth = 4, PageSize = 100)]
    public IQueryable<Artist> Get() => db.Artists;

    [EnableQuery(MaxExpansionDepth = 4)]
    public SingleResult<Artist> Get(int key) =>
        SingleResult.Create(db.Artists.Where(a => a.ArtistId == key));
}

public class AlbumsController(ChinookContext db) : ODataController
{
    [EnableQuery(MaxExpansionDepth = 4, PageSize = 100)]
    public IQueryable<Album> Get() => db.Albums;

    [EnableQuery(MaxExpansionDepth = 4)]
    public SingleResult<Album> Get(int key) =>
        SingleResult.Create(db.Albums.Where(a => a.AlbumId == key));
}

public class TracksController(ChinookContext db) : ODataController
{
    [EnableQuery(MaxExpansionDepth = 4, PageSize = 100)]
    public IQueryable<Track> Get() => db.Tracks;

    [EnableQuery(MaxExpansionDepth = 4)]
    public SingleResult<Track> Get(int key) =>
        SingleResult.Create(db.Tracks.Where(t => t.TrackId == key));
}

public class GenresController(ChinookContext db) : ODataController
{
    [EnableQuery(MaxExpansionDepth = 4, PageSize = 100)]
    public IQueryable<Genre> Get() => db.Genres;
}

public class MediaTypesController(ChinookContext db) : ODataController
{
    [EnableQuery(PageSize = 100)]
    public IQueryable<MediaType> Get() => db.MediaTypes;
}

public class CustomersController(ChinookContext db) : ODataController
{
    [EnableQuery(MaxExpansionDepth = 4, PageSize = 100)]
    public IQueryable<Customer> Get() => db.Customers;
}

public class InvoicesController(ChinookContext db) : ODataController
{
    [EnableQuery(MaxExpansionDepth = 4, PageSize = 100)]
    public IQueryable<Invoice> Get() => db.Invoices;
}

public class InvoiceLinesController(ChinookContext db) : ODataController
{
    [EnableQuery(MaxExpansionDepth = 4, PageSize = 100)]
    public IQueryable<InvoiceLine> Get() => db.InvoiceLines;
}
