using ChinookOData.Models;
using Microsoft.EntityFrameworkCore;

namespace ChinookOData.Data;

public class ChinookContext(DbContextOptions<ChinookContext> options) : DbContext(options)
{
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MediaType> MediaTypes => Set<MediaType>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Artist>(e =>
        {
            e.ToTable("artist");
            e.HasKey(x => x.ArtistId);
            e.Property(x => x.ArtistId).HasColumnName("artist_id");
            e.Property(x => x.Name).HasColumnName("name");
        });

        b.Entity<Album>(e =>
        {
            e.ToTable("album");
            e.HasKey(x => x.AlbumId);
            e.Property(x => x.AlbumId).HasColumnName("album_id");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.ArtistId).HasColumnName("artist_id");
            e.HasOne(x => x.Artist).WithMany(x => x.Albums).HasForeignKey(x => x.ArtistId);
        });

        b.Entity<Track>(e =>
        {
            e.ToTable("track");
            e.HasKey(x => x.TrackId);
            e.Property(x => x.TrackId).HasColumnName("track_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.AlbumId).HasColumnName("album_id");
            e.Property(x => x.MediaTypeId).HasColumnName("media_type_id");
            e.Property(x => x.GenreId).HasColumnName("genre_id");
            e.Property(x => x.Composer).HasColumnName("composer");
            e.Property(x => x.Milliseconds).HasColumnName("milliseconds");
            e.Property(x => x.Bytes).HasColumnName("bytes");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price");
            e.HasOne(x => x.Album).WithMany(x => x.Tracks).HasForeignKey(x => x.AlbumId);
            e.HasOne(x => x.Genre).WithMany(x => x.Tracks).HasForeignKey(x => x.GenreId);
            e.HasOne(x => x.MediaType).WithMany().HasForeignKey(x => x.MediaTypeId);
        });

        b.Entity<Genre>(e =>
        {
            e.ToTable("genre");
            e.HasKey(x => x.GenreId);
            e.Property(x => x.GenreId).HasColumnName("genre_id");
            e.Property(x => x.Name).HasColumnName("name");
        });

        b.Entity<MediaType>(e =>
        {
            e.ToTable("media_type");
            e.HasKey(x => x.MediaTypeId);
            e.Property(x => x.MediaTypeId).HasColumnName("media_type_id");
            e.Property(x => x.Name).HasColumnName("name");
        });

        b.Entity<Customer>(e =>
        {
            e.ToTable("customer");
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.FirstName).HasColumnName("first_name");
            e.Property(x => x.LastName).HasColumnName("last_name");
            e.Property(x => x.Company).HasColumnName("company");
            e.Property(x => x.City).HasColumnName("city");
            e.Property(x => x.Country).HasColumnName("country");
            e.Property(x => x.Email).HasColumnName("email");
        });

        b.Entity<Invoice>(e =>
        {
            e.ToTable("invoice");
            e.HasKey(x => x.InvoiceId);
            e.Property(x => x.InvoiceId).HasColumnName("invoice_id");
            e.Property(x => x.CustomerId).HasColumnName("customer_id");
            e.Property(x => x.InvoiceDate).HasColumnName("invoice_date");
            e.Property(x => x.BillingCity).HasColumnName("billing_city");
            e.Property(x => x.BillingCountry).HasColumnName("billing_country");
            e.Property(x => x.Total).HasColumnName("total");
            e.HasOne(x => x.Customer).WithMany(x => x.Invoices).HasForeignKey(x => x.CustomerId);
        });

        b.Entity<InvoiceLine>(e =>
        {
            e.ToTable("invoice_line");
            e.HasKey(x => x.InvoiceLineId);
            e.Property(x => x.InvoiceLineId).HasColumnName("invoice_line_id");
            e.Property(x => x.InvoiceId).HasColumnName("invoice_id");
            e.Property(x => x.TrackId).HasColumnName("track_id");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.HasOne(x => x.Invoice).WithMany(x => x.Lines).HasForeignKey(x => x.InvoiceId);
            e.HasOne(x => x.Track).WithMany().HasForeignKey(x => x.TrackId);
        });
    }
}
