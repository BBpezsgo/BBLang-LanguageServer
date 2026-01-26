using LanguageCore.Compiler;
using OmniSharpLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang
{
    public override async Task<IEnumerable<SymbolInformationOrDocumentSymbol>?> Symbols(DocumentSymbolParams e, CancellationToken cancellationToken)
    {
        await AwaitForCompilation(Version ?? 0, cancellationToken).ConfigureAwait(false);

        List<SymbolInformationOrDocumentSymbol> result = new();

        foreach (CompiledFunctionDefinition function in CompilerResult.FunctionDefinitions)
        {
            DocumentUri? uri = function.File is null ? null : (DocumentUri)function.File;
            if (uri is not null && !uri.Equals(e.TextDocument.Uri)) continue;

            result.Add(new SymbolInformation()
            {
                Kind = SymbolKind.Function,
                Name = function.Identifier.Content,
                Location = new OmniSharpLocation()
                {
                    Range = function.Position.Range.ToOmniSharp(),
                    Uri = uri ?? e.TextDocument.Uri,
                },
            });
        }

        foreach (CompiledOperatorDefinition function in CompilerResult.OperatorDefinitions)
        {
            DocumentUri? uri = function.File is null ? null : (DocumentUri)function.File;
            if (uri is not null && !uri.Equals(e.TextDocument.Uri)) continue;

            result.Add(new SymbolInformation()
            {
                Kind = SymbolKind.Function,
                Name = function.Identifier.Content,
                Location = new OmniSharpLocation()
                {
                    Range = function.Position.Range.ToOmniSharp(),
                    Uri = uri ?? e.TextDocument.Uri,
                },
            });
        }

        foreach (CompiledGeneralFunctionDefinition function in CompilerResult.GeneralFunctionDefinitions)
        {
            DocumentUri? uri = function.File is null ? null : (DocumentUri)function.File;
            if (uri is not null && !uri.Equals(e.TextDocument.Uri)) continue;

            result.Add(new SymbolInformation()
            {
                Kind = SymbolKind.Function,
                Name = function.Identifier.Content,
                Location = new OmniSharpLocation()
                {
                    Range = function.Position.Range.ToOmniSharp(),
                    Uri = uri ?? e.TextDocument.Uri,
                },
            });
        }

        foreach (CompiledStruct @struct in CompilerResult.Structs)
        {
            DocumentUri? uri = @struct.File is null ? null : (DocumentUri)@struct.File;
            if (uri is not null && !uri.Equals(e.TextDocument.Uri)) continue;

            result.Add(new SymbolInformation()
            {
                Kind = SymbolKind.Struct,
                Name = @struct.Identifier.Content,
                Location = new OmniSharpLocation()
                {
                    Range = @struct.Position.ToOmniSharp(),
                    Uri = uri ?? e.TextDocument.Uri,
                },
            });
        }

        return result;
    }
}
