namespace LanguageServer.Handlers;

sealed class CodeLensHandler : ICodeLensHandler
{
    public async Task<CodeLensContainer?> Handle(CodeLensParams request, CancellationToken cancellationToken)
    {
        Logger.Debug($"[Handler] CodeLens ({request.TextDocument})");

        if (OmniSharpService.Instance?.Server == null) return null;
        if (!OmniSharpService.Instance.Documents.TryGet(request.TextDocument, out DocumentBase? document)) return null;

        try
        {
            return new CodeLensContainer(await document.CodeLens(request, cancellationToken).ConfigureAwait(false) ?? Enumerable.Empty<CodeLens>());
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    }

    public CodeLensRegistrationOptions GetRegistrationOptions(CodeLensCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
        ResolveProvider = false,
    };
}
