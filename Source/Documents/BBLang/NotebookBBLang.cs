namespace LanguageServer.DocumentManagers;

sealed partial class NotebookBBLang : NotebookBase
{
    public NotebookBBLang(DocumentUri uri, string languageId, Documents app) : base(uri, languageId, app)
    {
    }
}
