namespace ZooFinder.Application.Features.Auth.Contracts;

public sealed record RegisterRequest(
    string Login,
    string Password);
