using AwesomeAssertions;
using KiotVietTool.Application.Abstractions;
using KiotVietTool.Application.Features.Customers;
using KiotVietTool.Domain.Customers;
using NSubstitute;

namespace KiotVietTool.Application.Tests.Customers;

public sealed class CustomerServiceTests
{
    static readonly DateTimeOffset Now = new(2026, 9, 26, 8, 0, 0, TimeSpan.Zero);

    readonly ICustomerRepository _repository = Substitute.For<ICustomerRepository>();
    readonly CustomerService _sut;
    readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public CustomerServiceTests()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        _sut = new CustomerService(_repository, time);
    }

    static SaveCustomerRequest ValidRequest(string code = "KH001") =>
        new(code, "  Nguyễn Văn A ", "0901234567", "a@example.com", null);

    [Fact]
    public async Task Create_valid_request_saves_trimmed_customer()
    {
        var result = await _sut.CreateAsync(ValidRequest(), _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Nguyễn Văn A");
        result.Value.UpdatedAtUtc.Should().Be(Now.UtcDateTime);
        await _repository.Received(1).AddAsync(Arg.Is<Customer>(c => c.Code == "KH001"), _ct);
    }

    [Fact]
    public async Task Create_duplicate_code_fails_without_saving()
    {
        _repository.CodeExistsAsync("KH001", null, _ct).Returns(true);

        var result = await _sut.CreateAsync(ValidRequest(), _ct);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("đã tồn tại");
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, _ct);
    }

    [Theory]
    [InlineData("", "Tên", null, "Mã khách hàng không được để trống.")]
    [InlineData("KH1", "   ", null, "Tên khách hàng không được để trống.")]
    [InlineData("KH1", "Tên", "not-an-email", "Email không hợp lệ.")]
    public async Task Create_invalid_input_returns_domain_error(string code, string name, string? email, string expected)
    {
        var result = await _sut.CreateAsync(new SaveCustomerRequest(code, name, null, email, null), _ct);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(expected);
        await _repository.DidNotReceiveWithAnyArgs().AddAsync(default!, _ct);
    }

    [Fact]
    public async Task Update_changes_fields_and_excludes_self_from_duplicate_check()
    {
        var existing = Customer.Create("KH001", "Cũ", null, null, null, Now.UtcDateTime.AddDays(-1));
        _repository.GetByIdAsync(7, _ct).Returns(existing);

        var result = await _sut.UpdateAsync(7, ValidRequest("KH002"), _ct);

        result.IsSuccess.Should().BeTrue();
        existing.Code.Should().Be("KH002");
        await _repository.Received(1).CodeExistsAsync("KH002", 7, _ct);
        await _repository.Received(1).UpdateAsync(existing, _ct);
    }

    [Fact]
    public async Task Update_missing_customer_fails()
    {
        var result = await _sut.UpdateAsync(99, ValidRequest(), _ct);

        result.IsSuccess.Should().BeFalse();
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, _ct);
    }

    [Fact]
    public async Task Delete_missing_customer_fails()
    {
        _repository.DeleteAsync(99, _ct).Returns(false);

        var result = await _sut.DeleteAsync(99, _ct);

        result.IsSuccess.Should().BeFalse();
    }
}
