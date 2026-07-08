namespace ChinookOData.Models;

// The Chinook schema uses snake_case tables/columns
// The column mapping lives in ChinookContext.OnModelCreating

// These entities are consumed by both EF and OData 
public class Artist
{
    public int ArtistId { get; set; }
    public string? Name { get; set; }

    public List<Album> Albums { get; set; } = [];
}

public class Album
{
    public int AlbumId { get; set; }
    public string Title { get; set; } = "";
    public int ArtistId { get; set; }

    public Artist? Artist { get; set; }
    public List<Track> Tracks { get; set; } = [];
}

public class Track
{
    public int TrackId { get; set; }
    public string Name { get; set; } = "";
    public int? AlbumId { get; set; }
    public int MediaTypeId { get; set; }
    public int? GenreId { get; set; }
    public string? Composer { get; set; }
    public int Milliseconds { get; set; }
    public int? Bytes { get; set; }
    public decimal UnitPrice { get; set; }

    public Album? Album { get; set; }
    public Genre? Genre { get; set; }
    public MediaType? MediaType { get; set; }
}

public class Genre
{
    public int GenreId { get; set; }
    public string? Name { get; set; }

    public List<Track> Tracks { get; set; } = [];
}

public class MediaType
{
    public int MediaTypeId { get; set; }
    public string? Name { get; set; }
}

public class Customer
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Company { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string Email { get; set; } = "";

    public List<Invoice> Invoices { get; set; } = [];
}

public class Invoice
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingCountry { get; set; }
    public decimal Total { get; set; }

    public Customer? Customer { get; set; }
    public List<InvoiceLine> Lines { get; set; } = [];
}

public class InvoiceLine
{
    public int InvoiceLineId { get; set; }
    public int InvoiceId { get; set; }
    public int TrackId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public Invoice? Invoice { get; set; }
    public Track? Track { get; set; }
}
