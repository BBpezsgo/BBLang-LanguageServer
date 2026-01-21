using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using LanguageServer.Handlers;

namespace LanguageServer;

sealed class OmniSharpService
{
    public static OmniSharpService? Instance { get; private set; }

    public ILanguageServer? Server { get; private set; }
    public IServiceProvider? ServiceProvider { get; private set; }
    public Documents Documents { get; }
    public JToken? Config { get; private set; }

    public OmniSharpService()
    {
        Instance = this;
        Documents = new Documents();
    }

    public async Task CreateAsync()
    {
        Server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(Configure).ConfigureAwait(false);

        Logger.Debug("Created");

        await Server.WaitForExit.ConfigureAwait(false);
    }

    void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ConfigurationItem()
        {
            Section = "terminal",
        });
    }

    void Configure(LanguageServerOptions options)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput());
#pragma warning restore CA2000 // Dispose objects before losing scope

        options
            .ConfigureLogging(
               x => x
                   .AddLanguageProtocolLogging()
                   .SetMinimumLevel(LogLevel.Information)
           );

        options
           .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
           .WithServices(ConfigureServices);

        options
           .WithHandler<CodeLensHandler>()
           .WithHandler<CompletionHandler>()
           //.WithHandler<DeclarationHandler>()
           .WithHandler<DefinitionHandler>()
           .WithHandler<DidChangeConfigurationHandler>()
           //.WithHandler<DocumentHighlightHandler>()
           .WithHandler<DocumentSymbolHandler>()
           .WithHandler<HoverHandler>()
           //.WithHandler<ImplementationHandler>()
           .WithHandler<ReferencesHandler>()
           .WithHandler<SemanticTokensHandler>()
           .WithHandler<SignatureHelpHandler>()
           .WithHandler<TextDocumentSyncHandler>()
           .WithHandler<TypeDefinitionHandler>()
        ;

        options.OnInitialize((server, request, cancellationToken) =>
        {
            if (request.Capabilities?.TextDocument != null)
            {
                request.Capabilities.TextDocument.SemanticTokens = new SemanticTokensCapability()
                {
                    TokenTypes = new Container<SemanticTokenType>(SemanticTokenType.Defaults),
                    TokenModifiers = new Container<SemanticTokenModifier>(SemanticTokenModifier.Defaults),
                    MultilineTokenSupport = false,
                    OverlappingTokenSupport = false,
                    Formats = new Container<SemanticTokenFormat>(SemanticTokenFormat.Defaults),
                    Requests = new SemanticTokensCapabilityRequests()
                    {
                        Full = new Supports<SemanticTokensCapabilityRequestFull?>(true),
                        Range = new Supports<SemanticTokensCapabilityRequestRange?>(false),
                    },
                    ServerCancelSupport = false,
                };
            }
            ServiceProvider = (server as OmniSharp.Extensions.LanguageServer.Server.LanguageServer)?.Services;
            return Task.CompletedTask;
        });

        options.OnInitialized((server, e, result, cancellationToken) =>
        {
            Logger.Debug($"Initialized");
            return Task.CompletedTask;
        });

        options.OnStarted((server, cancellationToken) =>
        {
            Logger.Debug($"Started");
            return Task.CompletedTask;
        });
    }

    public void OnConfigChanged(DidChangeConfigurationParams e)
    {
        Config = e.Settings;
    }
}
