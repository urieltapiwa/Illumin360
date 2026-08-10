using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Illumin360.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Xunit;

namespace Illumin360.Students.IntegrationTests;

/// <summary>
/// End-to-end test for student CV upload/download against a real PostgreSQL and MinIO via Testcontainers.
/// Requires a Docker daemon on the test host.
/// </summary>
public sealed class StudentCvUploadTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("illumin360_students")
        .WithUsername("illumin")
        .WithPassword("illumin_dev_pw")
        .Build();

    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage("minio/minio:latest")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());

        Environment.SetEnvironmentVariable("ConnectionStrings__students", _postgres.GetConnectionString() + ";SSL Mode=Disable");
        Environment.SetEnvironmentVariable("Storage__Endpoint", _minio.GetConnectionString());
        Environment.SetEnvironmentVariable("Storage__AccessKey", _minio.GetAccessKey());
        Environment.SetEnvironmentVariable("Storage__SecretKey", _minio.GetSecretKey());

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestAuth();
        });

        _ = _factory.Server;
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__students", null);
        Environment.SetEnvironmentVariable("Storage__Endpoint", null);
        Environment.SetEnvironmentVariable("Storage__AccessKey", null);
        Environment.SetEnvironmentVariable("Storage__SecretKey", null);
        await _factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _minio.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task Upload_without_a_token_returns_401()
    {
        var client = _factory.CreateClient();
        using var content = PdfForm();

        var response = await client.PostAsync("/v1/students/me/cv", content);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_then_metadata_and_download_round_trip_through_MinIO()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.ForRoles(["client.user"]));

        using var content = PdfForm();
        var upload = await client.PostAsync("/v1/students/me/cv", content);
        upload.StatusCode.Should().Be(HttpStatusCode.OK);

        var meta = await client.GetAsync("/v1/students/me/cv");
        meta.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await meta.Content.ReadFromJsonAsync<CvMeta>();
        dto!.FileName.Should().Be("resume.pdf");

        var download = await client.GetAsync("/v1/students/me/cv/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        (await download.Content.ReadAsByteArrayAsync()).Should().NotBeEmpty();
    }

    private static MultipartFormDataContent PdfForm()
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF");
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        return new MultipartFormDataContent { { file, "file", "resume.pdf" } };
    }

    private sealed record CvMeta(string FileName, string ContentType, long Size, DateTimeOffset UploadedAt);
}
