using System.Collections.Concurrent;
using System.Data;
using System.Text;

namespace TabulaLuma;

public enum TermType
{
    StringLiteral,
    Literal,
    Variable,
    Symbol,
}
public class Term
{
    public int Ordinal { get; set; } = -1;
    public string Value { get; private set; } = string.Empty;
    public TermType Type { get; private set; }
   
    public Term(TermType type, string value)
    {
        Type = type;
        Value = value;
    }
    public bool IsLiteral => Type == TermType.Literal || Type == TermType.StringLiteral;
    public bool IsVariable => Type == TermType.Variable;

    override public string ToString()
    {
        return this.Type switch 
        {
            TermType.StringLiteral => $"\"{Value}\"",
            TermType.Literal => $"({Value})",
            TermType.Variable => $"/{Value}/",
            TermType.Symbol => Value,
            _ => Value,
        };
    }

}
public class Clause
{
    static int nextOrdinal = 0;
    List<Term> terms = new List<Term>();
    List<Clause> optionalClauses = new List<Clause>();
    public Dictionary<string, string> Options { get; private set; } = new Dictionary<string, string>();
    public Clause()
    {
        nextOrdinal = 0;
    }
    public Clause[] OptionalClauses { get { return optionalClauses.ToArray(); } }
    public Term[] Terms { get { return terms.ToArray(); } }
    public int Count { get { return terms.Count; } }
    public string Relation { get { return string.Join(" ", this.Terms.Select(t => t.IsLiteral || t.IsVariable ? "_" : t.Value)); } }
    public void AddOption(string name, string value)
    {
        Options[name] = value;
    }
    public void AddOptionClause(Clause clause)
    {
        optionalClauses.Add(clause);
    }   
    public void Add(Term term)
    {
        terms.Add(term);
        term.Ordinal = nextOrdinal++;
    }

    public int[] GetOrdinals(string name)
    {
        var ordinals = new List<int>();
        foreach(var term in Terms)
        {
            if(term.IsVariable && term.Value == name)
            {
                ordinals.Add(term.Ordinal);
            }
        }
        return ordinals.ToArray();
    }
    public string GetTermValue(int ordinal)
    {
        return Terms[ordinal].Value;
    }
    public override string ToString()
    {
        return Terms.Select(t => t.ToString()).Aggregate( (a,b) => a + " " + b);
    }

}
public abstract class Statement
{
    List<Clause> clauses = new List<Clause>();
    public string Text { get; protected set; } = string.Empty;
    public void AddClause(Clause clause)
    {
        clauses.Add(clause);
    }
    public void AddClauseRange(Clause[] clauses)
    {
        this.clauses.AddRange(clauses);
    }
    protected Clause[] Clauses { get => clauses.ToArray(); }
    public int Id { get; set; } = -1;
    public ulong StartTimestanp { get; set; } = 0;
    public ulong StopTimestamp { get; set; } = 0;
    public ulong ElapsedUs { get; set; }
    public int Hash { get; private set; } = 0;
    public int Ordinal { get; set; } = -1;
    protected static ILoggingService loggingService = ServiceProvider.GetService<ILoggingService>();
    public class Token(TokenType tokenType)
    {         
        List<char> chars = new List<char>();
       
