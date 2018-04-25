using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Dynamic;
public static class RPNETCS {
    // after i convert this to a normal class (and not a command line program)
    // i will remove all static qualifiers


    //// parts of the runtime
    // the current OBject being executed
    static Object OB; 

    // the Data Stack. It's where arguments are popped from, results pushed to and the user sees as general purpose I/O
    static Stack<Object> DS = new Stack<Object>(); 

    // The RunStream. The topmost list is the "current" secondary.
    static Stack<List<Object>> RS = new Stack<List<Object>>();

    // return STacK. This is where the deeper IPs are pushed
    static Stack<int> STK = new Stack<int>(); 

    // index into the current secondary (the one on the top of the runstream) 
    static int IP = 0;

    // this list is supposed to be 1) dynamic 2) the entire dictionary of available commands
    static Action _DoCol = (Action)DoCol; // just so i don't carry the cast around
    static Action _DoSemi = (Action)DoSemi;
    static Action _DoList = (Action)DoList;
    static Action _DoSymb = (Action)DoSymb; // same thing, actually
    static Action _Begin = (Action)Begin;
    static Action _Again = (Action)Again;
    static Action _Until = (Action)Until;
    static Action _MaybeQuit = (Action)MaybeQuit;
    static Action _Drop = () => DS.Pop(); // some trivial words 
    static Action _Dup = () => DS.Push(DS.Peek());
    static Action _Eval = () => Eval(DS.Pop());
    static Action _StrTo = () => Eval(StrTo((String)DS.Pop()));
    static Action _Input = () => DS.Push(Console.ReadLine());
    static Action _Print = () => Console.Write((String)DS.Pop());

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

    class Identifier {
        
        public String name;
        public static explicit operator Identifier(String s) {
            return new Identifier() { name = s };
        }
        // TODO: add constructor that checks for sane name values
    }

    // push a secondary to the return stack and make it the new current secondary.
    static void DoCol() {
        // at this point, IP points to this here object (DoCol, actually _DoCol)
        // we start by keeping the index of the next object right after ::
        int startIndex = IP + 1; 
        // make IP point just past this seco
        SkipOb();
        // now IP points to the object right after this secondary's matching semi (;)
        RS.Push(RS.Peek().GetRange(startIndex, IP - startIndex));
        // the current secondary will continue at this IP after the new secondary completes execution
        STK.Push(IP);
        // the new secondary begins at the beginning
        IP = 0; 
    }

    static void DoSemi() {
        // no need to IP++ at the start since we're dropping the secondary anyway

        // pop the current secondary from the runstream, making the inner secondary the current one
        RS.Pop();
        // restore the inner secondary's IP
        IP = STK.Pop(); 
    }

    // doubt we'll ever get here, but why not
    public static void DoSymb() { }

    static void DoList() {
        // same deal as with DoCol the only thing that changes is that the object is pushed on the data stack instead 
        int startIndex = IP + 1; // keep it, before SkipOb rapes it
        SkipOb();
        DS.Push(RS.Peek().GetRange(startIndex, IP - startIndex - 1)); // ignore DoList AND DoSemi (it's a list)
    }

