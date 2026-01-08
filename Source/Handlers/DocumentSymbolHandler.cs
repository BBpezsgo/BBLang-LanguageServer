using MediatR;

namespace LanguageServer.Handlers;

sealed class DocumentSymbolHandler : IDocumentSymbolHandler
{
    Task<SymbolInformationOrDocumentSymbolContainer?> IRequestHandler<DocumentSymbolParams, SymbolInformationOrDocumentSymbolContainer?>.Handle(DocumentSymbolParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Log($"DocumentSymbolHandler.Handle({request})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return new SymbolInformationOrDocumentSymbolContainer(OmniSharpService.Instance.Documents.Get(request.TextDocument)?.Symbols(request) ?? Enumerable.Empty<SymbolInformationOrDocumentSymbol>());
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public DocumentSymbolRegistrationOptions GetRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
    {
        capability.HierarchicalDocumentSymbolSupport = true;
        return new DocumentSymbolRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
        };
    }
}
