namespace PascalNET.Core.Lexer.Tokens
{
    /// <summary>
    /// Типы токенов, на которые разбивается код лексером (лексическим анализатором)
    /// </summary>
    public enum TokenType
    {
        // Ключевые слова
        If,
        Then,
        Else,
        While,
        Do,
        Begin,
        End,
        Var,
        Mod,
        And,
        Or,
        Not,
        Program,
        Function,
        Procedure, Const,
        IntegerType,
        RealType,
        BooleanType,
        StringType,

        // Идентификаторы и литералы
        Identifier,
        IntegerLiteral,
        RealLiteral,
        StringLiteral,

        // Операторы
        Assign,      // :=
        Plus,        // +
        Minus,       // -
        Multiply,    // *
        Divide,      // /

        // Операторы сравнения
        Equal,       // =
        NotEqual,    // <>
        Less,        // <
        LessEqual,   // <=
        Greater,     // >
        GreaterEqual,// >=

        // Разделители
        Semicolon,   // ;
        Comma,       // ,
        Dot,         // .
        Colon,       // :
        LeftParen,   // (
        RightParen,  // )
        LeftBracket, // [
        RightBracket,// ]

        // Специальные токены
        Eof,
        Newline,
        Whitespace,
        Comment,
        Error
    }
}