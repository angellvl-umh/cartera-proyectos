using CarteraProyectos.Api.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace CarteraProyectos.UnitTests.Features.Activity;

public class ActivityEndpointAuthTests
{
    [Fact]
    public void GetActivity_IsRegisteredWithAuthorization()
    {
        // Verifica sobre el registro REAL (ReportEndpoints.MapReportEndpoints) que /api/activity
        // existe y lleva metadatos de autorización, sin levantar un servidor HTTP.
        var builder = WebApplication.CreateBuilder();
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
