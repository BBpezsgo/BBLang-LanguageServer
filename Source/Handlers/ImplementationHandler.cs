using MediatR;

namespace LanguageServer.Handlers;

sealed class ImplementationHandler : IImplementationHandler
{
    Task<LocationOrLocationLinks?> IRequestHandler<ImplementationParams, LocationOrLocationLinks?>.Handle(ImplementationParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] Implementation ({request.TextDocument}:{request.Position.ToStringMin()})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return OmniSharpService.Instance.Documents.Get(request.TextDocument)?.GotoImplementation(request);
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public ImplementationRegistrationOptions GetRegistrationOptions(ImplementationCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
    };
}