        public TokenType TokenType { get;} = tokenType;
        public string ErrorMessage { get; set; } = string.Empty;
        public string Value { get => new string(chars.ToArray()); }
        public void Append(char ch)
        {
            chars.Add(ch);
        }
    }
    public enum TokenType
    {
        StringLiteral,
        Literal,
        Optional,
        Variable,
        Symbol,
        ClauseDelimiter,
        Error,
        EndOfInput
    }
    static ParseResult LogAndMakeErrorResult( IEnumerable<string> errorMessages)
    {
        var result = new ParseResult(false);
        result.AddErrorRange(errorMessages);
      
        loggingService.LogErrors(result.ErrorMessages);
        return result;
    }
    public static ParseResult TryParse<T>(int id, string text, Action<Binding>? action, out T? statement) where T : Statement
    {
        var tokens = Tokenize<T>(text);
        if(tokens == null || !tokens.Any())
        {
            statement = null;
            return LogAndMakeErrorResult(["Empty statement"]);        
        }
        if(tokens.Any(t => t.TokenType == TokenType.Error))
        {
            var errorTokens = tokens.Where(t => t.TokenType == TokenType.Error);
            statement = null;
            return LogAndMakeErrorResult(errorTokens.Select(e => $"{id} Parse error: {errorTokens.First().ErrorMessage} \"{text}\""));
        }

        if (typeof(T) == typeof(When))
        {
            statement = (T)(Statement)new When() { Id = id, Text = text, Action = action };
        }
        else
            statement = (T)(Statement)new Claim() { Id = id, Text = text };

        var clause = new Clause();
        bool parsingOptionList = false;
        string optionName = "error";
        bool expectName = true;
        foreach (var token in tokens)
        {
            if(parsingOptionList)
                switch(token.TokenType)
                {
                    
                    case TokenType.EndOfInput:
                        if(!expectName)
                            return LogAndMakeErrorResult([$"Expecting a Literal or Variable in options list near {optionName}"]);
                        if (clause.Count > 0)
                        {
                            ((Statement)statement).AddClause(clause);
                        }
                        statement.Hash = HashCode.Combine(string.Join(" ", statement.Clauses.Select(c => c.ToString())), id);
                        return new ParseResult(true);
                    case TokenType.StringLiteral:
                    case TokenType.Literal:
                    case TokenType.Variable:
                        if (!expectName)
                        {
                            clause.AddOption(optionName, token.Value);
                            expectName = true;
                        }
                        else
                            return LogAndMakeErrorResult([$"Expecting a Literal or Variable in options list near {token.Value}"]);
                        break;
                    case TokenType.Symbol:
                        if (expectName)
                        {
                            optionName = token.Value;
                            expectName = false;
                        }
                        else
                            return LogAndMakeErrorResult([$"Expecting a name in options list near {token.Value}"]);
                        break;
                    case TokenType.ClauseDelimiter:
                        if (clause.Count > 0)
                        {
                            ((Statement)statement).AddClause(clause);
                            clause = new Clause();
                            parsingOptionList = false;
                        }
                        break;
                    default:                      
                        break;
                }
            else
                switch (token.TokenType)
                {
                    case TokenType.ClauseDelimiter:
                        if (clause.Count > 0)
                        {
                            ((Statement)statement).AddClause(clause);
                            clause = new Clause();
                        }
                        break;
                    case TokenType.Optional:
                        // parse optional clause
                        if (Claim.TryParse(-1, token.Value, out var optClaim).Success)
                        {
                            clause.AddOptionClause(optClaim.Clauses.First());
                        }
                        break;
                    case TokenType.EndOfInput:
                        ((Statement)statement).AddClause(clause);
                        statement.Hash = HashCode.Combine(string.Join(" ", statement.Clauses.Select(c => c.ToString())), id);
                        return new ParseResult(true);
                    case TokenType.StringLiteral:
                        clause.Add(new Term(TermType.StringLiteral, token.Value));
                        break;
                    case TokenType.Literal:
                        clause.Add(new Term(TermType.Literal, token.Value == "you" ? id.ToString() : token.Value));
                        break;
                    case TokenType.Variable:
                        clause.Add(new Term(TermType.Variable, token.Value));
                        break;
                    case TokenType.Symbol:
                        if (token.Value == "with")
                        {
                            parsingOptionList = true;
                            expectName = true;
                        }
                        else
                            clause.Add(new Term(TermType.Symbol, token.Value));
                        break;
                    default:
                        break;
                }
        }
        return LogAndMakeErrorResult([$"End of Input token not found \"{text}\""]);
    }

