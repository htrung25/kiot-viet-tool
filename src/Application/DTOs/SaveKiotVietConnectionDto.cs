namespace KiotVietTool.Application.DTOs;

public sealed record SaveKiotVietConnectionDto(string Retailer, string ClientId, string? ClientSecret)
{
    public override string ToString() => $"SaveKiotVietConnectionDto {{ Retailer = {Retailer}, ClientId = {ClientId} }}";
}
