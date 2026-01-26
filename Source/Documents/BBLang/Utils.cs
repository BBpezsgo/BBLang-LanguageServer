using System.Collections.Immutable;
using System.Text;
using LanguageCore;
using LanguageCore.Compiler;
using LanguageCore.Parser;
using LanguageCore.Tokenizing;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang
{
    bool GetCommentDocumentation(IPositioned position, Uri? file, [NotNullWhen(true)] out string? result)
        => GetCommentDocumentation(position.Position.Range.Start, file, out result);

    bool GetCommentDocumentation<TDefinition>(TDefinition definition, [NotNullWhen(true)] out string? result)
        where TDefinition : IPositioned, IInFile
    {
        if (definition is IHaveAttributes withAttributes)
        {
            return GetCommentDocumentation(definition.Position.Union(withAttributes.Attributes).Range.Start, definition.File, out result);
        }
        else
        {
            return GetCommentDocumentation(definition.Position.Range.Start, definition.File, out result);
        }
    }

    bool GetCommentDocumentation(SinglePosition position, Uri? file, [NotNullWhen(true)] out string? result)
    {
        result = null;

        if (file is null) return false;

        ImmutableArray<Token> tokens;

        if (Documents.TryGet(file, out DocumentBase? document) &&
            document is DocumentBBLang documentBBLang)
        {
            tokens = documentBBLang.Tokens;
        }
        else
        {
            ParsedFile f = CompilerResult.RawTokens.FirstOrDefault(v => v.File == file);
            if (f.File is null)
            {
                Logger.Warn($"Couldn't get comment documentation for {file}:{position.ToStringMin()} (file not found)");
                return false;
            }
            tokens = f.Tokens.Tokens;
        }

        if (tokens.IsDefault)
        {
            Logger.Warn($"Couldn't get comment documentation for {file}:{position.ToStringMin()} (file not tokenized)");
            return false;
        }

        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            Token token = tokens[i];
            if (token.Position.Range.Start >= position) continue;

            if (token.TokenType == TokenType.CommentMultiline)
            {
                StringBuilder parsedResult = new();
                string[] lines = token.Content.Split('\n');
                for (int j = 0; j < lines.Length; j++)
                {
                    string line = lines[j];
                    line = line.Trim();
                    if (line.StartsWith('*')) line = line[1..];
                    line = line.TrimStart();
                    parsedResult.AppendLine(line);
                }

                result = parsedResult.ToString();
                return true;
            }
            else if (token.TokenType == TokenType.Comment)
            {
                result = token.Content.Trim();
                return true;
            }

            break;
        }

        //Logger.Warn($"Couldn't get comment documentation for {file}:{position.ToStringMin()} (no comments found)");
        return false;
    }

    static void GetDeepestTypeInstance(ref TypeInstance type1, ref GeneralType type2, SinglePosition position)
    {
        switch (type1)
        {
            case TypeInstanceSimple simpleType:
                {
                    if (simpleType.TypeArguments.HasValue)
                    {
                        if (type2.Is(out StructType? structType) &&
                            structType.Struct.Template is not null)
                        {
                            for (int i = 0; i < simpleType.TypeArguments.Value.Length; i++)
                            {
                                TypeInstance? item = simpleType.TypeArguments.Value[i];
                                GeneralType? item2 = structType.TypeArguments[structType.Struct.Template.Parameters[i].Content];
                                if (item.Position.Range.Contains(position))
                                {
                                    type1 = item;
                                    type2 = item2;
                                    GetDeepestTypeInstance(ref type1, ref type2, position);
                                    return;
                                }
                            }
                        }
                    }

                    break;
                }
            case TypeInstancePointer pointerType:
                {
                    if (!type2.Is(out PointerType? pointerType2)) return;

                    if (pointerType.To.Position.Range.Contains(position))
                    {
                        type1 = pointerType.To;
                        type2 = pointerType2.To;
                        GetDeepestTypeInstance(ref type1, ref type2, position);
                        return;
                    }

                    break;
                }
            case TypeInstanceStackArray arrayType:
                {
                    if (!type2.Is(out ArrayType? arrayType2)) return;

                    if (arrayType.StackArrayOf.Position.Range.Contains(position))
                    {
                        type1 = arrayType.StackArrayOf;
                        type2 = arrayType2.Of;
                        GetDeepestTypeInstance(ref type1, ref type2, position);
                        return;
                    }

                    break;
                }
            case TypeInstanceFunction functionType:
                {
                    if (!type2.Is(out FunctionType? functionType2)) return;
                    if (functionType2.Parameters.Length != functionType.FunctionParameterTypes.Length) return;

                    if (functionType.FunctionReturnType.Position.Range.Contains(position))
                    {
                        type1 = functionType.FunctionReturnType;
                        type2 = functionType2.ReturnType;
                        GetDeepestTypeInstance(ref type1, ref type2, position);
                        return;
                    }

                    for (int i = 0; i < functionType.FunctionParameterTypes.Length; i++)
                    {
                        if (functionType.FunctionParameterTypes[i].Position.Range.Contains(position))
                        {
                            type1 = functionType.FunctionParameterTypes[i];
                            type2 = functionType2.Parameters[i];
                            GetDeepestTypeInstance(ref type1, ref type2, position);
                            return;
                        }
                    }

                    break;
                }
            default:
                break;
        }
    }
}
