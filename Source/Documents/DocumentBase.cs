namespace LanguageServer;

abstract class DocumentBase
{
    public Uri Uri => DocumentUri.ToUri();
    public DocumentUri DocumentUri { get; private set; }
    public string? Content { get; private set; }
    public string LanguageId { get; private set; }
    public int? Version { get; private set; }
    public string Path
    {
        get
        {
            string result = Uri.AbsolutePath;
            result = System.Net.WebUtility.UrlDecode(result);
            result = System.IO.Path.GetFullPath(result);
            return result;
        }
    }
    protected Documents Documents { get; }

    protected DocumentBase(DocumentUri uri, string? content, string languageId, Documents app)
    {
        DocumentUri = uri;
        Content = content;
        LanguageId = languageId;
        Documents = app;
    }

    public virtual void OnOpened(DidOpenTextDocumentParams e)
    {
        Logger.Debug($"[DocBuffer] Updated ({e.TextDocument}) ({e.TextDocument.Text.Length})");
        Content = e.TextDocument.Text;

        DocumentUri = e.TextDocument.Uri;
        LanguageId = e.TextDocument.GetExtension();
        Version = e.TextDocument.Version;
    }

    public virtual void OnChanged(DidChangeTextDocumentParams e)
    {
        foreach (TextDocumentContentChangeEvent change in e.ContentChanges)
        {
            if (change.Range is not null)
            {
                Logger.Debug($"[DocBuffer] Updated ({e.TextDocument}) ({change.Range}, {change.Text.Length})");
            }
            else
            {
                Logger.Debug($"[DocBuffer] Updated ({e.TextDocument}) ({change.Text.Length})");
                Content = change.Text;
            }
        }

        string? text = e.ContentChanges.FirstOrDefault()?.Text;

        if (text != null)
        {
            Logger.Debug($"[DocBuffer] Updated ({e.TextDocument}) ({text.Length})");
            Content = text;
        }

        DocumentUri = e.TextDocument.Uri;
        LanguageId = e.TextDocument.GetExtension();
        Version = e.TextDocument.Version;
    }

    public virtual void OnSaved(DidSaveTextDocumentParams e)
    {
        if (e.Text != null)
        {
            Logger.Debug($"[DocBuffer] Updated ({e.TextDocument}) ({e.Text.Length})");
            Content = e.Text;
        }

        DocumentUri = e.TextDocument.Uri;
        LanguageId = e.TextDocument.GetExtension();
    }

    public abstract Hover? Hover(HoverParams request);
    public abstract IEnumerable<DocumentHighlight>? DocumentHighlight(DocumentHighlightParams request);
    public abstract IEnumerable<CodeLens>? CodeLens(CodeLensParams request);
    public abstract IEnumerable<Location>? References(ReferenceParams request);
    public abstract SignatureHelp? SignatureHelp(SignatureHelpParams request);
    public abstract void GetSemanticTokens(SemanticTokensBuilder builder, ITextDocumentIdentifierParams request);
    public abstract IEnumerable<CompletionItem>? Completion(CompletionParams request);
    public abstract LocationOrLocationLinks? GotoDefinition(DefinitionParams request);
    public abstract LocationOrLocationLinks? GotoDeclaration(DeclarationParams request);
    public abstract LocationOrLocationLinks? GotoTypeDefinition(TypeDefinitionParams request);
    public abstract LocationOrLocationLinks? GotoImplementation(ImplementationParams request);
    public abstract IEnumerable<SymbolInformationOrDocumentSymbol>? Symbols(DocumentSymbolParams request);

    public override string ToString() => $"{Path}";
}
