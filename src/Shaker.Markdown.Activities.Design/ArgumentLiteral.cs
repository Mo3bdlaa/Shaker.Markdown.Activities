using System;
using System.Activities;
using System.Activities.Expressions;
using System.Diagnostics;
using System.Reflection;
using Activity = System.Activities.Activity;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// Reads and writes the plain text behind an <see cref="InArgument{T}"/>, for the properties the canvas
    /// has to show without running the workflow.
    /// </summary>
    /// <remarks>
    /// An argument may hold a literal or an expression, and only the first can be drawn at design time: the
    /// second needs variables that do not exist until the process runs. So a literal round-trips through the
    /// card's editor, and an expression is reported as one rather than guessed at.
    /// </remarks>
    internal static class ArgumentLiteral
    {
        /// <summary>What an argument holds.</summary>
        internal enum Kind
        {
            /// <summary>Nothing has been set.</summary>
            Empty,

            /// <summary>Plain text, which the canvas can render and the card's editor can change.</summary>
            Literal,

            /// <summary>An expression, whose value is not known until the workflow runs.</summary>
            Expression
        }

        /// <summary>Reads an argument's text, and says whether it was text at all.</summary>
        internal static Kind Read(InArgument<string> argument, out string text)
        {
            text = null;

            if (argument?.Expression == null)
                return Kind.Empty;

            if (argument.Expression is Literal<string> literal)
            {
                text = literal.Value;
                return string.IsNullOrEmpty(text) ? Kind.Empty : Kind.Literal;
            }

            string expression = ExpressionTextOf(argument.Expression);

            if (string.IsNullOrWhiteSpace(expression))
                return Kind.Empty;

            // A VB or C# expression that is nothing but a quoted string is a literal wearing a costume, and
            // it is what Studio writes when somebody types plain text into the properties panel.
            if (TryUnquote(expression, out string unquoted))
            {
                text = unquoted;
                return string.IsNullOrEmpty(text) ? Kind.Empty : Kind.Literal;
            }

            text = expression;
            return Kind.Expression;
        }

        /// <summary>An argument holding exactly this text, for writing back from the card's editor.</summary>
        /// <remarks>
        /// A <see cref="Literal{T}"/> rather than a quoted expression, because the text is Markdown: it runs
        /// to many lines and is full of quotes and backslashes, none of which survive being pasted into a VB
        /// string literal without escaping that the reader would then have to look at.
        /// </remarks>
        internal static InArgument<string> From(string text) =>
            new InArgument<string>(new Literal<string>(text ?? string.Empty));

        /// <summary>
        /// The source text of an expression, whichever language it is written in.
        /// </summary>
        /// <remarks>
        /// By reflection over the <c>ExpressionText</c> property rather than through
        /// <see cref="ITextExpression"/>, because a project may be VB or C# and Studio's own expression types
        /// are not all in assemblies this one references.
        /// </remarks>
        private static string ExpressionTextOf(Activity expression)
        {
            try
            {
                if (expression is ITextExpression text)
                    return text.ExpressionText;

                return expression.GetType()
                    .GetProperty("ExpressionText", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(expression, null) as string;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not read an expression's text: " + exception);
                return null;
            }
        }

        /// <summary>
        /// Unwraps <c>"hello"</c> to <c>hello</c>, and refuses anything that is more than one string.
        /// </summary>
        internal static bool TryUnquote(string expression, out string text)
        {
            text = null;

            string trimmed = (expression ?? string.Empty).Trim();

            if (trimmed.Length < 2 || trimmed[0] != '"' || trimmed[trimmed.Length - 1] != '"')
                return false;

            string body = trimmed.Substring(1, trimmed.Length - 2);
            var builder = new System.Text.StringBuilder(body.Length);

            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];

                if (c != '"')
                {
                    // A backslash escape means C#, and unescaping it properly is more than this needs to do.
                    if (c == '\\')
                        return false;

                    builder.Append(c);
                    continue;
                }

                // A doubled quote is VB's escape. A lone one ends the string, so anything after it makes
                // this a concatenation rather than a literal.
                if (i + 1 < body.Length && body[i + 1] == '"')
                {
                    builder.Append('"');
                    i++;
                    continue;
                }

                return false;
            }

            text = builder.ToString();
            return true;
        }
    }
}
