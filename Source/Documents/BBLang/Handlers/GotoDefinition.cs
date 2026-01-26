using System.Collections.Immutable;
using LanguageCore;
using LanguageCore.Compiler;
using LanguageCore.Parser;
using LanguageCore.Parser.Statements;
using LanguageCore.Tokenizing;
using Position = LanguageCore.Position;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang
{
    bool GetGotoDefinition(object? reference, [NotNullWhen(true)] out LocationLink? result)
    {
        result = null;
        if (reference is null)
        { return false; }

        Uri file = Uri;

        if (reference is IInFile inFile)
        {
            if (inFile.File is null)
            { return false; }
            file = inFile.File;
        }

        if (reference is IIdentifiable<Token> identifiable1)
        {
            result = new LocationLink()
            {
                TargetRange = identifiable1.Identifier.Position.ToOmniSharp(),
                TargetSelectionRange = identifiable1.Identifier.Position.ToOmniSharp(),
                TargetUri = DocumentUri.From(file),
            };
            return true;
        }

        if (reference is ILocated located)
        {
            result = new LocationLink()
            {
                TargetRange = located.Location.Position.ToOmniSharp(),
                TargetSelectionRange = located.Location.Position.ToOmniSharp(),
                TargetUri = DocumentUri.From(located.Location.File),
            };
            return true;
        }

        return false;
    }

    public override async Task<LocationOrLocationLinks?> GotoDefinition(DefinitionParams e, CancellationToken cancellationToken)
    {
        await AwaitForCompilation(Version ?? 0, cancellationToken).ConfigureAwait(false);

        List<LocationOrLocationLink> links = new();

        foreach (UsingDefinition @using in AST.Usings.IsDefault ? ImmutableArray<UsingDefinition>.Empty : AST.Usings)
        {
            if (!@using.Position.Range.Contains(e.Position.ToCool()))
            { continue; }
            if (@using.CompiledUri is null)
            { break; }

            links.Add(new LocationOrLocationLink(new LocationLink()
            {
                TargetUri = DocumentUri.From(@using.CompiledUri),
                OriginSelectionRange = new Position(@using.Path.DefaultIfEmpty(@using.Keyword)).ToOmniSharp(),
                TargetRange = Position.Zero.ToOmniSharp(),
                TargetSelectionRange = Position.Zero.ToOmniSharp(),
            }));
            break;
        }

        if (AST.GetStatementAt(e.Position.ToCool(), out Statement? statement))
        {
            foreach (Statement item in StatementWalker.Visit(statement))
            {
                Position from = Utils.GetInteractivePosition(item);

                if (!from.Range.Contains(e.Position.ToCool()))
                { continue; }

                if (item is IReferenceableTo _ref1 &&
                    GetGotoDefinition(_ref1.Reference, out LocationLink? link))
                {
                    links.Add(new LocationLink()
                    {
                        OriginSelectionRange = from.Range.ToOmniSharp(),
                        TargetRange = link.TargetRange,
                        TargetSelectionRange = link.TargetSelectionRange,
                        TargetUri = link.TargetUri,
                    });
                }
            }
        }

        return new LocationOrLocationLinks(links);
    }
}