    public static IEnumerable<Token> Tokenize<T>(string text) where T : Statement
    { 
        if (string.IsNullOrEmpty(text))
            yield break;

        char delimiter = ' ';
        char clauseDelimiter = ',';
        var groupDefs = new Tuple<TokenType, char, char>[]
        {
            new Tuple<TokenType, char, char>(TokenType.StringLiteral, '"', '"'),
            new Tuple<TokenType, char, char>(TokenType.StringLiteral, '\'', '\''),
            new Tuple<TokenType, char, char>(TokenType.Literal, '(', ')'),
            new Tuple<TokenType, char, char>(TokenType.Optional, '[', ']'),
            new Tuple<TokenType, char, char>(TokenType.Variable, '/', '/')
        };
    
        var groupDict = groupDefs.ToDictionary(g => g.Item2, g => new Tuple<TokenType, char>(g.Item1, g.Item3));

        char prevChar;
        char nextChar;
        char currChar;
        char groupBeginChar = '\0';
        char groupEndChar = '\0';
        bool inGroup = false;
        Token? token = null;
        for (int i = 0; i < text.Length; i++)
        {
            currChar = text[i];
            if (i > 0)
                prevChar = text[i - 1];
            else
                prevChar = '\0';

            if (i + 1 < text.Length)
                nextChar = text[i + 1];
            else
                nextChar = '\0';

            if (groupDict.ContainsKey(currChar) && (prevChar == '\0' || prevChar == delimiter) && !inGroup)
            {
                groupBeginChar = currChar;
                groupEndChar = groupDict[currChar].Item2;

                token = new Token(groupDict[currChar].Item1);

                inGroup = true;
                continue;
            }
            if (currChar == groupEndChar && (nextChar == '\0' || nextChar == delimiter) && inGroup)
            {
                if (groupEndChar == '/' && token != null && token.Value.Contains(','))
                    yield return new Token(TokenType.Error) { ErrorMessage = $"Forbidden char in variable: '{currChar}'" };
                else if (groupEndChar == '/' && typeof(T) != typeof(When))
                    yield return new Token(TokenType.Error) { ErrorMessage = $"Variables only allowed in When statements: '{currChar}'" };
                else
                {
                    if (token != null)
                        yield return token;
                }
                token = null;
                inGroup = false;
                continue;
            }
            if(currChar == clauseDelimiter && !inGroup)
            {
                if (token != null)
                {
                    yield return token;
                }
                yield return new Token(TokenType.ClauseDelimiter);
                
                token = null;
                continue;
            }
            if (currChar == delimiter && !inGroup)
            {
                if (token != null)
                    yield return token;
                token = null;
                continue;
            }
            if(token == null)
            {
                token = new Token(TokenType.Symbol);
            }
            token.Append(currChar);
        }
        if(token != null)
            yield return token;

        if(inGroup)
        {
            yield return new Token(TokenType.Error) { ErrorMessage = $"Unmatched char: {groupBeginChar}" } ;
        }
        yield return new Token(TokenType.EndOfInput);
    }
}
public class ParseResult
{
    public ParseResult(bool success)
    {
        Success = success;
    }
    List<string> errorMessages = new List<string>();
    public static ParseResult Default = new ParseResult(false);
    public bool Success { get; private set; } = false;
    public string[] ErrorMessages { get => errorMessages.ToArray(); }
    public void AddError(string errorMessage)
    {
        errorMessages.Add(errorMessage);
    }
    public void AddErrorRange(IEnumerable<string> errorMessages)
    {
        this.errorMessages.AddRange(errorMessages);
    }

}
public class Claim : Statement
{
    override public string ToString()
    {
        return $"{Id} claims " + Text;
    }
    public Clause Clause { get { return base.Clauses[0]; } }

    public static ParseResult TryParse(int id, string text, out Claim? statement)
    {
        return Statement.TryParse<Claim>(id, text, null, out statement);
    }
    public int[] MakeKeys()
    { 
        var terms = this.Clause.Terms.Where(t => t.IsLiteral || t.IsVariable).ToArray();
        var keys = new List<int>();
        int relationKey = this.Clause.Relation.GetHashCode();
        keys.Add( relationKey);

        if(terms.Length > 0)
        {
            if(terms[0].IsLiteral)
                keys.Add(relationKey + terms[0].Value.GetHashCode() * 3);
        }
        if(terms.Length > 1)
        {
            if (terms[1].IsLiteral)
                keys.Add(relationKey + terms[1].Value.GetHashCode() * 5);

            if (terms[0].IsLiteral && terms[1].IsLiteral)
                keys.Add(relationKey + terms[0].Value.GetHashCode() * 3 + terms[1].Value.GetHashCode() * 5);
        }

        return keys.ToArray(); 
    }
}
public class OtherwiseBuider
{
    When when;
    public OtherwiseBuider(When when)
    {
     this.when = when;
    }
    public void Otherwise(Action func)
    {
        when.OtherwiseAction = func;
    }
}
public class WhenBuilder
{
    string text;
    Database db;
    int id;
    public WhenBuilder(string text, Database db, int id)
    {
        this.text = text;
        this.db = db;
        this.id = id;
    }
    public WhenBuilder And(string text)
    {
        this.text += " , " + text;
        return this;
    }
    public OtherwiseBuider And(string text, Action<Binding> action)
    {
        this.text += " , " + text;
        var when = db.AddWhenStatement(id, this.text, action);

        return new OtherwiseBuider(when);
    }
}
public class When : Statement
{    
    public List<Claim[]> Matches = new List<Claim[]>();
    public Action<Binding>? Action { get; set; } = null;
    public Action? OtherwiseAction { get; set; } = null;
  
