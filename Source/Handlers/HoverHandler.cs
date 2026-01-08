using MediatR;

namespace LanguageServer.Handlers;

sealed class HoverHandler : IHoverHandler
{
    Task<Hover?> IRequestHandler<HoverParams, Hover?>.Handle(HoverParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Log($"HoverHandler.Handle({request})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return OmniSharpService.Instance.Documents.Get(request.TextDocument)?.Hover(request);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
