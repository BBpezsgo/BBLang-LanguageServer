namespace LanguageServer.Handlers;

sealed class InlineValuesHandler : IInlineValuesHandler
{
    public async Task<Container<InlineValueBase>?> Handle(InlineValueParams request, CancellationToken cancellationToken)
    {
        Logger.Debug($"[Handler] InlineValues ({request.TextDocument}:{request.Range})");

        if (OmniSharpService.Instance?.Server == null) return null;
        if (!OmniSharpService.Instance.Documents.TryGet(request.TextDocument.Uri, out DocumentBase? document)) return null;

        try
        {
            IEnumerable<InlineValueBase>? result = await document.InlineValues(request, cancellationToken).ConfigureAwait(false);
            return result is null ? null : new(result);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    }

    public InlineValueRegistrationOptions GetRegistrationOptions(InlineValueClientCapabilities capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
