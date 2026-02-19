using System.Collections.Immutable;
using LanguageCore;
using LanguageCore.BBLang.Generator;
using LanguageCore.Compiler;
using LanguageCore.Parser;
using LanguageCore.Tokenizing;
using LanguageCore.Workspaces;
using Diagnostic = LanguageCore.Diagnostic;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang : DocumentBase
{
    static readonly Dictionary<Uri, CacheItem> Cache = new();

    public ImmutableArray<Token> Tokens;
    public ParserResult AST;
    public CompilerResult CompilerResult;

    public CompilerSettings CompilerSettings;

    Task? CompilationTask;
    int CompiledVersion;
    int CurrentlyCompilingVersion;
    int DesiredCompiledVersion;

    public DocumentBBLang(DocumentUri uri, string? content, string languageId, Documents app) : base(uri, content, languageId, app)
    {
        Tokens = ImmutableArray<Token>.Empty;
        AST = ParserResult.Empty;
        CompilerResult = CompilerResult.MakeEmpty(uri.ToUri());
    }

    void RequestCompilation(int version)
    {
        if (CompilationTask is not null && !CompilationTask.IsCompleted) return;
        if (CompiledVersion == version) return;
        if (DesiredCompiledVersion == version) return;

        Logger.Debug($"Requesting compilation for {version}");

        DesiredCompiledVersion = version;

        CompilationTask = Task.Run(async () =>
        {
            await Task.Delay(150).ConfigureAwait(false);

            if (CompiledVersion == DesiredCompiledVersion) return;

            await CompileAsync().ConfigureAwait(false);

            if (DesiredCompiledVersion != CompiledVersion)
            {
                RequestCompilation(DesiredCompiledVersion);
            }
        });
    }

    Task AwaitForCompilation(int version, CancellationToken cancellationToken)
    {
        RequestCompilation(version);
#pragma warning disable VSTHRD110 // Observe result of async calls
        return CompilationTask?.WaitAsync(cancellationToken) ?? Task.CompletedTask;
#pragma warning restore VSTHRD110 // Observe result of async calls
    }

    async Task CompileAsync()
    {
        CurrentlyCompilingVersion = Version ?? 0;

        Logger.Debug($"Validating {CurrentlyCompilingVersion} ...");

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            DiagnosticsCollection diagnostics = new();

            Configuration config = Configuration.Parse([
                ..Documents.OpenedDocuments.SelectMany(v => ConfigurationManager.Search(v.Uri, Documents)).DistinctBy(v => v.Uri)
            ], diagnostics);

            diagnostics.Clear();

            CompilerSettings compilerSettings = CompilerSettings = new(CodeGeneratorForMain.DefaultCompilerSettings)
            {
                Optimizations = OptimizationSettings.None,
                CompileEverything = true,
                PreprocessorVariables = PreprocessorVariables.Normal,
                SourceProviders = [
                    Documents,
                    new FileSourceProvider()
                    {
                        ExtraDirectories = config.ExtraDirectories,
                    },
                ],
                AdditionalImports = config.AdditionalImports,
                ExternalFunctions = config.ExternalFunctions.As<LanguageCore.Runtime.IExternalFunction>(),
                ExternalConstants = config.ExternalConstants,
                TokenizerSettings = new TokenizerSettings(TokenizerSettings.Default)
                {
                    TokenizeComments = true,
                },
                Cache = Cache,
            };
            HashSet<Uri> compiledFiles;
            if (DocumentUri.Scheme == "file")
            {
                CompilerResult compilerResult = CompilerResult.MakeEmpty(Uri);
                try
                {
                    compilerResult = StatementCompiler.CompileFiles(Documents.OpenedDocuments.Select(v => v.Uri.ToString()).ToArray(), compilerSettings, diagnostics);
                }
                catch (LanguageExceptionAt languageException)
                {
                    diagnostics.Add(languageException.ToDiagnostic());
                }

                ParsedFile raw = compilerResult.RawTokens.FirstOrDefault(v => v.File == Uri);
                Tokens = !raw.AST.Tokens.IsDefault ? raw.AST.Tokens : !raw.Tokens.Tokens.IsDefault ? Tokens : ImmutableArray<Token>.Empty;
                AST = raw.AST.IsNotEmpty ? raw.AST : AST;
                CompilerResult = compilerResult;

                compiledFiles = new(compilerResult.RawTokens.Select(v => v.File));
                Logger.Info($"Validated {CurrentlyCompilingVersion} ({(diagnostics.HasErrors ? "failed" : "ok")})");
            }
            else if (Content is not null)
            {
                TokenizerResult tokens = Tokenizer.Tokenize(Content, diagnostics, Uri, compilerSettings.PreprocessorVariables, compilerSettings.TokenizerSettings);
                ParserResult ast = Parser.Parse(tokens.Tokens, Uri, diagnostics);
                Tokens = !ast.Tokens.IsDefault ? ast.Tokens : !tokens.Tokens.IsDefault ? tokens.Tokens : ImmutableArray<Token>.Empty;
                AST = ast.IsNotEmpty ? ast : AST;
                compiledFiles = new() { Uri };
                Logger.Info($"Validated {CurrentlyCompilingVersion} (fallback)");
            }
            else
            {
                compiledFiles = new();
            }

            foreach (Diagnostic item in diagnostics.DiagnosticsWithoutContext)
            {
                Logger.Error(item.ToString());
            }

            Dictionary<Uri, List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>> diagnosticsPerFile = new();

            static string GetFullMessage(Diagnostic diagnostic, int indent)
            {
                string result = $"{diagnostic.Message}";
                foreach (Diagnostic item in diagnostic.SubErrors)
                {
                    result += $"\n{new string(' ', indent)} -> {GetFullMessage(item, indent + 2)}";
                }
                return result;
            }

            static void CompileDiagnostic(Diagnostic diagnostic, Dictionary<Uri, List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>> diagnosticsPerFile)
            {
                if (diagnostic is DiagnosticAt diagnosticWithPosition)
                {
                    if (diagnosticWithPosition.File is null) return;

                    if (!diagnosticsPerFile.TryGetValue(diagnosticWithPosition.File, out List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>? container))
                    { container = diagnosticsPerFile[diagnosticWithPosition.File] = new(); }

                    container.Add(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic()
                    {
                        Severity = diagnosticWithPosition.Level.ToOmniSharp(),
                        Range = diagnosticWithPosition.Position.ToOmniSharp(),
                        Message = GetFullMessage(diagnosticWithPosition, 0),
                        Source = diagnosticWithPosition.File.ToString(),
                    });

                    foreach (Diagnostic item in diagnostic.SubErrors)
                    {
                        if (item is DiagnosticAt diagnosticWithPosition2 && diagnosticWithPosition2.Location.Equals(diagnosticWithPosition.Location)) continue;
                        CompileDiagnostic(item, diagnosticsPerFile);
                    }
                }
                else
                {
                    foreach (Diagnostic item in diagnostic.SubErrors)
                    {
                        CompileDiagnostic(item, diagnosticsPerFile);
                    }
                }
            }

            foreach (DiagnosticAt diagnostic in diagnostics.Diagnostics)
            {
                CompileDiagnostic(diagnostic, diagnosticsPerFile);
            }

            foreach (Uri file in compiledFiles)
            {
                if (!diagnosticsPerFile.TryGetValue(file, out List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>? fileDiagnostics))
                {
                    fileDiagnostics = new();
                }

                int? version = null;
                if (Documents.TryGet(file, out DocumentBase? document)) version = document.Version;
                OmniSharpService.Instance?.Server?.PublishDiagnostics(new PublishDiagnosticsParams()
                {
                    Uri = file,
                    Diagnostics = fileDiagnostics,
                    Version = version,
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            OmniSharpService.Instance?.Server?.Window?.ShowError($"BBLang {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            CompiledVersion = CurrentlyCompilingVersion;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
