using MediatR;

namespace LanguageServer.Handlers;

sealed class CompletionHandler : ICompletionHandler
{
    Task<CompletionList> IRequestHandler<CompletionParams, CompletionList>.Handle(CompletionParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] Completion ({request.TextDocument}:{request.Position.Line}:{request.Position.Character}) ({request.Context})");

        if (OmniSharpService.Instance?.Server == null) return new CompletionList();

        try
        {
            return new CompletionList(OmniSharpService.Instance.Documents.Get(request.TextDocument)?.Completion(request) ?? Enumerable.Empty<CompletionItem>());
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return new CompletionList();
        }
    });

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        capability.ContextSupport = false;
        return new CompletionRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
            ResolveProvider = false,
            TriggerCharacters = new string[] { "." },
        };
    }
}
