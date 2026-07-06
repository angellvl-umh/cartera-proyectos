namespace CarteraProyectos.Core.Interfaces;

public enum IdentityCredentialsStatus { Created, AlreadyExists, Unavailable }

public record IdentityCredentialsResult(IdentityCredentialsStatus Status, string? TemporaryPassword);

/// <summary>Gestión de cuentas locales en el identity provider (Keycloak).</summary>
public interface IIdentityProviderService
{
    /// <summary>
    /// Crea un usuario local con contraseña temporal (required action UPDATE_PASSWORD).
    /// AlreadyExists si el username/email ya existe (no es error).
    /// Unavailable si el IdP no responde (no debe tumbar el alta de la Person).
    /// </summary>
    Task<IdentityCredentialsResult> CreateUserWithTemporaryPasswordAsync(
        string name, string email, CancellationToken ct);
}
