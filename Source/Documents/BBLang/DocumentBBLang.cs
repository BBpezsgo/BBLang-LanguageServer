using System.Collections.Immutable;
using LanguageCore;
using LanguageCore.BBLang.Generator;
using LanguageCore.Compiler;
using LanguageCore.Parser;
using LanguageCore.Tokenizing;
using LanguageCore.Workspaces;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang : DocumentBase
{
    static readonly Dictionary<Uri, CacheItem> Cache = new();

    public ImmutableArray<Token> Tokens;
    public ParserResult AST;
    public CompilerResult CompilerResult;

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

        DesiredCompiledVersion = version;

        CompilationTask = Task.Run(async () =>
        {
            await Task.Delay(150).ConfigureAwait(false);

            if (CompiledVersion == DesiredCompiledVersion) return;

            await CompileAsync().ConfigureAwait(false);
            CompilationTask = null;

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

        Logger.Debug($"Validate");

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            DiagnosticsCollection diagnostics = new();

            Configuration config = Configuration.Parse([
                ..Documents.SelectMany(v => ConfigurationManager.Search(v.Uri, Documents)).DistinctBy(v => v.Uri)
            ], diagnostics);

            diagnostics.Clear();

            CompilerSettings compilerSettings = new(CodeGeneratorForMain.DefaultCompilerSettings)
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
                    compilerResult = StatementCompiler.CompileFiles(Documents.Select(v => v.Uri.ToString()).ToArray(), compilerSettings, diagnostics);
                }
                catch (LanguageException languageException)
                {
                    diagnostics.Add(languageException.ToDiagnostic());
                }

                ParsedFile raw = compilerResult.RawTokens.FirstOrDefault(v => v.File == Uri);
                Tokens = !raw.AST.Tokens.IsDefault ? raw.AST.Tokens : !raw.Tokens.Tokens.IsDefault ? Tokens : ImmutableArray<Token>.Empty;
                AST = raw.AST.IsNotEmpty ? raw.AST : AST;
                CompilerResult = compilerResult;

                compiledFiles = new(compilerResult.RawTokens.Select(v => v.File));
                Logger.Info($"Validated ({(diagnostics.HasErrors ? "failed" : "ok")})");
            }
            else if (Content is not null)
            {
                TokenizerResult tokens = Tokenizer.Tokenize(Content, diagnostics, compilerSettings.PreprocessorVariables, Uri, compilerSettings.TokenizerSettings);
                ParserResult ast = Parser.Parse(tokens.Tokens, Uri, diagnostics);
                Tokens = !ast.Tokens.IsDefault ? ast.Tokens : !tokens.Tokens.IsDefault ? tokens.Tokens : ImmutableArray<Token>.Empty;
                AST = ast.IsNotEmpty ? ast : AST;
                compiledFiles = new() { Uri };
                Logger.Info($"Validated (fallback)");
            }
            else
            {
                compiledFiles = new();
            }

            foreach (DiagnosticWithoutContext item in diagnostics.DiagnosticsWithoutContext)
            {
                Logger.Error(item.ToString());
            }

            Dictionary<Uri, List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>> diagnosticsPerFile = new();

            static string GetFullMessage(LanguageCore.Diagnostic diagnostic, int indent)
            {
                string result = $"{diagnostic.Message}";
                foreach (LanguageCore.Diagnostic item in diagnostic.SubErrors)
                {
                    result += $"\n{new string(' ', indent)} -> {GetFullMessage(item, indent + 2)}";
                }
                return result;
            }

            void CompileDiagnostic(LanguageCore.Diagnostic diagnostic, Dictionary<Uri, List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>> diagnosticsPerFile)
            {
                if (diagnostic.File is null) return;

                if (!diagnosticsPerFile.TryGetValue(diagnostic.File, out List<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic>? container))
                { container = diagnosticsPerFile[diagnostic.File] = new(); }

                container.Add(new OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic()
                {
                    Severity = diagnostic.Level.ToOmniSharp(),
                    Range = diagnostic.Position.ToOmniSharp(),
                    Message = GetFullMessage(diagnostic, 0),
                    Source = diagnostic.File.ToString(),
                });

                foreach (LanguageCore.Diagnostic item in diagnostic.SubErrors)
                {
                    if (item.Position.Equals(diagnostic.Position) && item.File == diagnostic.File) continue;
                    CompileDiagnostic(item, diagnosticsPerFile);
                }
            }

            foreach (LanguageCore.Diagnostic diagnostic in diagnostics.Diagnostics)
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
            OmniSharpService.Instance?.Server?.Window?.ShowError($"BBLang {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            CompiledVersion = CurrentlyCompilingVersion;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