    override public string ToString()
    {
        return $"{Id} When " + Text;
    }

    new public Clause[] Clauses { get { return base.Clauses; } }
    public Binding Binding = new Binding();

    public static ParseResult TryParse(int id, string text, Action<Binding>? action, out When? statement)
    {
         return Statement.TryParse<When>(id, text, action, out statement);
    }
 

    /// <summary>
    /// Returns an array of query keys for this when statement
    /// AND statements will return a key for each clause
    /// </summary>
    /// <returns></returns>
    public int[] MakeQueryKeys()
    {    
        int[] keys = new int[Clauses.Length];
        for(int c = 0; c < Clauses.Length; c++) 
        {
            var clause = Clauses[c];
            var terms = clause.Terms.Where(t => t.IsLiteral || t.IsVariable).ToArray();

            keys[c] = clause.Relation.GetHashCode();

            if(terms.Length > 0 && terms[0].IsLiteral)
            {
                 keys[c] = keys[c] + terms[0].Value.GetHashCode() * 3;              
            }
            if(terms.Length > 1 && terms[1].IsLiteral)
            {
                 keys[c] = keys[c] + terms[1].Value.GetHashCode() * 5;
            }        
        }
   
        return keys;
    }
}
public class WhenProcessor
{
    When when;
    List<Claim>[] claimsArray;
    int clauseCount = 0;
    Database database;
    public WhenProcessor(Database database, When when)
    {
        this.database = database;
        this.when = when;
        clauseCount = when.Clauses.Length;
        claimsArray = new List<Claim>[clauseCount];
    }
    public bool Process()
    {
        var queryKeys = when.MakeQueryKeys();

        if (!GetMatchingClaims(queryKeys))
            return false;

        if(clauseCount == 1)
        {
            //simple case
            // bind and execute when for all claims
            foreach(var claim in claimsArray[0])
                BindAndExecute([claim]);
        }
        else
        {
            // get variables that occur more than once
            var sharedVars = when.Clauses.SelectMany(c => c.Terms.Where(t => t.IsVariable))
                                .GroupBy(v => v.Value).Where(g => g.Count() > 1).Select(g => g.Key);

            foreach(var combo in GetCombinations(claimsArray))
            {
                if (!sharedVars.Any())
                {
                    // no shared vars, always a match
                    BindAndExecute(combo);
                }
                else
                {
                    bool allSharedVarsMatch = true;
                    foreach (var sharedVar in sharedVars)
                    {
                        if (!SharedVarsEqual(sharedVar, combo.ToArray()))
                        {
                            allSharedVarsMatch = false;
                            break;
                        }
                    }
                    if (allSharedVarsMatch)
                    {
                        // make binding and execute when
                        BindAndExecute(combo);
                    }
                }
            }
        }
        return true;
    }
    bool GetMatchingClaims(int[] queryKeys)
    {
       
        for (int i = 0; i < clauseCount; i++)
        {
            if (!database.claimByHash.TryGetValue(queryKeys[i], out var claims))
                return false; // every clause must match

            claimsArray[i] = new List<Claim>(claims);
        }
        return true;
    }
  
