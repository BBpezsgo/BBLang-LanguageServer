namespace LanguageServer.Handlers;

sealed class SignatureHelpHandler : ISignatureHelpHandler
{
    public Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] SignatureHelp ({request.TextDocument}:{request.Position.Line}:{request.Position.Character}) ({request.Context})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return OmniSharpService.Instance.Documents.Get(request.TextDocument)?.SignatureHelp(request);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
        TriggerCharacters = new Container<string>("(", ","),
    };
}
