namespace LanguageServer.Handlers;

sealed class TypeDefinitionHandler : ITypeDefinitionHandler
{
    public async Task<LocationOrLocationLinks?> Handle(TypeDefinitionParams request, CancellationToken cancellationToken)
    {
        Logger.Debug($"[Handler] TypeDefinition ({request.TextDocument}:{request.Position.ToStringMin()})");

        if (OmniSharpService.Instance?.Server == null) return null;
        if (!OmniSharpService.Instance.Documents.TryGet(request.TextDocument.Uri, out DocumentBase? document)) return null;

        try
        {
            return await document.GotoTypeDefinition(request, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    }

    public TypeDefinitionRegistrationOptions GetRegistrationOptions(TypeDefinitionCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
