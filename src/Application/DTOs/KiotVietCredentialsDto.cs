namespace KiotVietTool.Application.DTOs;

public sealed record KiotVietCredentialsDto(string Retailer, string ClientId, string ClientSecret)
{
    public override string ToString() => $"KiotVietCredentialsDto {{ Retailer = {Retailer}, ClientId = {ClientId} }}";
}
