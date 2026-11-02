using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace MealManager.IntegrationTests;

/// <summary>P2-BE-3/4: utility type CRUD (manager-scoped) + utility payments linked to types.</summary>
public sealed class UtilitiesFlowTests(BmmApiFactory factory) : IClassFixture<BmmApiFactory>
{
    private static async Task<JsonElement> Body(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<Guid> CreateTypeAsync(DomainTestMess mess, string name)
    {
        var response = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/utility-types")
        {
            Content = JsonContent.Create(new { name, description = (string?)null }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await Body(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task UtilityType_Crud_With_Duplicate_Guard()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "21");
        var id = await CreateTypeAsync(mess, "Electricity");

        // Duplicate name (case-insensitive) → 409
        var duplicate = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/utility-types")
        {
            Content = JsonContent.Create(new { name = "electricity" }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Member cannot create or update types
        var memberCreate = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/utility-types")
        {
            Content = JsonContent.Create(new { name = "Gas" }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        memberCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Update: rename + deactivate
        var update = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/utility-types/{id}")
        {
            Content = JsonContent.Create(new { name = "Electricity Bill", description = "Monthly", isActive = false }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await Body(update);
        updated.GetProperty("name").GetString().Should().Be("Electricity Bill");
        updated.GetProperty("isActive").GetBoolean().Should().BeFalse();

        // Renaming to another type's name → 409
        await CreateTypeAsync(mess, "Wi-Fi");
        var clash = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Put, $"/api/utility-types/{id}")
        {
            Content = JsonContent.Create(new { name = "Wi-Fi", isActive = true }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        clash.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // No payments reference it → delete works
        var delete = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/utility-types/{id}")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/utility-types")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var page = await Body(list);
        page.GetProperty("totalCount").GetInt32().Should().Be(1); // only Wi-Fi remains
        page.GetProperty("items").EnumerateArray().Should().ContainSingle(t => t.GetProperty("name").GetString() == "Wi-Fi");
    }

    [Fact]
    public async Task UtilityType_Delete_With_Payments_Returns_409()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "22");
        var id = await CreateTypeAsync(mess, "Cook Salary");

        await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new { utilityTypeId = id, amount = 5000m, paymentDate = new DateOnly(2026, 11, 1) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });

        var delete = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/utility-types/{id}")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        delete.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UtilityPayment_Create_And_List()
    {
        var mess = await DomainTestSetup.CreateMessWithMemberAsync(factory, "23");
        var electricity = await CreateTypeAsync(mess, "Electricity");
        var wifi = await CreateTypeAsync(mess, "Wi-Fi");

        var create = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new
            {
                utilityTypeId = electricity,
                amount = 1200.50m,
                paymentDate = new DateOnly(2026, 11, 5),
                note = "October bill",
            }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await Body(create);
        payment.GetProperty("utilityTypeName").GetString().Should().Be("Electricity");
        payment.GetProperty("amount").GetDecimal().Should().Be(1200.50m);

        await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new
            {
                utilityTypeId = wifi,
                amount = 900m,
                paymentDate = new DateOnly(2026, 11, 6),
            }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });

        // Member cannot record utility payments
        var memberCreate = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new { utilityTypeId = wifi, amount = 1m, paymentDate = new DateOnly(2026, 11, 6) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        memberCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Cross-mess / unknown type → 404
        var unknownType = await mess.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new { utilityTypeId = Guid.NewGuid(), amount = 1m, paymentDate = new DateOnly(2026, 11, 6) }),
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        unknownType.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // List: type filter + date range + default date desc
        var list = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            "/api/expenses/utility?from=2026-11-01&to=2026-11-30")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        var page = await Body(list);
        page.GetProperty("totalCount").GetInt32().Should().Be(2);
        page.GetProperty("items").EnumerateArray()
            .Select(p => p.GetProperty("paymentDate").GetString())
            .Should().ContainInOrder("2026-11-06", "2026-11-05");

        var electricityOnly = await mess.Member.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"/api/expenses/utility?utilityTypeId={electricity}")
        {
            Headers = { { "X-Mess-Id", mess.MessId.ToString() } },
        });
        (await Body(electricityOnly)).GetProperty("totalCount").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Utility_Types_And_Payments_Are_Tenant_Isolated()
    {
        var messA = await DomainTestSetup.CreateMessWithMemberAsync(factory, "24");
        var messB = await DomainTestSetup.CreateMessWithMemberAsync(factory, "25");

        var typeA = await CreateTypeAsync(messA, "Electricity");
        var typeB = await CreateTypeAsync(messB, "Electricity"); // same name, different mess — allowed

        await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new { utilityTypeId = typeA, amount = 100m, paymentDate = new DateOnly(2026, 11, 1) }),
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        await messB.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new { utilityTypeId = typeB, amount = 200m, paymentDate = new DateOnly(2026, 11, 1) }),
            Headers = { { "X-Mess-Id", messB.MessId.ToString() } },
        });

        var listA = await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/expenses/utility")
        {
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        var pageA = await Body(listA);
        pageA.GetProperty("totalCount").GetInt32().Should().Be(1);
        pageA.GetProperty("items").EnumerateArray()
            .Should().OnlyContain(p => p.GetProperty("amount").GetDecimal() == 100m);

        // Mess A's manager cannot use Mess B's utility type id (404 masks cross-tenant existence)
        var crossType = await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/expenses/utility")
        {
            Content = JsonContent.Create(new { utilityTypeId = typeB, amount = 1m, paymentDate = new DateOnly(2026, 11, 1) }),
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        crossType.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var typesA = await messA.Manager.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/utility-types")
        {
            Headers = { { "X-Mess-Id", messA.MessId.ToString() } },
        });
        (await Body(typesA)).GetProperty("totalCount").GetInt32().Should().Be(1);
    }
}
