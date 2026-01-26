using System.Collections.Immutable;
using System.Text;
using LanguageCore;
using LanguageCore.Compiler;
using LanguageCore.Parser;
using LanguageCore.Parser.Statements;
using LanguageCore.Tokenizing;
using Position = LanguageCore.Position;

namespace LanguageServer.DocumentManagers;

sealed partial class DocumentBBLang
{
    static string GetFunctionHover<TFunction>(TFunction function, ImmutableDictionary<string, GeneralType>? typeArguments)
        where TFunction : FunctionThingDefinition, ICompiledFunctionDefinition, IReadable
    {
        StringBuilder builder = new();

        IEnumerable<Token> modifiers = Utils.GetVisibleModifiers(function.Modifiers);
        if (modifiers.Any())
        {
            builder.AppendJoin(' ', modifiers);
            builder.Append(' ');
        }

        builder.Append(GeneralType.TryInsertTypeParameters(function.Type, typeArguments) ?? function.Type);
        builder.Append(' ');
        builder.Append(function.Identifier.ToString());
        if (function.Template != null)
        {
            builder.Append('<');
            builder.AppendJoin(", ", typeArguments is not null ? function.Template.Parameters.Select(v => typeArguments[v.Content].ToString()) : function.Template.Parameters.Select(v => v.Content));
            builder.Append('>');
        }
        builder.Append('(');
        for (int i = 0; i < function.Parameters.Count; i++)
        {
            if (i > 0) builder.Append(", ");
            builder.AppendJoin(' ', function.Parameters[i].Modifiers);
            if (function.Parameters[i].Modifiers.Length > 0)
            { builder.Append(' '); }

            builder.Append(GeneralType.TryInsertTypeParameters((function as ICompiledFunctionDefinition).Parameters[i].Type, typeArguments).ToString());

            builder.Append(' ');
            builder.Append(function.Parameters[i].Identifier.ToString());
        }
        builder.Append(')');
        return builder.ToString();
    }

    static string GetStructHover(CompiledStruct @struct)
    {
        StringBuilder builder = new();
        IEnumerable<Token> modifiers = Utils.GetVisibleModifiers(@struct.Modifiers);
        if (modifiers.Any())
        {
            builder.AppendJoin(' ', modifiers);
            builder.Append(' ');
        }

        builder.Append(DeclarationKeywords.Struct);
        builder.Append(' ');
        builder.Append(@struct.Identifier);
        return builder.ToString();
    }

    static string GetStructHover(StructDefinition @struct)
    {
        StringBuilder builder = new();
        IEnumerable<Token> modifiers = Utils.GetVisibleModifiers(@struct.Modifiers);
        if (modifiers.Any())
        {
            builder.AppendJoin(' ', modifiers);
            builder.Append(' ');
        }

        builder.Append(DeclarationKeywords.Struct);
        builder.Append(' ');
        builder.Append(@struct.Identifier);
        return builder.ToString();
    }

    static string GetTypeHover(GeneralType type) => type switch
    {
        StructType structType => $"{DeclarationKeywords.Struct} {structType.Struct.Identifier.Content}",
        GenericType genericType => $"(generic) {genericType.Identifier}",
        AliasType aliasType => $"{DeclarationKeywords.Alias} {aliasType.Identifier} {aliasType.Value}",
        _ => type.ToString()
    };

    static void HandleTypeHovering(Statement statement, ref string? typeHover)
    {
        if (statement is Expression statementWithValue &&
            statementWithValue.CompiledType is not null)
        { typeHover = GetTypeHover(statementWithValue.CompiledType); }
    }

    bool HandleDefinitionHover<TFunction>(StatementCompiler.FunctionQueryResult<TFunction> function, ref string? definitionHover, ref string? docsHover)
        where TFunction : FunctionThingDefinition, ICompiledFunctionDefinition, IReadable
    {
        if (function.OriginalFunction.File is null)
        { return false; }

        definitionHover = GetFunctionHover(function.Function, function.TypeArguments);
        GetCommentDocumentation(function.OriginalFunction, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(object? definition, ref string? definitionHover, ref string? docsHover) => definition switch
    {
        CompiledOperatorDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        CompiledFunctionDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        CompiledGeneralFunctionDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        CompiledVariableConstant v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        VariableDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        CompiledVariableDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        CompiledParameter v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        ParameterDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        CompiledField v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        FieldDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        CompiledStruct v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),
        StatementCompiler.FunctionQueryResult<CompiledFunctionDefinition> v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),

        StructDefinition v => HandleDefinitionHover(v, ref definitionHover, ref docsHover),

        _ => false,
    };

