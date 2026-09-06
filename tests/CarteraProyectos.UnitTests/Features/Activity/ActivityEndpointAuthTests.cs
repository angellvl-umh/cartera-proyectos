using System.Net;
using CarteraProyectos.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Activity;

public class ActivityEndpointAuthTests
{
    /// <summary>
    /// Host de prueba mínimo: monta un único endpoint que replica el contrato de autorización
    /// de <c>GET /api/activity</c> (<c>RequireAuthorization()</c> sin rol adicional, igual que sus
    /// vecinos en <see cref="ReportEndpoints"/>) con autenticación JWT, sin base de datos ni MediatR
    /// reales. Una petición sin token debe cortarse en la autorización (401) antes de tocar el handler.
    /// El registro real del endpoint con esa misma llamada se verifica en
    /// <see cref="GetActivity_IsRegisteredWithAuthorization"/>.
    /// </summary>
    private static async Task<TestServer> CreateServerAsync()
    {
        var builder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.RequireHttpsMetadata = false;
                            options.TokenValidationParameters.ValidateIssuer = false;
                            options.TokenValidationParameters.ValidateAudience = false;
                            options.TokenValidationParameters.ValidateIssuerSigningKey = false;
                            options.TokenValidationParameters.SignatureValidator = (token, _) =>
                                new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token);
                        });
                    services.AddAuthorization();
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints =>
                        endpoints.MapGet("/api/activity", () => Results.Ok())
                                 .RequireAuthorization());
                });
            });

        var host = await builder.StartAsync();
        return host.GetTestServer();
    }

    [Fact]
    public async Task GetActivity_WithoutAuthentication_Returns401()
    {
        using var server = await CreateServerAsync();
        using var client = server.CreateClient();

        var response = await client.GetAsync("/api/activity");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public void GetActivity_IsRegisteredWithAuthorization()
    {
        // Verifica sobre el registro REAL (ReportEndpoints.MapReportEndpoints) que /api/activity
        // existe y lleva metadatos de autorización.
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        // Registrar los servicios que inyectan los endpoints de informes para que
        // RequestDelegateFactory los trate como parámetros de servicio (no de body)
        // al materializar los endpoints.
        builder.Services.AddScoped(_ => Substitute.For<CarteraProyectos.Core.Interfaces.IAppDbContext>());
        builder.Services.AddScoped(_ => Substitute.For<MediatR.IMediator>());
        var app = builder.Build();
        app.MapReportEndpoints();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints);
        var activity = endpoints
            .OfType<RouteEndpoint>()
            .SingleOrDefault(e => e.RoutePattern.RawText == "/api/activity");

        activity.ShouldNotBeNull();
        activity!.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
            .ShouldNotBeNull();
    }
}