    static void SkipOb() {
        // this one iterates over objects, increasing the depth for each prologue that starts a composite
        // and decreasing it for every semi
        //        1 :: 2 2 :: 2 2 ; 3 3 { 4 4 { 5 :: 6 ; } :: 7 ; } ; 
        // depth: 0 1  1 1 2  2 2 1 1 1 2 2 2 3 3 4  4 3 2 3  3 2 1 0
        // so in the above case, only the initial '1' (which makes it the "current object") will be skipped

        int depth = 0;

        // don't go past the end of the current secondary
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

    // this pushes the next object to the data stack and skips over it in the runstream.
    // in other words, instead of executing it, it pushes it on the stack.
    // that way the program becomes data
    static void DoQuote() {
        // first find what this object is
        int startIndex = IP + 1;
        // SkipOb takes care of skipping over any kind of object (composite or atomic)
        SkipOb();

        // if we skipped over more than one IP it means we're pushing a composite
        if (IP - startIndex > 1) { 
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

    // this marks the beginning of a loop
    static void Begin() {
        IP++; // Don't need to call SkipOb, we know _Begin is atomic
        STK.Push(IP);
    }

    // this marks the end of an infinite (not indefinite) loop
    // currently there is no way to exit this kind of loop.
    // it would require a way to directly pop the STK
    static void Again() {
        IP = STK.Peek();
    }

    // pops a bool off of the data stack and does Again if it is false
    // so
    // #0 begin dup #1 + dup #10 == until
    // pushes #0 to #10 on the data stack
    static void Until() {
        if ((bool)DS.Pop()) {
            STK.Pop();
            IP++;
        } else {
            IP = STK.Peek(); // "Again"
        }
    }

    // this is an interesting word
    // removes the next object from the runstream
    // pops the runstream
    // and inserts the object in the inner secondary at the position of its IP
    // its purpose is improving tail recursion efficiency
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

    static void MaybeQuit() {
        IP++;
        ConsoleKeyInfo k = Console.ReadKey();
        DS.Push(k.Key == ConsoleKey.Escape); // copout for now
    }

    public static void Main() {
        // "boot" process
        // :: begin 1 :: 2 ; 3 maybequit until ;
        //while (true) {
        //    String s = Console.ReadLine();
        //    if (s == "q") {
        //        break;
        //    }
        //    Type t = Type.GetType(s, false, true);
        //    Console.WriteLine(t != null ? t.Name : "null");
        //}
        String str = ":: # 1 % 1.2 %% 1.23 $ \"123\\n\" id haha <system.windows.forms.form> ;";
        List<String> terms = Split(str);
        //terms.ForEach(term => Console.WriteLine("'" + term + "'"));
        Secondary parsed = StrTo(str);

        Console.ReadKey();
        //List<Object> outerLoop = new List<object> { _Begin,  _MaybeQuit, _Until }; // outer loop has no semi. It's never popped from RS
        //RS.Push(outerLoop);
        //IP = 0;
        //
        //// inner loop
        //// just keep executing objects one after the other
        //while (IP < RS.Peek().Count) {
        //    OB = RS.Peek()[IP];
        //    Eval();
        //}
    }

    // when the system becomes self-sufficient (self-contained?) , this is going to be written in RPL
    // and thus be extendable at runtime
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
                // normally, each object is responsible for adjusting the IP, but since for now we push
                // them ourselves, we adjust it directly
                IP++;
                DS.Push(OB);
                break;
        }
    }

    [DebuggerStepThrough]
    static String ToStr(Object ob) {

        if (type_specifier.ContainsValue(ob.GetType())) {
            return type_specifier.FirstOrDefault(x => x.Value == ob.GetType()).Key + " " + ob.ToString();
        } else switch (ob) {
            // there are three "special" cases, one for each type of composite
            // i haven't done the case for symbolics yet
            case Secondary sec:
                String s = "::";
                for (int i= 0; i< sec.Count; i++) {
                        s+= " " + ToStr(sec[i]);
                }
                return s;
            case Action act:
                return act.Method.Name;
            default:
                return "<" + ob.GetType().FullName + ">";
        }
    }

    //// parts of the parser/compiler
    static Dictionary<String, Type> type_specifier = new Dictionary<string, Type> {
        { "#", typeof(System.Int32) }, // #123 or # 123 or maybe even #b #s #i #l to indicate byte, short, integer, long
        { "%", typeof(System.Single) }, // %123 or % 123
        { "%%", typeof(System.Double) },
        { "id", typeof(Identifier) },
        { "$", typeof(System.String) },
    };

    static Dictionary<String, Object> words = new Dictionary<string, Object>  {
        { "::", _DoCol },
        { ";", _DoSemi },
        { "{", _DoList },
        { "}", _DoSemi }, // yes, it has to be this way, this marks the end of the list
        { "begin", _Begin },
        { "again", _Again },
        { "until", _Until },
        { "maybequit", _MaybeQuit },
        { "drop", _Drop },
        { "dup", _Dup },
        { "eval", _Eval },
        { "'", (Action)DoQuote },
    };

    // for escaping characters in a string
    static Dictionary<Char, Char> escapes = new Dictionary<char, char> {
        //from  to
        // \\    \
        { '\\', '\\' },
        // \n  newline
        { 'n', '\n' },
        // \r  carriage return
        { 'r', '\r' },
        // \t  horizontal tab
        { 't', '\t' },
        // \"  doublequote
        { '"', '"' },
    };

    enum Estate {
        whitespace,
        cstring,
        token,
        escape,
        comment,
    }


    // the syntax is pretty simple
    // a type descript and a sequence of characters like
    // # 123 ("System.Int32")
    // % 1.23 ("System.Single")
    // %% 1.23456 ("System.Double")
    // id foo (identifier)
    // $ "character string" OR you can omit doublequotes if there are no spaces
    // and no escapes $ fooba\rbaz ("System.String") does NOT have a linefeed character in it
    // <type> characters (whatever type is)
    // so upon encountering a #, %,%%, id, $, OR a <type>, the parser knows how to interpreted the next token
    // and in the case of a string, how to parse it
    // should cause the appropriate object to be generated and 
    // inserted into the secondary under construction
    // i know how i implement it seems far from elegant
    // but it allows for great flexibility
    // i might add typed arrays as follows:
    // arrays are typed the same but the literal is like [ characters, ... ]
    // so # [ 1,2,3] is an array of integers
    // <date> [ 1/1/2010, 1/1/2011] should be an array of Date etc
    
    // as is right now, the output is a secondary which will be inserted into the runstream
    // it should actually process what kind of object it is being generated
    // so that whatever object is described in it, will be pushed on the stack
    // instead of being executed.
    // also, arguments like " # 1 # 2 " which describe *two* objects should be an error condition
    // this will be different from how the command line will be treated.
    // the command line will implicitly be a secondary, that is, the command line will be implicitly prepended with ":: "
    // appended with " ;", parsed and evaluated.
    // this is different from the original behavior of STR->, which is more or less equivalent to entering its argument on the command line.

    static Secondary StrTo(String str) {
        List<Object> tokens = new List<object>();
        Type expect = null;
        foreach (String term in Split(str)) 
            if (expect != null) {
                dynamic ob;
                tokens.Add((term, expect));
                expect = null;
            } else if (type_specifier.TryGetValue(term, out expect)) {
                expect = type_specifier[term];
            } else if ('<' == term[0] && term.EndsWith(">")) {
                String typename = term.Substring(1, term.Length - 2);
                Type t = Type.GetType(typename, false, true);
                if (t != null) {
                    tokens.Add(t);
                } else {
                    throw new Exception("can't find Type for '" + typename);
                }
            } else {
                if (words.TryGetValue(term, out object ob)) {
                    tokens.Add(ob);
                } else {
                    throw new Exception("unknown term '" + term + "'");
                }
            }
        return new Secondary(tokens);
    }

    static List<String> Split(string str) {
        List<String> Split = new List<string>();
        Estate state = Estate.whitespace;
        String token = "";
        foreach (Char c in str) {
            switch (state) {
                case Estate.whitespace:
                    if ( '"' == c) {
                        state = Estate.cstring;
                        token = "";
                    } else if ('`' == c) {
                        state = Estate.comment;
                    } else if (!Char.IsWhiteSpace(c)) {
                        state = Estate.token;
                        token = c.ToString();
                    }
                    break;
                case Estate.cstring:
                    if ('\\' == c) {
                        state = Estate.escape;
                    } else if ('"' == c) {
                        state = Estate.whitespace;
                        Split.Add(token);
                    } else {
                        token += c;
                    }
                    break;
                case Estate.token:
                    if (Char.IsWhiteSpace(c)) {
                        state = Estate.whitespace;
                        Split.Add(token);
                    } else {
                        token += c;
                    }
                    break;
                case Estate.escape:
                    if (escapes.ContainsKey(c)) {
                        state = Estate.cstring;
                        token += escapes[c];
                    } else {
                        throw new Exception("bad escape char '" + c + "'");
                    }
                    break;
                case Estate.comment:
                    if ('\r' == c || '\n' == c) {
                        state = Estate.whitespace;
                    }
                    break;
            }
        }
        switch (state) {
            case Estate.comment:
            case Estate.whitespace:
                //all is ok
                break;
            case Estate.token:
                Split.Add(token);
                break;
            default:
                throw new Exception("expecting " + state.ToString() + ", not '" + token + "'");
        }
        return Split;

    }



    // a delegate that can be overwritten so that the calling code can set it to whatever
    public static Action<String> W = (s) => Debug.WriteLine(s);

}
