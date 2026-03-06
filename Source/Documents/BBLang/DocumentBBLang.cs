using System.Collections.Immutable;
using LanguageCore.Compiler;
using LanguageCore.Parser;
using LanguageCore.Tokenizing;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang : DocumentBase
{
    public DocumentBBLang(DocumentUri uri, string? content, string languageId, Documents app) : base(uri, content, languageId, app)
    {
        Tokens = ImmutableArray<Token>.Empty;
        AST = ParserResult.Empty;
        CompilerResult = CompilerResult.MakeEmpty(uri.ToUri());
    }
}
