namespace LanguageServer.Handlers;

sealed class DeclarationHandler : IDeclarationHandler
{
    public async Task<LocationOrLocationLinks?> Handle(DeclarationParams request, CancellationToken cancellationToken)
    {
        Logger.Debug($"[Handler] Declaration ({request.TextDocument}:{request.Position.ToStringMin()})");

        if (OmniSharpService.Instance?.Server == null) return null;
        if (!OmniSharpService.Instance.Documents.TryGet(request.TextDocument, out DocumentBase? document)) return null;

        try
        {
            return await document.GotoDeclaration(request, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    }

    public DeclarationRegistrationOptions GetRegistrationOptions(DeclarationCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
