using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

public static class RPNETCS {
    // after i convert this to a normal class (and not a command line program)
    // i will remove all static qualifiers

    static Object OB; // the current OBject being executed
    static Stack<Object> DS = new Stack<Object>(); // DataStack
    static Stack<List<Object>> RS = new Stack<List<Object>>(); // RunStream
    static Stack<int> STK = new Stack<int>(); // return STacK, basically the index of the next object to be executed
    static int IP = -1; // the instruction pointer for the topmost seco

    static Action _DoCol = (Action)DoCol; // just so i don't carry the cast around
    static Action _DoSemi = (Action)DoSemi;
    static Action _DoList = (Action)DoList;
    static Action _DoSymb = (Action)DoSymb; // same thing, actually
    static Action _Begin = (Action)Begin;
    static Action _Again = (Action)Again;
    static Action _Until = (Action)Until;
    static Action _MaybeQuit = (Action)MaybeQuit;
    static Action _Drop = () => DS.Pop();
    static Action _Dup = () => DS.Push(DS.Peek());
    static Action _Eval = () => Eval(DS.Pop());

    static void DoCol() {
        int startIndex = IP + 1; // ignore DoCol ..
        SkipOb();
        RS.Push(RS.Peek().GetRange(startIndex, IP - startIndex)); // ... but not DoSemi
        STK.Push(IP); // the current secondary continues after the object
        IP = 0; // the new secondary begins at the beginning
    }

    static void DoList() {
        // same deal as with DoCol the only thing that changes is that the object is pushed on the data stack instead 
        int startIndex = IP + 1; // keep it, before SkipOb rapes it
        SkipOb();
        DS.Push(RS.Peek().GetRange(startIndex, IP - startIndex - 1)); // ignore DoList AND DoSemi (it's a list)
    }

    static void SkipOb() {
        int depth = 0;
        int end = RS.Peek().Count - 1;
        do {
            OB = RS.Peek()[IP++]; // get object, move to next
            if (OB.Equals(_DoSemi)) {
                depth--;
            } else if (OB.Equals(_DoCol) || OB.Equals(_DoList) || OB.Equals(_DoSymb)) {
                depth++;
            }
        } while (IP < end && depth > 0);
        if (IP >= end || depth != 0) {
            throw new Exception("unmatched semi"); // sanity check
        }
    }

    static void DoSemi() {
        RS.Pop();
        IP = STK.Pop(); // no need to IP++ at the start since we're dropping the secondary anyway
    }

    static void DoTick() {
        int startIndex = IP + 1;
        SkipOb();
        if (IP - startIndex > 1) { // yep and this is where the Secondary Class becomes necessary
            // create a new object depending on what this one was
            // it is either a secondary, a symbolic or a list
            Object ob = RS.Peek()[startIndex];
            if (ob.Equals(_DoCol)) {
                // ignore the "prologue" keep the semi 
                DS.Push(new Secondary(RS.Peek().GetRange(startIndex, IP - startIndex)));
            } else if (ob.Equals(_DoSymb)) {
                // ignore the "prologue" keep the semi 
                DS.Push(new Symbolic(RS.Peek().GetRange(startIndex, IP - startIndex)));
            } else if (ob.Equals(_DoList)) {
                DS.Push(new List<Object>(RS.Peek().GetRange(startIndex + 1, IP - startIndex - 1)));
                // ignore "prologue" and semi
            } else {
                throw new Exception("unknown composite");
            }
        } else {
            DS.Push(RS.Peek()[startIndex]);
        }
    }

    static void Begin() {
        IP++; // Don't need to call SkipOb 
        STK.Push(IP);
    }
    static void Again() {
        IP = STK.Peek();
    }
    static void Until() {
        if ((bool)DS.Pop()) {
            STK.Pop();
            IP++;
        } else {
            IP = STK.Peek(); // "Again"
        }
    }