    bool SharedVarsEqual(string varName, Claim[] claims)
    {
        string? value = null;
        for (int i = 0; i < claims.Length; i++)
        {
            var clause = claims[i].Clause;
            foreach (var ordinal in when.Clauses[i].GetOrdinals(varName))
            {
                if (value == null)
                    value = clause.GetTermValue(ordinal);
                else if (value != clause.GetTermValue(ordinal))
                    return false; // not a match
            }
        }
        return true;
    }
  
    IEnumerable<Claim[]> GetCombinations(List<Claim>[] claimsArrays)
    {
        if (claimsArrays == null || claimsArrays.Length == 0)
            yield break;

        IEnumerable<Claim[]> GenerateCombinations(int index, List<Claim> prefix)
        {
            // Base case: if we've processed all arrays, yield the current combination
            if (index == claimsArrays.Length)
            {
                yield return prefix.ToArray();
                yield break;
            }

            // Iterate through the current array's elements
            foreach (var claim in claimsArrays[index])
            {
                // Add the current element to the prefix
                prefix.Add(claim);

                // Recursively generate combinations for the next index
                foreach (var combination in GenerateCombinations(index + 1, prefix))
                {
                    yield return combination;
                }

                // Backtrack: remove the last added element
                prefix.RemoveAt(prefix.Count - 1);
            }
        }

        // Start the recursive combination generation with an empty prefix
        foreach (var combination in GenerateCombinations(0, new List<Claim>()))
        {
            yield return combination;
        }
    }

    bool AlreadyMatched(Claim[] claims)
    {
        foreach (var match in when.Matches)
        {
            var allMatched = true;
            for (int i = 0; i < clauseCount; i++)
            {
                if (match[i].Hash != claims[i].Hash)
                {
                    allMatched = false;
                    break;
                }
            }
            if(allMatched)
                return true;

        }
        return false;
    }
    void  BindAndExecute(Claim[] claims)
    {
        if (AlreadyMatched(claims))
            return;
        when.Matches.Add(claims);
        var binding = new Binding();

        for(int i = 0; i < clauseCount; i++)
        {
            MakeBinding(when.Clauses[i], claims[i].Clause, ref binding);
        }
  
        database.Log($"Match: When {when.Id} {when} MATCHES claim(s): {string.Join(" && ", claims.Select(c => c))}");
        var start = Utils.GetElapsedMicroseconds();
        var sid = Stats.Begin(when.ToString());
        when.Action?.Invoke(binding);
        Stats.End(sid);
    }
    public static void MakeBinding(Clause whenClause, Clause claimClause, ref Binding binding)
    {
        try
        {
            // all variable and literal terms' names must match
            for (int i = 0; i < whenClause.Terms.Length; i++)
            {
                var t = whenClause.Terms[i];
                if (!t.IsVariable && !t.IsLiteral && t.Value != claimClause.Terms[i].Value)
                    return;
            }

            for (int i = 0; i < whenClause.Terms.Length; i++)
            {
                if (whenClause.Terms[i].IsVariable && claimClause.Terms[i].IsLiteral)
                {
                    binding[whenClause.Terms[i].Value] = claimClause.Terms[i].Value;
                }
            }
            // "With" options
            foreach(var option in whenClause.Options)
            {
                if(claimClause.Options.TryGetValue(option.Key, out var claimValue))
                {
                    binding[option.Key] = claimValue;
                }               
            }

            // Optional clauses 
            foreach (var optWhen in whenClause.OptionalClauses)
            {
                foreach (var optClaim in claimClause.OptionalClauses)
                    MakeBinding(optWhen, optClaim, ref binding);
            }
            return;
        }
        catch
        {
            return;
        }
    }
}

