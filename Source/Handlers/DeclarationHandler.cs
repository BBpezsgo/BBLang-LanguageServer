using MediatR;

namespace LanguageServer.Handlers;

sealed class DeclarationHandler : IDeclarationHandler
{
    Task<LocationOrLocationLinks?> IRequestHandler<DeclarationParams, LocationOrLocationLinks?>.Handle(DeclarationParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] Declaration ({request.TextDocument}:{request.Position.Line}:{request.Position.Character})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return OmniSharpService.Instance.Documents.Get(request.TextDocument)?.GotoDeclaration(request);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public DeclarationRegistrationOptions GetRegistrationOptions(DeclarationCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
