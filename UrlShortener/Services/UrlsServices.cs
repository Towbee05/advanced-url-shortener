using UrlShortener.Repository;

public interface IUrlServices
{
    Task<string> CreateShortUrlAsync (Guid userId, string longrl, DateTime? expiredAt);
}

public class UrlServices
{
    private readonly IUrlRepository _urlRepo;

    public UrlServices(IUrlRepository urlRepository)
    {
        this._urlRepo = urlRepository;
    }

    public async Task<string> CreateShortUrlAsync (Guid userId, string longrl, DateTime? expiredAt)
    {
        // Generate the short code

    }
}