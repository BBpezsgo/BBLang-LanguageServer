using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace LanguageServer.Handlers;

sealed class DidChangeConfigurationHandler : IDidChangeConfigurationHandler
{
    public Task<Unit> Handle(DidChangeConfigurationParams request, CancellationToken cancellationToken) => Task.Run(() =>
    {
        Logger.Debug($"[Handler] ConfigurationChanged");

        OmniSharpService.Instance?.OnConfigChanged(request);

        return Unit.Value;
    });

    public void SetCapability(DidChangeConfigurationCapability capability, ClientCapabilities clientCapabilities)
    {

    }
}
