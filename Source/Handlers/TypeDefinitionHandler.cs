using MediatR;

namespace LanguageServer.Handlers;

sealed class TypeDefinitionHandler : ITypeDefinitionHandler
{
    Task<LocationOrLocationLinks?> IRequestHandler<TypeDefinitionParams, LocationOrLocationLinks?>.Handle(TypeDefinitionParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] TypeDefinition ({request.TextDocument}:{request.Position.Line}:{request.Position.Character})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return OmniSharpService.Instance.Documents.Get(request.TextDocument)?.GotoTypeDefinition(request);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public TypeDefinitionRegistrationOptions GetRegistrationOptions(TypeDefinitionCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
