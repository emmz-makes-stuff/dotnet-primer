using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SessionShop.Tests;

/// <summary>
/// Two copies of the shop in the test process, as two servers behind a load balancer: one shared
/// distributed cache, one shared keys folder and one application name, given to both through
/// configuration. Each test makes its own.
/// </summary>
/// <remarks>
/// Each copy gets a content root of its own, as two servers have. Data Protection keeps apps apart
/// by content root unless they're given one application name, so without TODO 3 neither copy can
/// read the other's session cookie. (Copies of one container image share a path, /app, but each
/// makes a key ring of its own, with the same result. The Session and state page explains both.)
/// </remarks>
public sealed class ShopCluster : IDisposable
{
    public const string ApplicationName = "session-shop-tests";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "session-shop-" + Guid.NewGuid().ToString("N"));

    public ShopCluster()
    {
        KeysPath = Directory.CreateDirectory(Path.Combine(_root, "keys")).FullName;
        A = new ShopInstance(this, "a");
        B = new ShopInstance(this, "b");
    }

    public RecordingCache Cache { get; } = new();

    public string KeysPath { get; }

    public ShopInstance A { get; }

    public ShopInstance B { get; }

    internal string ContentRoot(string name) => Directory.CreateDirectory(Path.Combine(_root, "server-" + name)).FullName;

    public void Dispose()
    {
        A.Dispose();
        B.Dispose();
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // A key file still open on a slow machine; the folder is in the temp directory anyway.
        }
    }
}

/// <summary>One copy of the shop.</summary>
public sealed class ShopInstance(ShopCluster cluster, string name) : WebApplicationFactory<Program>
{
    private HttpClient? _client;

    public string Name => name;

    /// <summary>A client that doesn't keep cookies: a <see cref="Shopper"/> carries them.</summary>
    public HttpClient Client => _client ??= CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(cluster.ContentRoot(name));
        builder.UseSetting("DataProtection:KeysPath", cluster.KeysPath);
        builder.UseSetting("DataProtection:ApplicationName", ShopCluster.ApplicationName);
        builder.ConfigureTestServices(services => services.AddSingleton<IDistributedCache>(cluster.Cache));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>An in-memory distributed cache that remembers every value written to it.</summary>
public sealed class RecordingCache : IDistributedCache
{
    private readonly MemoryDistributedCache _inner = new(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly List<string> _writes = [];

    /// <summary>Every value written, as text. Session's own format keeps each value's bytes as they are.</summary>
    public IReadOnlyList<string> Writes
    {
        get
        {
            lock (_writes)
            {
                return [.. _writes];
            }
        }
    }

    public byte[]? Get(string key) => _inner.Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => _inner.GetAsync(key, token);

    public void Refresh(string key) => _inner.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);

    public void Remove(string key) => _inner.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        Record(value);
        _inner.Set(key, value, options);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Record(value);
        return _inner.SetAsync(key, value, options, token);
    }

    private void Record(byte[] value)
    {
        lock (_writes)
        {
            _writes.Add(Encoding.UTF8.GetString(value));
        }
    }
}