    bool HandleDefinitionHover<TFunction>(TFunction function, ref string? definitionHover, ref string? docsHover)
        where TFunction : FunctionThingDefinition, ICompiledFunctionDefinition, IReadable
    {
        if (function.File is null)
        { return false; }

        definitionHover = GetFunctionHover(function, null);
        GetCommentDocumentation(function, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(CompiledStruct @struct, ref string? definitionHover, ref string? docsHover)
    {
        if (@struct.File is null)
        { return false; }

        definitionHover = GetStructHover(@struct);
        GetCommentDocumentation(@struct, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(StructDefinition @struct, ref string? definitionHover, ref string? docsHover)
    {
        if (@struct.File is null)
        { return false; }

        definitionHover = GetStructHover(@struct);
        GetCommentDocumentation(@struct, out docsHover);
        return true;
    }

    /*
    bool HandleDefinitionHover(CompiledVariable variable, ref string? definitionHover, ref string? docsHover)
    {
        if (variable.File is null)
        { return false; }

        StringBuilder builder = new();

        if (variable.Modifiers.Contains(ModifierKeywords.Const))
        { builder.Append("(constant) "); }
        else
        { builder.Append("(variable) "); }

        if (variable.Modifiers.Length > 0)
        {
            builder.AppendJoin(' ', variable.Modifiers);
            builder.Append(' ');
        }
        builder.Append(variable.Type);
        builder.Append(' ');
        builder.Append(variable.Identifier);
        definitionHover = builder.ToString();

        GetCommentDocumentation(variable, out docsHover);
        return true;
    }
    */

    bool HandleDefinitionHover(VariableDefinition variable, ref string? definitionHover, ref string? docsHover)
    {
        if (variable.File is null)
        { return false; }

        StringBuilder builder = new();

        if (variable.Modifiers.Contains(ModifierKeywords.Const))
        { builder.Append("(constant) "); }
        else
        { builder.Append("(variable) "); }

        if (variable.Modifiers.Length > 0)
        {
            builder.AppendJoin(' ', variable.Modifiers);
            builder.Append(' ');
        }
        builder.Append(variable.CompiledType?.ToString() ?? variable.Type.ToString());
        builder.Append(' ');
        builder.Append(variable.Identifier);
        definitionHover = builder.ToString();

        GetCommentDocumentation(variable, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(CompiledVariableDefinition variable, ref string? definitionHover, ref string? docsHover)
    {
        StringBuilder builder = new();

        builder.Append("(variable) ");

        builder.Append(variable.Type.ToString());
        builder.Append(' ');
        builder.Append(variable.Identifier);
        definitionHover = builder.ToString();

        GetCommentDocumentation(variable, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(CompiledVariableConstant variable, ref string? definitionHover, ref string? docsHover)
    {
        StringBuilder builder = new();

        builder.Append("(constant) ");

        builder.Append(variable.Type.ToString());
        builder.Append(' ');
        builder.Append(variable.Identifier);

        builder.Append(" = ");
        builder.Append(variable.Value.ToStringValue());

        definitionHover = builder.ToString();

        GetCommentDocumentation(variable, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(CompiledParameter parameter, ref string? definitionHover, ref string? docsHover)
    {
        StringBuilder builder = new();
        builder.Append("(parameter) ");
        if (parameter.Modifiers.Length > 0)
        {
            builder.AppendJoin(' ', parameter.Modifiers);
            builder.Append(' ');
        }
        builder.Append(parameter.Type);
        builder.Append(' ');
        builder.Append(parameter.Identifier);
        definitionHover = builder.ToString();

        GetCommentDocumentation(parameter, parameter.File, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(ParameterDefinition parameter, ref string? definitionHover, ref string? docsHover)
    {
        StringBuilder builder = new();
        builder.Append("(parameter) ");
        if (parameter.Modifiers.Length > 0)
        {
            builder.AppendJoin(' ', parameter.Modifiers);
            builder.Append(' ');
        }
        builder.Append(parameter.Type);
        builder.Append(' ');
        builder.Append(parameter.Identifier);
        definitionHover = builder.ToString();

        GetCommentDocumentation(parameter, parameter.File, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(CompiledField field, ref string? definitionHover, ref string? docsHover)
    {
        if (field.Context is null)
        { return false; }
        if (field.Context.File is null)
        { return false; }

        definitionHover = $"(field) {field.Type} {field.Identifier}";
        GetCommentDocumentation(field, field.Context.File, out docsHover);
        return true;
    }

    bool HandleDefinitionHover(FieldDefinition field, ref string? definitionHover, ref string? docsHover)
    {
        if (field.Context is null)
        { return false; }
        if (field.Context.File is null)
        { return false; }

        definitionHover = $"(field) {field.Type} {field.Identifier}";
        GetCommentDocumentation(field, field.Context.File, out docsHover);
        return true;
    }

    bool HandleReferenceHovering(Statement statement, ref string? definitionHover, ref string? docsHover)
    {
        if (statement is IReferenceableTo _ref1 &&
            HandleDefinitionHover(_ref1.Reference, ref definitionHover, ref docsHover))
        { return true; }

        if (statement is VariableDefinition variableDeclaration)
        { return HandleDefinitionHover(variableDeclaration, ref definitionHover, ref docsHover); }

        return false;
    }

    public bool GetFieldAt(Uri file, SinglePosition position, [NotNullWhen(true)] out FieldDefinition? result)
    {
        foreach (StructDefinition @struct in AST.Structs.IsDefault ? ImmutableArray<StructDefinition>.Empty : AST.Structs)
        {
            if (@struct.File != file) continue;

            foreach (FieldDefinition field in @struct.Fields)
            {
                if (field.Identifier.Position.Range.Contains(position))
                {
                    result = field;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }

    public override async Task<Hover?> Hover(HoverParams e, CancellationToken cancellationToken)
    {
        SinglePosition position = e.Position.ToCool();

        Token? token = Tokens.GetTokenAt(position);

        if (token == null)
        {
            Logger.Debug($"No token at {e.Position.ToStringMin()} ({Tokens.Length})");
            return null;
        }

        Range<SinglePosition> range = token.Position.Range;

        {
            foreach (IHaveAttributes function1 in
                AST.Functions.Append(AST.Operators)
                .Append(AST.Structs.SelectMany(v => v.Functions.CastArray<IHaveAttributes>().Append(v.GeneralFunctions).Append(v.Operators).Append(v.Constructors)))
                .Append(AST.Structs)
                .Append(AST.AliasDefinitions))
            {
                foreach (AttributeUsage attribute in function1.Attributes)
                {
                    if (attribute.Identifier.Position.Range.Contains(position))
                    {
                        string? attributeHover = attribute.Identifier.Content switch
                        {
                            AttributeConstants.MSILIncompatibleIdentifier => "Marks the function not compatible with MSIL, therefore it won't be optimized using the IL generator",
                            AttributeConstants.BuiltinIdentifier => "Marks the function as built-in, so it will be used by the compiler to generate code for syntax sugars",
                            AttributeConstants.ExposeIdentifier => "Marks the function as exposable, so it can be called from outside the interpreter",
                            AttributeConstants.ExternalIdentifier => "Marks the function as external, as it's implementation is defined outside the interpreter",
                            AttributeConstants.InternalType => "Marks the type as the default one for the specified kind of values",
                            _ => null,
                        };

                        if (attributeHover is not null)
                        {
                            return new Hover()
                            {
                                Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
                                {
                                    Kind = MarkupKind.Markdown,
                                    Value = attributeHover,
                                }),
                                Range = attribute.Identifier.Position.Range.ToOmniSharp(),
                            };
                        }
                    }
                }
            }
        }

        string? typeHover = null;
        string? definitionHover = null;
        string? docsHover = null;

        if (CompilerResult.GetFunctionAt(Uri, position, out CompiledFunctionDefinition? function))
        {
            HandleDefinitionHover(function, ref definitionHover, ref docsHover);
        }
        else if (CompilerResult.GetGeneralFunctionAt(Uri, position, out CompiledGeneralFunctionDefinition? generalFunction))
        {
            HandleDefinitionHover(generalFunction, ref definitionHover, ref docsHover);
        }
        else if (CompilerResult.GetOperatorAt(Uri, position, out CompiledOperatorDefinition? @operator))
        {
            HandleDefinitionHover(@operator, ref definitionHover, ref docsHover);
        }
        else if (CompilerResult.GetStructAt(Uri, position, out CompiledStruct? @struct))
        {
            HandleDefinitionHover(@struct, ref definitionHover, ref docsHover);
        }
        else if (StatementExtensions.GetThingAt<StructDefinition, Token>(AST.Structs, Uri, position, out StructDefinition? @struct2))
        {
            HandleDefinitionHover(@struct2, ref definitionHover, ref docsHover);
        }
        else if (CompilerResult.GetFieldAt(Uri, position, out CompiledField? field))
        {
            HandleDefinitionHover(field, ref definitionHover, ref docsHover);
        }
        else if (GetFieldAt(Uri, position, out FieldDefinition? field2))
        {
            HandleDefinitionHover(field2, ref definitionHover, ref docsHover);
        }
        else if (CompilerResult.GetParameterDefinitionAt(Uri, position, out ParameterDefinition? parameter, out _) &&
                 parameter.Identifier.Position.Range.Contains(position))
        {
            HandleDefinitionHover(parameter, ref definitionHover, ref docsHover);
        }
        else if (AST.GetStatementAt(position, out Statement? statement))
        {
            Logger.Debug(statement);
            foreach (Statement item in StatementWalker.Visit(statement))
            {
                if (!item.Position.Range.Contains(e.Position.ToCool())) continue;

                Position checkPosition = Utils.GetInteractivePosition(item);

                if (!checkPosition.Range.Contains(e.Position.ToCool())) continue;

                range = checkPosition.Range;

                HandleDefinitionHover(item, ref definitionHover, ref docsHover);
                HandleTypeHovering(item, ref typeHover);
                HandleReferenceHovering(item, ref definitionHover, ref docsHover);
            }
        }
        else
        {
            foreach (UsingDefinition @using in AST.Usings)
            {
                if (new Position(@using.Path.DefaultIfEmpty(@using.Keyword)).Range.Contains(e.Position.ToCool()))
                {
                    if (@using.CompiledUri != null)
                    { definitionHover = $"{@using.Keyword} \"{@using.CompiledUri}\""; }
                    break;
                }
            }
        }

        if (typeHover is null &&
            (AST, CompilerResult).GetTypeInstanceAt(Uri, e.Position.ToCool(), out TypeInstance? typeInstance, out GeneralType? generalType))
        {
            GetDeepestTypeInstance(ref typeInstance, ref generalType, e.Position.ToCool());

            range = typeInstance.Position.Range;
            typeHover = GetTypeHover(generalType);
        }

        StringBuilder contents = new();

        if (definitionHover is not null)
        {
            if (contents.Length > 0) contents.AppendLine("---");
            contents.AppendLine($"```{LanguageConstants.LanguageId}");
            contents.AppendLine(definitionHover);
            contents.AppendLine("```");
        }
        else if (typeHover is not null)
        {
            if (contents.Length > 0) contents.AppendLine("---");
            contents.AppendLine($"```{LanguageConstants.LanguageId}");
            contents.AppendLine(typeHover);
            contents.AppendLine("```");
        }

        if (docsHover is not null)
        {
            if (contents.Length > 0) contents.AppendLine("---");
            contents.AppendLine(docsHover);
        }

        return new Hover()
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent()
            {
                Kind = MarkupKind.Markdown,
                Value = contents.ToString(),
            }),
            Range = range.ToOmniSharp(),
        };
    }
}
