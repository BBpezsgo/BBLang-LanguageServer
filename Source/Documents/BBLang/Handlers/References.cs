using LanguageCore.Compiler;
using LanguageCore.Parser;
using LanguageCore.Parser.Statements;
using OmniSharpLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang
{
    public override async Task<IEnumerable<OmniSharpLocation>?> References(ReferenceParams e, CancellationToken cancellationToken)
    {
        await AwaitForCompilation(Version ?? 0, cancellationToken).ConfigureAwait(false);

        List<OmniSharpLocation> result = new();

        if (CompilerResult.GetFunctionAt(Uri, e.Position.ToCool(), out CompiledFunctionDefinition? function))
        {
            foreach (Reference<Expression?> reference in function.References.DistinctBy(v => v.Source))
            {
                if (reference.SourceFile == null) continue;
                if (reference.Source == null) continue;
                result.Add(new OmniSharpLocation()
                {
                    Range = reference.Source.Position.ToOmniSharp(),
                    Uri = reference.SourceFile,
                });
            }
        }

        if (CompilerResult.GetGeneralFunctionAt(Uri, e.Position.ToCool(), out CompiledGeneralFunctionDefinition? generalFunction))
        {
            foreach (Reference<Expression?> reference in generalFunction.References.DistinctBy(v => v.Source))
            {
                if (reference.SourceFile == null) continue;
                if (reference.Source == null) continue;
                result.Add(new OmniSharpLocation()
                {
                    Range = reference.Source.Position.ToOmniSharp(),
                    Uri = reference.SourceFile,
                });
            }
        }

        if (CompilerResult.GetOperatorAt(Uri, e.Position.ToCool(), out CompiledOperatorDefinition? @operator))
        {
            foreach (Reference<Expression> reference in @operator.References.DistinctBy(v => v.Source))
            {
                if (reference.SourceFile == null) continue;
                result.Add(new OmniSharpLocation()
                {
                    Range = reference.Source.Position.ToOmniSharp(),
                    Uri = reference.SourceFile,
                });
            }
        }

        if (CompilerResult.GetStructAt(Uri, e.Position.ToCool(), out CompiledStruct? @struct))
        {
            foreach (Reference<TypeInstance> reference in @struct.References.DistinctBy(v => v.Source))
            {
                if (reference.SourceFile == null) continue;
                result.Add(new OmniSharpLocation()
                {
                    Range = reference.Source.Position.ToOmniSharp(),
                    Uri = reference.SourceFile,
                });
            }
        }

        return result;
    }
}
