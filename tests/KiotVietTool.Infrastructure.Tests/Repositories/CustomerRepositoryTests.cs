using AwesomeAssertions;
using KiotVietTool.Domain.Customers;
using KiotVietTool.Infrastructure.Repositories;

namespace KiotVietTool.Infrastructure.Tests.Repositories;

public sealed class CustomerRepositoryTests : IAsyncLifetime
{
    static readonly DateTime Now = new(2026, 9, 26, 8, 0, 0, DateTimeKind.Utc);

    readonly SqliteTestDatabase _db = new();
    readonly CustomerRepository _sut;
    readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public CustomerRepositoryTests() => _sut = new CustomerRepository(_db);

    public ValueTask InitializeAsync() => _db.InitializeAsync();
    public ValueTask DisposeAsync() => _db.DisposeAsync();

    async Task<Customer> SeedAsync(string code, string name, string? phone = null)
    {
        var customer = Customer.Create(code, name, phone, null, null, Now);
        await _sut.AddAsync(customer, _ct);
        return customer;
    }

    [Fact]
    public async Task Search_matches_code_name_or_phone_case_insensitive_sorted_by_name()
    {
        await SeedAsync("KH001", "Bình");
        await SeedAsync("KH002", "An", "0901111111");
        await SeedAsync("NCC01", "Công ty X");

        (await _sut.SearchAsync("kh", _ct)).Select(c => c.Code).Should().Equal("KH002", "KH001");
        (await _sut.SearchAsync("0901", _ct)).Should().ContainSingle().Which.Code.Should().Be("KH002");
        (await _sut.SearchAsync(null, _ct)).Should().HaveCount(3);
    }

    [Fact]
    public async Task CodeExists_is_case_insensitive_and_can_exclude_self()
    {
        var customer = await SeedAsync("KH001", "An");

        (await _sut.CodeExistsAsync("kh001", null, _ct)).Should().BeTrue();
        (await _sut.CodeExistsAsync("KH001", customer.Id, _ct)).Should().BeFalse();
        (await _sut.CodeExistsAsync("KH999", null, _ct)).Should().BeFalse();
    }

    [Fact]
    public async Task Update_persists_changes()
    {
        var created = await SeedAsync("KH001", "An");
        var loaded = (await _sut.GetByIdAsync(created.Id, _ct))!;

        loaded.Update("KH001", "An Mới", "0909", "an@example.com", "Hà Nội", Now.AddHours(1));
        await _sut.UpdateAsync(loaded, _ct);

        var reloaded = await _sut.GetByIdAsync(created.Id, _ct);
        reloaded!.Name.Should().Be("An Mới");
        reloaded.Address.Should().Be("Hà Nội");
        reloaded.CreatedAtUtc.Should().Be(Now);
    }

    [Fact]
    public async Task Delete_removes_row_and_reports_missing()
    {
        var customer = await SeedAsync("KH001", "An");

        (await _sut.DeleteAsync(customer.Id, _ct)).Should().BeTrue();
        (await _sut.DeleteAsync(customer.Id, _ct)).Should().BeFalse();
        (await _sut.GetByIdAsync(customer.Id, _ct)).Should().BeNull();
    }
}
