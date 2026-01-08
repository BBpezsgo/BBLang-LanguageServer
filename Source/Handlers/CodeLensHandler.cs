using MediatR;

namespace LanguageServer.Handlers;

sealed class CodeLensHandler : ICodeLensHandler
{
    Task<CodeLensContainer?> IRequestHandler<CodeLensParams, CodeLensContainer?>.Handle(CodeLensParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Log($"CodeLensHandler.Handle({request})");

        if (OmniSharpService.Instance?.Server == null) return null;

        try
        {
            return new CodeLensContainer(OmniSharpService.Instance.Documents.Get(request.TextDocument)?.CodeLens(request) ?? Enumerable.Empty<CodeLens>());
        }
        catch (ServiceException error)
        {
            OmniSharpService.Instance?.Server?.Window?.ShowWarning($"BBLang ServiceException: {error.Message}");
            return null;
        }
    });

    public CodeLensRegistrationOptions GetRegistrationOptions(CodeLensCapability capability, ClientCapabilities clientCapabilities) => new()
    {
        DocumentSelector = TextDocumentSelector.ForLanguage(LanguageCore.LanguageConstants.LanguageId),
        ResolveProvider = false,
    };
}
