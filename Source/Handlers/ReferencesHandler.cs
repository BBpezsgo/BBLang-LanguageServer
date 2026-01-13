using MediatR;

namespace LanguageServer.Handlers;

sealed class ReferencesHandler : IReferencesHandler
{
    Task<LocationContainer?> IRequestHandler<ReferenceParams, LocationContainer?>.Handle(ReferenceParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] References ({request.TextDocument}:{request.Position.Line}:{request.Position.Character}) ({request.Context})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            IEnumerable<Location>? result = OmniSharpService.Instance.Documents.Get(request.TextDocument)?.References(request);
            return result is null ? null : new LocationContainer(result);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public ReferenceRegistrationOptions GetRegistrationOptions(ReferenceCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
