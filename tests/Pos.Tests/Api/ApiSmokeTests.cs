using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pos.Tests.Api;

public sealed class ApiSmokeTests(PosApiFactory factory) : IClassFixture<PosApiFactory>
{
    [Fact]
    public async Task Health_is_anonymous()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_require_a_token()
    {
        var response = await factory.CreateClient().GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bad_credentials_return_problem_details()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Salesman_can_bill_but_cannot_manage_products()
    {
        var client = await LoginAsync("salesman", "Sales@123");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/products", new { name = "X", salePrice = 10 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/reports/sales?period=Monthly")).StatusCode);

        // No register yet => 204, then open one.
        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/registers/current")).StatusCode);
        var open = await client.PostAsJsonAsync("/api/registers/open", new { openingCash = 5000 });
        Assert.Equal(HttpStatusCode.Created, open.StatusCode);

        var product = await client.GetFromJsonAsync<JsonObject>("/api/products/barcode/2000000000015");
        var sale = await client.PostAsJsonAsync("/api/sales", new
        {
            items = new[] { new { productId = (int)product!["id"]!, quantity = 2 } },
            paymentMethod = "Cash",
            amountPaid = 1000,
            billDiscount = 0
        });
        Assert.Equal(HttpStatusCode.Created, sale.StatusCode);

        var saleJson = await sale.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal("Cash", (string?)saleJson!["paymentMethod"]);
        Assert.Equal(640m, (decimal)saleJson["total"]!);

        var receipt = await client.GetFromJsonAsync<JsonObject>($"/api/sales/{(int)saleJson["id"]!}/receipt");
        Assert.Contains((string)saleJson["invoiceNumber"]!, (string)receipt!["qrContent"]!);
        Assert.False(string.IsNullOrEmpty((string?)receipt["store"]!["urduInstructions"]));
    }

    [Fact]
    public async Task Admin_gets_validation_errors_keyed_by_camel_case_field()
    {
        var client = await LoginAsync("admin", "Admin@123");

        var response = await client.PostAsJsonAsync("/api/products", new { name = "", salePrice = 0, costPrice = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
        var errors = problem!["errors"]!.AsObject();
        Assert.True(errors.ContainsKey("name"));
        Assert.True(errors.ContainsKey("salePrice"));
    }

    [Fact]
    public async Task Admin_can_read_report_and_filter_lists_by_date()
    {
        var client = await LoginAsync("admin", "Admin@123");

        var report = await client.GetFromJsonAsync<JsonObject>("/api/reports/sales?period=Yearly&date=2026-03-15");
        Assert.Equal("2026-01-01", (string?)report!["from"]);
        Assert.Equal(12, report["buckets"]!.AsArray().Count);

        var sales = await client.GetAsync("/api/sales?from=2026-01-01&to=2026-12-31&page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, sales.StatusCode);
        var discounted = await client.GetFromJsonAsync<JsonObject>("/api/products?onlyDiscounted=true&pageSize=50");
        Assert.All(discounted!["items"]!.AsArray(), p => Assert.NotNull(p!["discountedPrice"]));
    }

    [Fact]
    public async Task Admin_can_upload_replace_and_remove_a_barcode_image()
    {
        var client = await LoginAsync("admin", "Admin@123");
        var product = await client.GetFromJsonAsync<JsonObject>("/api/products/barcode/2000000000015");
        var url = $"/api/products/{(int)product!["id"]!}/barcode-image";
        Assert.False((bool)product["hasBarcodeImage"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);

        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
        var upload = await client.PutAsync(url, ImageForm(png, "logo.png"));
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var updated = await upload.Content.ReadFromJsonAsync<JsonObject>();
        Assert.True((bool)updated!["hasBarcodeImage"]!);
        Assert.NotNull(updated["barcodeImageUpdatedAt"]);

        var image = await client.GetAsync(url);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(png, await image.Content.ReadAsByteArrayAsync());

        var notAnImage = await client.PutAsync(url, ImageForm("hello"u8.ToArray(), "notes.png"));
        Assert.Equal(HttpStatusCode.BadRequest, notAnImage.StatusCode);

        var salesman = await LoginAsync("salesman", "Sales@123");
        Assert.Equal(HttpStatusCode.OK, (await salesman.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await salesman.DeleteAsync(url)).StatusCode);

        var removed = await client.DeleteAsync(url);
        Assert.False((bool)(await removed.Content.ReadFromJsonAsync<JsonObject>())!["hasBarcodeImage"]!);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(url)).StatusCode);
    }

    private static MultipartFormDataContent ImageForm(byte[] content, string fileName) =>
        new() { { new ByteArrayContent(content), "file", fileName } };

    private async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }
}
