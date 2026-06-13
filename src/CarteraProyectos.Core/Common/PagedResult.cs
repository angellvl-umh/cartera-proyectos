namespace CarteraProyectos.Core.Common;

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);
