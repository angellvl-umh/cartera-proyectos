using CarteraProyectos.Core.Domain;
using CarteraProyectos.Core.Interfaces;
using FluentValidation;
using MediatR;

namespace CarteraProyectos.Core.Features.Chat;

public record CreateConversationCommand(int PersonId, string Title) : IRequest<int>;

public class CreateConversationValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PersonId).GreaterThan(0);
    }
}

public sealed class CreateConversationHandler(IAppDbContext db)
    : IRequestHandler<CreateConversationCommand, int>
{
    public async Task<int> Handle(CreateConversationCommand request, CancellationToken ct)
    {
        var conversation = Conversation.Create(request.PersonId, request.Title);
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(ct);
        return conversation.Id;
    }
}