    static void Cola() {
        // IP++; // typical for any object, but since we'll be doing DoSemi, IP+1 below is enough
        // important 
        IP++; // if the next object is composite, you're fucked.
        // get the NEXT object in the current seco
        Object ob = RS.Peek()[IP];
        // pops the current seco
        DoSemi(); // changes IP.
        // and pushes the (popped) object so that the next object executed is this one
        RS.Peek().Insert(IP, ob);
    }
    // doubt we'll ever get here, but why not
    public static void DoSymb() { }

    static void MaybeQuit() {
        IP++;
        ConsoleKeyInfo k = Console.ReadKey();
        DS.Push(k.Key == ConsoleKey.Escape); // copout for now
    }

    public static void Main() {
        // "boot" process
        // :: BEGIN 1 :: 2 ; 3 ESC? UNTIL ;
        List<Object> outerLoop = new List<object> { _Begin, 1, _DoCol, 2, _DoSemi, 3, _MaybeQuit, _Until }; // outer loop has no semi. It's never popped from RS
        RS.Push(outerLoop);
        IP = 0;

        // inner loop
        while (IP < RS.Peek().Count) {
            OB = RS.Peek()[IP];
            Eval();
        }
    }

    static void Eval(Object ob = null) {
        // execute
        switch ((ob is null) ? OB : ob) {
            case Secondary sec: // insert it in the runstream
                RS.Peek().Insert(IP, _DoCol);
                RS.Peek().InsertRange(IP + 1, sec);
                //RS.Push(sec);
                //STK.Push(IP);
                //IP = 0;
                break;
            case Symbolic sym:
                DS.Push(sym);
                break;
            case Action act:
                act();
                break;
            default:
                IP++;
                DS.Push(OB);
                break;
        }
    }

    [DebuggerStepThrough]
    static String ToStr(Object ob) {
        switch (ob) {
            //case Secondary sec:
            //    String s = "::";
            //    for (int i= 0; i< sec.Count; i++) {
            //            s+= " " + ToStr(sec[i]);
            //    }
            //    return s;
            case Int32 i:
                return i.ToString();
            case Action act:
                return act.Method.Name;
            default:
                return "<" + ob.GetType().ToString() + ">";
        }
    }
    static Secondary StrTo(String src) {
        Secondary strto = new Secondary(new Object[] { });
        List<String> tokens = Split(src);
        Stack<Object> st = new Stack<object>();
        if (tokens.Count == 0) {
            return strto;
        }
        void AppendToTopLevel(object ob) {
            if (st.Count > 0) {
                if (st.Peek() is Secondary) {
                    ((Secondary)st.Peek()).Add(ob);
                } else if (st.Peek() is List<Object>) {
                    ((List<Object>)st.Peek()).Add(ob);
                }
            }
        }
        int pos = 0;
        st.Push(strto);
        Tok expect = Tok.none;
        try {
            while (pos < tokens.Count) {
                if (expect != Tok.none) {
                    dynamic o = null;
                    try {

                        if (expect == Tok.delim_cstring) {
                            o = tokens[pos];
                        } else if (expect == Tok.delim_identifier) {
                            o = new Identifier() { name = tokens[pos] };
                        } else {
                            o = Convert.ChangeType(tokens[pos], types[expect]);
                        }
                        if (o == null) {
                            throw new Exception();
                        }
                    } catch (Exception ex) {
                        throw new Exception("can not represent a " + types[expect].Name);
                    }
                    expect = Tok.none;
                } else {
                    Tok wi = WhatIs(tokens[pos]);
                    switch (wi) {
                        case Tok.CurlyOpen:
                            st.Push(new List<Object>());
                            break;
                        case Tok.CurlyClose:
                            if (st.Peek() is List<Object>) {
                                AppendToTopLevel(st.Pop());
                            } else {
                                throw new Exception("mismatched list");
                            }
                            break;
                        case Tok.SecondaryOpen:
                            st.Push(new Secondary(new Object[] { }));
                            break;
                        case Tok.SecondaryClose:
                            if (st.Peek() is Secondary) {
                                AppendToTopLevel(st.Pop());
                            } else {
                                throw new Exception("mismatched list");
                            }
                            break;
                        case Tok.BracketOpen:
                            break;
                        case Tok.BracketClose:
                            break;
                        case Tok.ParenOpen:
                            break;
                        case Tok.ParenClose:
                            break;
                        case Tok.word:
                            break;
                        case Tok.delim_bint:
                        case Tok.delim_single:
                        case Tok.delim_double:
                        case Tok.delim_cstring:
                        case Tok.delim_identifier:
                            expect = wi;
                            break;
                        case Tok.none:
                            throw new Exception("unknown term");
                    }
                }
                pos++;
            }
        } catch (Exception ex) {
            W(tokens[pos] + '(' + pos.ToString() + ')' + ex.Message);
            return null;
        }
        return strto;
    }
    static Dictionary<String, Object> words = new Dictionary<string, object> { { "m", "a" } };
    static Dictionary<String, Tok> delims = new Dictionary<string, Tok> { { "::", Tok.SecondaryOpen } };
    static Tok WhatIs(String s) {
        if (words.ContainsKey(s)) {
            return Tok.word;
        }
        if (delims.ContainsKey(s)) {
            return delims[s];
        }
        return Tok.none;
    }

