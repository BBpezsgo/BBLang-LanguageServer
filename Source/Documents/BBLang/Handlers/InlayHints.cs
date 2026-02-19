using System.Diagnostics;
using LanguageCore;
using LanguageCore.Parser;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang
{
    public override async Task<IEnumerable<InlayHint>?> InlayHints(InlayHintParams request, CancellationToken cancellationToken)
    {
        await AwaitForCompilation(Version ?? 0, cancellationToken).ConfigureAwait(false);

        MutableRange<SinglePosition> range = request.Range.ToCool();
        List<InlayHint> result = new();

        static IEnumerable<TypeInstance> EnumerateNestedTypeInstances(TypeInstance v)
        {
            yield return v;
            switch (v)
            {
                case TypeInstanceFunction w:
                    foreach (TypeInstance x in EnumerateNestedTypeInstances(w.FunctionReturnType)) yield return x;
                    foreach (TypeInstance x in w.FunctionParameterTypes.SelectMany(v => EnumerateNestedTypeInstances(v))) yield return x;
                    break;
                case TypeInstancePointer w:
                    foreach (TypeInstance x in EnumerateNestedTypeInstances(w.To)) yield return x;
                    break;
                case TypeInstanceSimple w:
                    if (w.TypeArguments.HasValue) foreach (TypeInstance x in w.TypeArguments.Value.SelectMany(v => EnumerateNestedTypeInstances(v))) yield return x;
                    break;
                case TypeInstanceStackArray w:
                    foreach (TypeInstance x in EnumerateNestedTypeInstances(w.StackArrayOf)) yield return x;
                    break;
                case MissingTypeInstance: break;
                default: throw new UnreachableException();
            }
        }

        foreach (TypeInstance item in AST.EnumerateTypeInstances())
        {
            if (!RangeUtils.Overlaps(range, item.Position.Range)) continue;

            foreach (TypeInstance type in EnumerateNestedTypeInstances(item))
            {
                if (type is TypeInstanceStackArray arrayType
                    && arrayType.StackArraySize is null
                    && arrayType.CompiledType is not null
                    && arrayType.CompiledType.Length.HasValue)
                {
                    result.Add(new InlayHint()
                    {
                        Kind = InlayHintKind.Parameter,
                        Label = new StringOrInlayHintLabelParts(arrayType.CompiledType.Length.Value.ToString()),
                        Position = arrayType.SquareBrackets.Start.Position.Range.End.ToOmniSharp(),
                        TextEdits = new Container<TextEdit>(new TextEdit()
                        {
                            NewText = arrayType.CompiledType.Length.Value.ToString(),
                            Range = new Range<SinglePosition>(arrayType.SquareBrackets.Start.Position.Range.End, arrayType.SquareBrackets.Start.Position.Range.End).ToOmniSharp(),
                        })
                    });
                }
            }
        }

        return result;
    }
}
