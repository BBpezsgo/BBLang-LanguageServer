namespace LanguageServer.Handlers;

sealed class HoverHandler : IHoverHandler
{
    public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        Logger.Debug($"[Handler] Hover ({request.TextDocument}:{request.Position.ToStringMin()})");

        if (OmniSharpService.Instance?.Server == null) return null;
        if (!OmniSharpService.Instance.Documents.TryGet(request.TextDocument, out DocumentBase? document)) return null;

        try
        {
            return await document.Hover(request, cancellationToken).ConfigureAwait(false);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
