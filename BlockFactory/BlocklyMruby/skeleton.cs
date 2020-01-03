# jay skeleton for C#
#
# character in column 1 determines outcome...
# # is a comment
# . is copied
# t is copied as //t unless -t is set
# other lines are interpreted to call jay procedures
#
 	version	c# 1.1.0 (c) 2002-2006 ats@cs.rit.edu
.
 	prolog		## %{ ... %} prior to the first %%
.
.		// %token constants
 	tokens	
 	local		## %{ ... %} after the first %%
.
.		/// <summary>
.		///   final state of parser.
.		/// </summary>
 	yyFinal	protected const int yyFinal =
.
.		/// <summary>
.		///   parser tables.
.		///   Order is mandated by jay.
.		/// </summary>
.		protected static readonly short[] yyLhs = new short[] {
 	yyLhs
.		}, yyLen = new short[] {
 	yyLen
.		}, yyDefRed = new short[] {
 	yyDefRed
.		}, yyDgoto = new short[] {
 	yyDgoto
.		}, yySindex = new short[] {
 	yySindex
.		}, yyRindex = new short[] {
 	yyRindex
.		}, yyGindex = new short[] {
 	yyGindex
.		}, yyTable = new short[] {
 	yyTable
.		}, yyCheck = new short[] {
 	yyCheck
.		};
.
.		/// <summary>
.		///   maps symbol value to printable name.
.		///   see <c>yyExpecting</c>
.		/// </summary>
.		protected static readonly string[] yyNames = {
 	yyNames-strings
.		};
.
t		/// <summary>
t		///   printable rules for debugging.
t		/// </summary>
t		protected static readonly string[] yyRule = {
 	yyRule-strings
t		};
t
t		/// <summary>
t		///   debugging support, requires <c>yyDebug</c>.
t		///   Set to <c>null</c> to suppress debugging messages.
t		/// </summary>
t		protected yyDebugOut yyDebug;
t
t		/// <summary>
t		///   index-checked interface to <c>yyNames[]</c>.
t		/// </summary>
t		/// <param name='token'>single character or <c>%token</c> value</param>
t		/// <returns>token name or <c>[illegal]</c> or <c>[unknown]</c></returns>
t		public static string yyName(int token)
t		{
t			if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
t			string name;
t			if ((name = yyNames[token]) != null) return name;
t			return "[unknown]";
t		}
t
.		public static int yyToken(string name)
.		{
.			int token = 0;
.			foreach (var n in yyNames) {
.				if (n == name)
.					return token;
.				token++;
.			}
.			return yyErrorCode;
.		}
.
.		/// <summary>
.		///   thrown for irrecoverable syntax errors and stack overflow.
.		/// </summary>
.		/// <remarks>
.		///   Nested for convenience, does not depend on parser class.
.		/// </remarks>
.		public class yyException : System.Exception
.		{
.			public yyException(string message) : base(message)
.			{
.			}
.		}
.
.		/// <summary>
.		///   must be implemented by a scanner object to supply input to the parser.
.		/// </summary>
.		/// <remarks>
.		///   Nested for convenience, does not depend on parser class.
.		/// </remarks>
.		public interface yyInput
.		{
.			/// <summary>
.			///   move on to next token.
.			/// </summary>
.			/// <returns><c>false</c> if positioned beyond tokens</returns>
.			/// <exception><c>IOException</c> on input error</exception>
.			bool Advance();
.
.			/// <summary>
.			///   classifies current token by <c>%token</c> value or single character.
.			/// </summary>
.			/// <remarks>
.			///   Should not be called if <c>Advance()</c> returned false.
.			/// </remarks>
.			int Token { get; }
.
.			/// <summary>
.			///   value associated with current token.
.			/// </summary>
.			/// <remarks>
.			///   Should not be called if <c>Advance()</c> returned false.
.			/// </remarks>
.			object Value { get; }
.		}
.
.		public interface yyDebugOut
.		{
.			void push(int state, object value);
.			void lex(int state, int token, string name, object value);
.			void shift(int from, int to, int errorFlag);
.			void pop(int state);
.			void discard(int state, int token, string name, object value);
.			void reduce(int from, int to, int rule, string text, int len);
.			void shift(int from, int to);
.			void accept(object value);
.			void error(string message);
.			void reject();
.		}
.
.		public interface yyConsoleOut
.		{
.			void yyWarning(string message, object[] expected);
.			void yyError(string message, object[] expected);
.		}
.
.		public yyConsoleOut yyConsole;
.
.		/// <summary>
.		///   (syntax) warning message.
.		///   Can be overwritten to control message format.
.		/// </summary>
.		/// <param name='message'>text to be displayed</param>
.		/// <param name='expected'>list of acceptable tokens, if available</param>
.		public void yyWarning(string message, params object[] expected)
.		{
.			if (yyConsole == null)
.				return;
.			yyConsole.yyWarning(message, expected);
.		}
.
.		/// <summary>
.		///   (syntax) error message.
.		///   Can be overwritten to control message format.
.		/// </summary>
.		/// <param name='message'>text to be displayed</param>
.		/// <param name='expected'>list of acceptable tokens, if available</param>
.		public void yyError(string message, params object[] expected)
.		{
.			if (yyConsole == null)
.				return;
.			yyConsole.yyError(message, expected);
.		}
.
.		/// <summary>
.		///   computes list of expected tokens on error by tracing the tables.
.		/// </summary>
.		/// <param name='state'>for which to compute the list</param>
.		/// <returns>list of token names</returns>
.		protected string[] yyExpecting(int state)
.		{
.			int token, n, len = 0;
.			bool[] ok = new bool[yyNames.Length];
.
.			if ((n = yySindex[state]) != 0)
.				for (token = n < 0 ? -n : 0;
.					 (token < yyNames.Length) && (n + token < yyTable.Length); ++token)
.					if (yyCheck[n + token] == token && !ok[token] && yyNames[token] != null) {
.						++len;
.						ok[token] = true;
.					}
.			if ((n = yyRindex[state]) != 0)
.				for (token = n < 0 ? -n : 0;
.					 (token < yyNames.Length) && (n + token < yyTable.Length); ++token)
.					if (yyCheck[n + token] == token && !ok[token] && yyNames[token] != null) {
.						++len;
.						ok[token] = true;
.					}
.
.			string[] result = new string[len];
.			for (n = token = 0; n < len; ++token)
.				if (ok[token]) result[n++] = yyNames[token];
.			return result;
.		}
.
.		/// <summary>
.		///   the generated parser, with debugging messages.
.		///   Maintains a dynamic state and value stack.
.		/// </summary>
.		/// <param name='yyLex'>scanner</param>
.		/// <param name='yyDebug'>debug message writer implementing <c>yyDebug</c>,
.		///   or <c>null</c></param>
.		/// <returns>result of the last reduction, if any</returns>
.		/// <exceptions><c>yyException</c> on irrecoverable parse error</exceptions>
.		public object yyParse(yyInput yyLex, yyDebugOut yyDebug)
.		{
t			this.yyDebug = yyDebug;
.			return yyParse(yyLex);
.		}
.
.		/// <summary>
.		///   initial size and increment of the state/value stack [default 256].
.		///    This is not final so that it can be overwritten outside of invocations
.		///    of <c>yyParse()</c>.
.		/// </summary>
.		protected int yyMax;
.
.		protected int yyNest;
.
.		/// <summary>
.		///   executed at the beginning of a reduce action.
.		///   Used as <c>$$ = yyDefault($1)</c>, prior to the user-specified action, if any.
.		///   Can be overwritten to provide deep copy, etc.
.		/// </summary>
.		/// <param first value for $1, or null.
.		/// <return first.
.		protected object yyDefault(object first)
.		{
.			return first;
.		}
.
.		/// <summary>
.		///   the generated parser, with debugging messages.
.		///   Maintains a dynamic state and value stack.
.		/// </summary>
.		/// <param name='yyLex'>scanner</param>
.		/// <returns>result of the last reduction, if any</returns>
.		/// <exceptions><c>yyException</c> on irrecoverable parse error</exceptions>
.		public object yyParse(yyInput yyLex)
.		{
.			yyNest++;
.			if (yyMax <= 0) yyMax = 256;                // initial size
.			int yyState = 0;                            // state stack ptr
.			int[] yyStates = new JsArray<int>();        // state stack 
.			object yyVal = null;
.			object[] yyVals = new JsArray<object>();    // value stack
.			int yyToken = -1;                           // current input
.			int yyErrorFlag = 0;                        // #tokens to shift
.
.			for (int yyTop = 0; ; ++yyTop) {
.				while (yyTop >= yyStates.Length) {         // dynamically increase
.					yyStates.Push(0);
.					yyVals.Push(null);
.				}
.				yyStates[yyTop] = yyState;
.				yyVals[yyTop] = yyVal;
t				if (yyDebug != null) yyDebug.push(yyState, yyVal);
.
.				for (bool yyLoop = true; yyLoop;) { // discarding a token does not change stack
.					int yyN;
.					if ((yyN = yyDefRed[yyState]) == 0) {   // else [default] reduce (yyN)
.						if (yyToken < 0) {
.							yyToken = yyLex.Advance() ? yyLex.Token : 0;
t							if (yyDebug != null)
t								yyDebug.lex(yyState, yyToken, yyName(yyToken), yyLex.Value);
.						}
.						if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
.							&& (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
t							if (yyDebug != null)
t								yyDebug.shift(yyState, yyTable[yyN], yyErrorFlag > 0 ? yyErrorFlag - 1 : 0);
.							yyState = yyTable[yyN];     // shift to yyN
.							yyVal = yyLex.Value;
.							yyToken = -1;
.							if (yyErrorFlag > 0) --yyErrorFlag;
.							break;
.						}
.						if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
.							&& yyN < yyTable.Length && yyCheck[yyN] == yyToken)
.							yyN = yyTable[yyN];         // reduce (yyN)
.						else
.							switch (yyErrorFlag) {
.							case 0:
.							case 1:
.							case 2:
.								if (yyErrorFlag == 0) {
.									yyError("syntax error, expecting {0}", String.Join(",", yyExpecting(yyState)));
t									if (yyDebug != null) yyDebug.error("syntax error");
.								}
.								yyErrorFlag = 3;
.								do {
.									if ((yyN = yySindex[yyStates[yyTop]]) != 0
.										&& (yyN += yyErrorCode) >= 0 && yyN < yyTable.Length
.										&& yyCheck[yyN] == yyErrorCode) {
t										if (yyDebug != null)
t											yyDebug.shift(yyStates[yyTop], yyTable[yyN], 3);
.										yyState = yyTable[yyN];
.										yyVal = yyLex.Value;
.										yyLoop = false;
.										break;
.									}
t									if (yyDebug != null) yyDebug.pop(yyStates[yyTop]);
.								} while (--yyTop >= 0);
.								if (!yyLoop)
.									continue;
t								if (yyDebug != null) yyDebug.reject();
.								throw new yyException("irrecoverable syntax error");
.							case 3:
.								if (yyToken == 0) {
.									yyNest--;
.									if (yyNest <= 0)
.										return yyVal;
t									if (yyDebug != null) yyDebug.reject();
.									throw new yyException("irrecoverable syntax error at end-of-file");
.								}
t								if (yyDebug != null)
t									yyDebug.discard(yyState, yyToken, yyName(yyToken),
t									  yyLex.Value);
.								yyToken = -1;
.								continue;			// leave stack alone
.							}
.					}
.					int yyV = yyTop + 1 - yyLen[yyN];
t					if (yyDebug != null)
t						yyDebug.reduce(yyState, yyStates[yyV - 1], yyN, yyRule[yyN], yyLen[yyN]);
.					yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
.					switch (yyN) {

 	actions		## code from the actions within the grammar

.					}
.					yyTop -= yyLen[yyN];
.					yyState = yyStates[yyTop];
.					int yyM = yyLhs[yyN];
.					if (yyState == 0 && yyM == 0) {
t						if (yyDebug != null) yyDebug.shift(0, yyFinal);
.						yyState = yyFinal;
.						if (yyToken < 0) {
.							yyToken = yyLex.Advance() ? yyLex.Token : 0;
t							if (yyDebug != null)
t								yyDebug.lex(yyState, yyToken, yyName(yyToken), yyLex.Value);
.						}
.						if (yyToken == 0) {
t							if (yyDebug != null) yyDebug.accept(yyVal);
.							yyNest--;
.							return yyVal;
.						}
.						break;
.					}
.					if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
.						&& (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
.						yyState = yyTable[yyN];
.					else
.						yyState = yyDgoto[yyM];
t					if (yyDebug != null) yyDebug.shift(yyStates[yyTop], yyState);
.					break;
.				}
.			}
.		}
 	epilog			## text following second %%