    static Dictionary<Tok, Type> types = new Dictionary<Tok, Type> { { Tok.delim_bint, typeof(int) }, { Tok.delim_single, typeof(Single) }, { Tok.delim_double, typeof(Double) }, { Tok.delim_cstring, typeof(String) }, { Tok.delim_identifier, typeof(String) } };

    public static Action<String> W = (s) => Debug.WriteLine(s);
    class Identifier {
        public String name;
    }

    /*    not needed anymore but i keep it here just in case    */
    class Secondary : List<Object> {
        [DebuggerStepThrough]
        public Secondary(IEnumerable<Object> l) {
            base.AddRange(l);
        }
    }

    class Symbolic : List<object> {
        [DebuggerStepThrough]
        public Symbolic(IEnumerable<Object> l) {
            base.AddRange(l);
        }
    }

    static List<String> Split(String s) {
        List<String> split = new List<String>();
        String currentToken = "";
        Lex t = Lex.white;
        foreach (Char c in s) {
            switch (t) {
                case Lex.white:
                    if ('"' == c) {
                        t = Lex.cstri;
                        currentToken = "";
                    } else if ('`' == c) {
                        t = Lex.comme;
                    } else if (!Char.IsWhiteSpace(c)) {
                        t = Lex.token;
                        currentToken = c.ToString();
                    }
                    break;
                case Lex.token:
                    if (Char.IsWhiteSpace(c)) {
                        split.Add(currentToken);
                        currentToken = "";
                        t = Lex.white;
                    } else {
                        currentToken += c;
                    }
                    break;
                case Lex.cstri:
                    if ('\\' == c) {
                        t = Lex.escap;
                    } else if ('"' == c) {
                        split.Add(currentToken);
                        t = Lex.white;
                    } else {
                        currentToken += c;
                    }
                    break;
                case Lex.escap:
                    if (escapes.ContainsKey(c)) {
                        currentToken += escapes[c];
                        t = Lex.cstri;
                    } else {
                        throw new Exception("bad escape char '" + c + "'");
                    }
                    break;
                case Lex.comme:
                    if ('\n' == c || '\r' == c) {
                        t = Lex.white;
                    }
                    break;
            }
        }
        switch (t) {
            case Lex.cstri:
            case Lex.escap:
                throw new Exception("badly terminated string '" + currentToken + "'");
            case Lex.token:
                split.Add(currentToken);
                break;
        }
        return split;
    }


    static Dictionary<Char, Char> escapes = new Dictionary<char, char> { { '\\', '\\' }, { 'n', '\n' }, { 'r', '\r' }, { 't', '\t' }, { '"', '"' } };

    enum Tok {
        CurlyOpen,
        CurlyClose,
        SecondaryOpen,
        SecondaryClose,
        BracketOpen,
        BracketClose,
        ParenOpen,
        ParenClose,
        word,
        delim_bint,
        delim_single,
        delim_double,
        delim_cstring,
        delim_identifier,
        none
    }
    enum Lex {
        white,
        cstri,
        token,
        escap,
        comme
    }

}
