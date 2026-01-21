using MediatR;

namespace LanguageServer.Handlers;

sealed class DocumentHighlightHandler : IDocumentHighlightHandler
{
    public Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] DocumentHighlight ({request.TextDocument}:{request.Position.ToStringMin()})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return new DocumentHighlightContainer(OmniSharpService.Instance.Documents.Get(request.TextDocument)?.DocumentHighlight(request) ?? Enumerable.Empty<DocumentHighlight>());
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public DocumentHighlightRegistrationOptions GetRegistrationOptions(DocumentHighlightCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
