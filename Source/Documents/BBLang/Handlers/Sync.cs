namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang
{
    public override void OnChanged(DidChangeTextDocumentParams e)
    {
        base.OnChanged(e);
    }

    public override void OnSaved(DidSaveTextDocumentParams e)
    {
        base.OnSaved(e);
        RequestCompilation(Version ?? 0);
    }

    public override void OnOpened(DidOpenTextDocumentParams e)
    {
        base.OnOpened(e);
        RequestCompilation(Version ?? 0);
    }
}
