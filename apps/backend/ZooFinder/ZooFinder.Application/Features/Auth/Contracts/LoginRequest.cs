namespace ZooFinder.Application.Features.Auth.Contracts;

public sealed record LoginRequest(
    string Login,
    string Password);