public class Database : ILoggingService
{
    private ConcurrentBag<string> errorLog = new ConcurrentBag<string>();
    public Dictionary<int, List<Claim>> claimByHash = new Dictionary<int, List<Claim>>();
    private Dictionary<int, When> whenByHash = new Dictionary<int, When>();
    private ConcurrentBag<When> whens = new ConcurrentBag<When>();
    private ConcurrentDictionary<int, When> whenHash = new ConcurrentDictionary<int, When>();
    private ConcurrentDictionary<int, Claim> claimHash = new ConcurrentDictionary<int, Claim>();
    private ConcurrentBag<Claim> claims = new ConcurrentBag<Claim>();
    public string LogString => logSb.ToString();
    private StringBuilder logSb = new StringBuilder();
    public ConcurrentBag<Tuple<ulong, ulong, string>> timelineEntries = new ConcurrentBag<Tuple<ulong, ulong, string>>();
    private string timelineEntriesText = string.Empty;
    static int count = 0;
    public int StatementCount { get { return claims.Count + whens.Count; } }
    public bool Debugging { get; set; } = false;    
    public void LogError(string error)
    {
        errorLog.Add(error);
    }
    public void LogErrors(IEnumerable<string> errors)
    {
        foreach(var error in errors)
        {
            errorLog.Add(error);
        }
    }
    public string[] GetErrors()
    {
        return errorLog.ToArray();
    }
    public void ClearErrors()
    {
        errorLog = new ConcurrentBag<string>();
    }
    
    bool ProcessWhen(When when)
    {
        var whenProcessor = new WhenProcessor(this, when);
        return  whenProcessor.Process();
    }
    public void AddClaimStatement(int Id, string a)
    {
        if(Claim.TryParse(Id, a, out var claim).Success)
        {
            AddStatement(claim);
        }
    }
    public When AddWhenStatement(int Id, string a, Action<Binding>? action)
    {
        if (When.TryParse(Id, a, action, out var when).Success)
        {
            AddStatement(when);
        }
        return when;
    }
    public void AddWishStatement(int Id, string a)
    {
       AddClaimStatement(Id, a);
    }
    public void AddStatement(Statement statement)
    {
       // LogTime($"\n\tClaims:\n\t {string.Join("\n\t", claims.Select(c => c))} \n\tWhens:\n\t {string.Join("\n\t", whens.Select(c => c))}\n");
        Log($"Adding: {statement}");
        if (statement is Claim claim)
        {
            if(claimHash.ContainsKey(claim.Hash))
            {
                Log($"Duplicate claim: {claim}");
                return;
            }
            else
            {
                claimHash[claim.Hash] = claim;
            }
            var keys = claim.MakeKeys();
          
            claims.Add(claim);
            claim.Ordinal = count++;
            foreach (var key in keys)
            {
                if(!claimByHash.ContainsKey(key))
                {
                    claimByHash[key] = new List<Claim>();
                }   
                claimByHash[key].Add(claim);
            }
            //check all whens for matches
            foreach (When when in whens.ToArray())
            {
                ProcessWhenOtherwise(when);           
            }
                          
        }
        else if(statement is When when)
        {
            if(whenHash.ContainsKey(when.Hash))
            {
                Log($"Duplicate when: {when}");
                return;
            }
            else
            {
                whenHash[when.Hash] = when;
            }

            whens.Add(when);

            ProcessWhenOtherwise(when);         
        }
        return;
    }
   
    void ProcessWhenOtherwise(When when)
    {
        if (!ProcessWhen(when))
        { 
            if (when.OtherwiseAction != null)
            {
                when.OtherwiseAction?.Invoke();
                when.OtherwiseAction = null;
                Log($"Match otherwise: When {when.Id} {when}");
            }
        }
    }
   
    public void Log(string name)
    {
        if(Debugging)
            logSb.AppendLine($"{name}");
    }
    void MakeTimeline()
    {
        if (Debugging)
        {
            var timeStart = timelineEntries.Any() ? timelineEntries.Min(e => e.Item1) - 1000 : 0;
            var sb = new StringBuilder();
            foreach (var entry in timelineEntries)
            {
                sb.AppendLine($"{(entry.Item1 - timeStart)}\t{entry.Item2}\t{entry.Item3}");
            }
            timelineEntriesText = sb.ToString();
        }
    }

    public void Clear()
    {
        count = 0;
        MakeTimeline();
        // set breakpoint on next line to inspect LogString and timelineEntriesText
        var log = LogString;
        whens.Clear();
        claims.Clear();
        claimByHash.Clear();
        whenByHash.Clear();
        whenHash.Clear();
        claimHash.Clear();
        Reference.ClearCache();
        Illumination.Clear();
        logSb.Clear();
        timelineEntries.Clear();
        timelineEntriesText = string.Empty;
        ServiceProvider.GetService<ILoggingService>()?.ClearErrors();
    }
}