Imports System.Reflection
Imports System.Diagnostics
Imports System.Text

Module RPNETVB
    ''parts of the runtime

    'the current OBject being executed
    Private _OB As Object

    ' the Data Stack. It's where arguments are popped from, results pushed to 
    'And the user sees as general purpose I/O
    Private _DS As New StackList(Of Object)

    ' a list with push and pop in it
    Class StackList(Of T)
        Inherits List(Of T)
        Public Function Pop() As T
            Pop = MyBase.Item(0)
            MyBase.RemoveAt(0)
        End Function
        Public Function Peek() As T
            Peek = MyBase.Item(0)
        End Function
        Public Sub Push(v As T)
            MyBase.Insert(0, v)
        End Sub
    End Class

    'the "current" secondary. Out of the stack mainly for convenience
    Private _RS As Secondary

    ' The RunStream. This is where the deeper secondaries are pushed
    Private _RSSTK As New StackList(Of List(Of Object))

    ' return STacK. This is where the deeper IPs are pushed
    Private _IPSTK As New StackList(Of Integer)

    ' index into the current secondary (the one on the top of the runstream) 
    Private _IP As Integer = 0

    ' for definite loops
    ' start, end, IP and index
    ' should find another way to implement this
    Private _LOOPSTK As New StackList(Of Integer)
    Private _LOOPstart, _LOOPend, _LOOPindex, _LOOPip As Integer

    ' for local variables, yes it is inefficient
    Private _LOCALSTK As New Stack(Of Dictionary(Of String, Object))
    Private _LOCAL As New Dictionary(Of String, Object)

    ' this list is supposed to be 1) dynamic 2) the entire dictionary of available commands
    ' handled with attributes, but some are still needed 
    Private _DoCol As Action = AddressOf DoCol ' just so i don't carry the cast around
    Private _DoSemi As Action = AddressOf DoSemi
    Private _DoList As Action = AddressOf DoList
    Private _DoSym As Action = AddressOf DoSymb ' same thing, actually

    <AttributeUsage(AttributeTargets.Method, Inherited:=False, AllowMultiple:=True)>
    Class RPLWord
        Inherits Attribute
        Public WordName As String
        Public Sub New(Optional name As String = "")
            WordName = name
        End Sub
    End Class

    <RPLWord> Sub dup()
        _IP += 1
        _DS.Push(_DS.Peek())
    End Sub

    <RPLWord> Sub read()
        _IP += 1
        _DS.Push(Console.ReadLine())
    End Sub

    <RPLWord> Sub print()
        _IP += 1
        Console.WriteLine(_DS.Pop().ToString)
    End Sub

    <RPLWord> Sub drop()
        _IP += 1
        _DS.Pop()
    End Sub


    Class Secondary
        Inherits List(Of Object)
        <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
            If l IsNot Nothing Then MyBase.AddRange(l)
        End Sub
    End Class

    Class Symbolic
        Inherits List(Of Object)
        <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
            If l IsNot Nothing Then MyBase.AddRange(l)
        End Sub
    End Class

    Class ObList
        Inherits List(Of Object)
        <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
            If l IsNot Nothing Then MyBase.AddRange(l)
        End Sub
    End Class

    Class Identifier
        Public name As String
        Public Shared Widening Operator CType(s As String) As Identifier
            Return New Identifier With {.name = s}
        End Operator
        ' TODO: add constructor that checks for sane name values
    End Class

    Class Lambda
        Public name As String
        Public Shared Widening Operator CType(s As String) As Lambda
            Return New Lambda With {.name = s}
        End Operator
        ' TODO: add constructor that checks for sane name values
    End Class

    <RPLWord("::")>
    Sub DoCol()
        ' at this point, IP points to this here object (DoCol, actually _DoCol)
        ' we start by keeping the index of the next object right after ::
        Dim startIndex As Integer = _IP + 1
        ' make _IP point just past this seco
        SkipOb()
        ' now _IP points to the object right after this secondary's matching semi (;)
        _RSSTK.Push(_RS.GetRange(startIndex, _IP - startIndex))
        ' the current secondary will continue at this _IP after the new secondary completes execution
        _IPSTK.Push(_IP)
        ' the new secondary begins at the beginning
        _IP = 0
    End Sub

    <RPLWord(";")>
    Sub DoSemi()
        ' no need to IP++ at the start since we're replacing the current secondary anyway
        ' pop the top secondary from the runstream, and make it the current one
        _RS = _RSSTK.Pop()
        ' restore the inner secondary's IP
        _IP = _IPSTK.Pop()
    End Sub

    'doubt we'll ever get here, but why not
    <RPLWord("sym")>
    Sub DoSymb()
    End Sub

    <RPLWord("{")>
    Sub DoList()
        'same deal as with DoCol the only thing that changes is that the object is pushed on the data stack instead 
        Dim startIndex As Integer = _IP + 1 'keep it, before SkipOb rapes it
        SkipOb()
        _DS.Push(_RS.GetRange(startIndex, _IP - startIndex)) 'ignore DoList AND DoSemi (it's a list)
    End Sub

    Sub SkipOb()
        ' this one iterates over objects, increasing the depth for each prologue that starts a composite
        ' And decreasing it for every semi
        '        1 : 2 2 : 2 2 ; 3 3 { 4 4 { 5 : 6 ; } : 7 ; } ; 
        ' depth: 0 1  1 1 2  2 2 1 1 1 2 2 2 3 3 4  4 3 2 3  3 2 1 0
        ' so in the above case, only the initial '1' (which makes it the "current object") will be skipped

        Dim depth As Integer = 0
        Dim endIndex As Integer = _RS.Count - 1
        Do
            _OB = _RS(_IP)
            _IP += 1
            If _OB.Equals(_DoSemi) Then
                depth -= 1
            ElseIf _OB.Equals(_DoCol) OrElse _OB.Equals(_DoList) OrElse _OB.Equals(_DoSym) Then
                depth += 1
            End If
        Loop While _IP < endIndex AndAlso depth > 0
        If _IP >= endIndex OrElse depth <> 0 Then Throw New Exception("unmatched semi")
    End Sub

    ' this pushes the next object to the data stack And skips over it in the runstream.
    ' in other words, instead of executing it, it pushes it on the stack.
    ' that way the program becomes data
    <RPLWord("'")> Sub DoQuote()
        ' go past self
        _IP += 1
        ' first find what this object is
        Dim startIndex As Integer = _IP
        ' SkipOb takes care of skipping over any kind of object (composite or atomic)
        SkipOb()
        If _IP - startIndex > 1 Then
            ' if we skipped over more than one IP it means we skipped over a composite
            ' which we will push on the DS

            Dim ob As Object = _RS(startIndex)
            If ob.Equals(_DoCol) Then
                ' ignore the "prologue" keep the semi 
                _DS.Push(New Secondary(_RS.GetRange(startIndex + 1, _IP - startIndex - 1)))
            ElseIf ob.Equals(_DoSym) Then
                ' ignore the "prologue" keep the semi 
                _DS.Push(New Symbolic(_RS.GetRange(startIndex + 1, _IP - startIndex - 1)))
            ElseIf ob.Equals(_DoList) Then
                ' ignore "prologue" and semi
                _DS.Push(New ObList(_RS.GetRange(startIndex + 1, _IP - startIndex - 2)))
            Else
                Throw New Exception("unknown composite")
            End If
        Else
            ' push atomic object
            _DS.Push(_RS(startIndex))
        End If
    End Sub

    ' this marks the beginning of a loop
    <RPLWord> Sub begin()
        _IP += 1
        _IPSTK.Push(_IP)
    End Sub

    ' this marks the end of an infinite (not indefinite) loop
    ' currently there is no way to exit this kind of loop.
    ' it would require a way to directly pop the STK
    <RPLWord> Sub again()
        _IP = _IPSTK.Peek
    End Sub

    ' pops a bool off of the data stack and does Again if it is false
    ' so
    ' #0 begin dup #1 + dup #10 == until
    ' pushes #0 to #10 on the data stack
    <RPLWord("until")> Sub _Until()
        If _DS.Pop Then
            _IPSTK.Pop()
            _IP += 1
        Else
            _IP = _IPSTK.Peek
        End If
    End Sub

    ' this is an interesting word
    ' removes the next object from the runstream
    ' pops the runstream
    ' and inserts the object in the inner secondary at the position of its IP
    ' its purpose is improving tail recursion efficiency
    <RPLWord> Sub cola()
        _IP += 1
        Dim ob As Object = _RS(_IP)
        DoSemi()
        _RS.Insert(_IP, ob)
    End Sub

    <RPLWord("quit?")> Sub quitq()
        If _DS.Pop Then _IP = _RS.Count Else _IP += 1
        ' yes, a total cop-out
    End Sub

    <RPLWord("not")> Sub _not()
        _IP += 1
        _DS.Push(Not _DS.Pop)
    End Sub

    <RPLWord("==")> Sub eq()
        _IP += 1
        _DS.Push(_DS.Pop = _DS.Pop)
    End Sub

    <RPLWord("if")> Sub _if()
        If _DS(1) Then
            _DS.RemoveAt(1)
            rpleval()
        Else
            _IP += 1
            _DS.RemoveRange(0, 2)
        End If
    End Sub

    Sub Main()
        ' fill the words Dictionary
        Dim nt As Long = Now.Ticks
        Dim asRPLWord As RPLWord
        For Each mi As MethodInfo In GetType(RPNETVB).GetMethods
            asRPLWord = mi.GetCustomAttribute(GetType(RPLWord))
            If asRPLWord IsNot Nothing Then
                words.Add(If(asRPLWord.WordName.Length = 0, mi.Name, asRPLWord.WordName), mi.CreateDelegate(GetType(Action)))
            End If
        Next
        Debug.WriteLine(Now.Ticks - nt)
        Debug.WriteLine("done")
        ' trivial loop
        ' echoes back what is written until nothing is entered
        '"begin read parse eval depth # 0 == ' :: $ ""stack empty"" print ; ' :: depth # 0 swap do depth ?i - pick print loop ; ifte again"
        Dim str As String = "begin read parse eval depth # 0 == not ' :: # 1 depth do ?i pick tostr ?i tostr $ "" "" + swap + print loop ; if again "
        _RS = StrTo(str)
        While _IP < _RS.Count
            _OB = _RS(_IP)
            W("==== current object before Eval() ====")
            W(tostr(_OB))
            Eval()
            W("==== current object after Eval() ====")
            W(tostr(_OB))

            W("==== data stack ====")
            If _DS.Count > 0 Then
                For i As Integer = _DS.Count - 1 To 0 Step -1
                    W(i.ToString.PadLeft(4) & ": " & tostr(_DS(i)))
                Next
            Else
                W("   0:")
            End If

            W("==== runstream ====")
            For i As Integer = 0 To _RS.Count - 1 ' we can assume it's not empty
                W(i.ToString.PadLeft(4) & ": " & tostr(_RS(i)) & If(_IP = i, " <---", ""))
            Next

            W("==== return stack ====")
            If _RSSTK.Count > 0 Then
                For l As Integer = 0 To _RSSTK.Count - 1
                    W("-- level " & l)
                    Dim lst As List(Of Object) = _RSSTK(l)
                    For i As Integer = 0 To lst.Count - 1
                        W(i.ToString.PadLeft(4) & ": " & tostr(lst(i)) & If(i = _IPSTK(l), " <---", ""))
                    Next
                Next
            Else
                W("(empty)")
            End If

        End While
        Console.ReadLine()
    End Sub

    <RPLWord> Sub ticks()
        _IP += 1
        _DS.Push(Now.Ticks)
    End Sub

    Private tAction As Type = GetType(Action)

    <RPLWord("eval")> Sub rpleval()
        _IP += 1 ' (standard)
        _OB = _DS.Peek
        If TypeOf _OB Is Secondary Then
            _DS.Pop()
            _RSSTK.Push(_RS)
            _IPSTK.Push(_IP)
            _RS = _OB
            _IP = 0 ' explicit prolog
        ElseIf TypeOf _OB Is Action Then
            _DS.Pop()
            DirectCast(_OB, Action)()
        End If ' else do nothing
    End Sub

    Sub Eval()
        If TypeOf _OB Is Action Then
            DirectCast(_OB, Action)()
        Else
            _DS.Push(_OB)
            _IP += 1
        End If
    End Sub

    ' the syntax is pretty simple
    ' a type descript and a sequence of characters like
    ' # 123 ("System.Int32")
    ' % 1.23 ("System.Single")
    ' %% 1.23456 ("System.Double")
    ' id foo (identifier)
    ' $ "character string" OR you can omit doublequotes if there are no spaces
    ' and no escapes $ fooba\rbaz ("System.String") does NOT have a linefeed character in it
    ' <type> characters (whatever type is)
    ' so upon encountering a #, %,%%, id, $, OR a <type>, the parser knows how to interpreted the next token
    ' and in the case of a string, how to parse it
    ' should cause the appropriate object to be generated and
    ' inserted into the secondary under construction
    ' i know how i implement it seems far from elegant
    ' but it allows for great flexibility
    ' i might add typed arrays as follows:
    ' arrays are typed the same but the literal is like [ characters, ... ]
    ' so # [ 1,2,3] is an array of integers
    ' <date> [ 1/1/2010, 1/1/2011] should be an array of Date etc

    ' as is right now, the output is a secondary which will be inserted into the runstream
    ' it *should* actually process what kind of object it is being generated
    ' so that whatever object is described in it, will be pushed on the stack
    ' instead of being executed.
    ' also, arguments like " # 1 # 2 " which describe *two* objects should be an error condition
    ' this will be different from how the command line will be treated.
    ' the command line will implicitly be a secondary, that is, the command line will be implicitly prepended with ":: "
    ' appended with " ;", parsed and evaluated.
    ' this is different from the original behavior of STR->, which is more or less equivalent to entering its argument on the command line.

    Dim escapes As New Dictionary(Of Char, Char) From {{"\"c, "\"c}, {"n"c, Chr(10)}, {"r"c, Chr(13)}, {"t"c, Chr(9)}, {""""c, """"c}}

    Dim type_specifier As New Dictionary(Of String, Type) From {
         {"#", GetType(Integer)}, ' #123 or # 123 or maybe even #b #s #i #l to indicate byte, short, integer, long
         {"%", GetType(Single)}, ' %123 or % 123
         {"%%", GetType(Double)},
         {"id", GetType(Identifier)},
         {"$", GetType(String)},
         {"lam", GetType(Lambda)}
    }

    ' this will be filled at initialization from the <RplWord> methods in this class
    Dim words As New Dictionary(Of String, Object) From {
         {"}", _DoSemi}', ' yes, it has to be this way, this marks the end of the list
    }
    '{"::", _DoCol},
    '{";", _DoSemi},
    '{"{", _DoList},
    '{"sym", _DoSym}

    <RPLWord("+")> Sub plus()
        _IP += 1
        _DS.Push(_DS.Pop + _DS.Pop)
    End Sub

    <RPLWord("-")> Sub minus()
        _IP += 1
        Dim l1 As Object = _DS.Pop
        Dim l2 As Object = _DS.Pop
        _DS.Push(l2 - l1)
    End Sub

    <RPLWord> Sub parse()
        _IP += 1
        Dim ob As Object = StrTo(_DS.Pop & " ;")  ' implied prolog
        _DS.Push(ob)
    End Sub

    <RPLWord> Sub depth()
        _IP += 1
        _DS.Push(_DS.Count)
    End Sub

    <RPLWord> Sub swap() ' something as trivial as this takes so much time. it's just two pointers. 
        _IP += 1
        Dim l2 As Object = _DS(1)
        _DS.RemoveAt(1)
        _DS.Push(l2)
    End Sub

    <RPLWord("!")> Sub PropertySet()
        _IP += 1
        Dim propertyName As String = _DS.Pop
        Dim propertyValue As Object = _DS.Pop
        CallByName(_DS.Pop, propertyName, CallType.Set, propertyValue)
    End Sub

    <RPLWord("?")> Sub PropertyGet()
        _IP += 1
        Dim propertyName As String = _DS.Pop
        _DS.Push(CallByName(_DS.Pop, propertyName, CallType.Get))
    End Sub

    <RPLWord("@")> Sub MethodCall()
        _IP += 1
        Dim methodName As String = _DS.Pop
        Dim args() As Object = DirectCast(_DS.Pop, ObList).ToArray
        _DS.Push(CallByName(_DS.Pop, methodName, CallType.Method, args))
    End Sub

    <RPLWord> Sub def()
        _IP += 1
        Dim wordName As String = _DS.Pop
        words.Add(wordName, _DS.Pop)
    End Sub

    <RPLWord> Sub pick()
        _IP += 1
        _DS.Push(_DS(_DS.Pop - 1))
    End Sub

    ' pushes the topmost loop index on the stack
    <RPLWord("?i")> Sub indx()
        _IP += 1
        _DS.Push(_LOOPindex)
    End Sub

    ' pushes an inner loop index on the stack
    <RPLWord("?n")> Sub indxn()
        _IP += 1
        Dim i As Integer = _DS.Pop
        _DS.Push(If(i = 0, _LOOPindex, _LOOPSTK(4 * (i - 1))))
    End Sub

    <RPLWord("do")> Sub loopbegin()
        _IP += 1

        _LOOPSTK.Push(_LOOPstart) ' save current loop data
        _LOOPSTK.Push(_LOOPend) ' save current loop data
        _LOOPSTK.Push(_LOOPip) ' save current loop data
        _LOOPSTK.Push(_LOOPindex) ' save current loop data
        _LOOPend = _DS.Pop
        _LOOPstart = _DS.Pop
        _LOOPindex = _LOOPstart
        _LOOPip = _IP
    End Sub

    ' should optimize this later on
    <RPLWord("loop")> Sub loopend()
        If _LOOPindex < _LOOPend Then
            _LOOPindex += 1
            _IP = _LOOPip
        Else
            _IP += 1
            _LOOPindex = _LOOPSTK.Pop
            _LOOPip = _LOOPSTK.Pop
            _LOOPend = _LOOPSTK.Pop
            _LOOPstart = _LOOPSTK.Pop
        End If
    End Sub

    Function StrTo(str As String) As Secondary
        Dim tokens As New List(Of Object)
        Dim expect As Type = Nothing
        For Each term As String In Split(str)
            If expect IsNot Nothing Then
                tokens.Add(CTypeDynamic(term, expect))
                expect = Nothing
            ElseIf type_specifier.TryGetValue(term, expect) Then ' if it succeeds i don't have to do anything else
            ElseIf term(0) = "<"c AndAlso term.EndsWith(">") Then
                Dim TypeName As String = term.Substring(1, term.Length - 2)
                Dim t As Type = Type.GetType(TypeName, False, True)
                If t IsNot Nothing Then tokens.Add(t) Else Throw New Exception("can't find Type for '" & TypeName)
            Else
                Dim ob As Object = Nothing
                If words.TryGetValue(term, ob) Then tokens.Add(ob) Else Throw New Exception("unknown term '" + term + "'")
            End If
        Next
        Return New Secondary(tokens)
    End Function

    Enum LexerState
        token
        whitespace
        escape
        cstring
        comment
    End Enum

    Function Split(str As String) As List(Of String)
        Split = New List(Of String)
        Dim state As LexerState = LexerState.whitespace
        Dim token As String = ""
        For Each c As Char In str
            Select Case state
                Case LexerState.whitespace
                    If """"c = c Then
                        state = LexerState.cstring
                        token = ""
                    ElseIf "`"c = c Then
                        state = LexerState.comment
                    ElseIf Not Char.IsWhiteSpace(c) Then
                        state = LexerState.token
                        token = c.ToString
                    End If
                Case LexerState.token
                    If Char.IsWhiteSpace(c) Then
                        Split.Add(token)
                        token = ""
                        state = LexerState.whitespace
                    Else
                        token &= c
                    End If
                Case LexerState.cstring
                    If "\"c = c Then
                        state = LexerState.escape
                    ElseIf """"c = c Then
                        Split.Add(token)
                        state = LexerState.whitespace
                    Else
                        token &= c
                    End If
                Case LexerState.escape
                    If escapes.ContainsKey(c) Then
                        token &= escapes(c)
                        state = LexerState.cstring
                    Else
                        Throw New Exception("bad escape char '" & c & "'")
                    End If
                Case LexerState.comment
                    If Chr(10) = c OrElse Chr(13) = c Then
                        state = LexerState.whitespace
                    End If
            End Select
        Next
        Select Case state
            Case LexerState.comment ' all is ok
            Case LexerState.whitespace

            Case LexerState.token
                Split.Add(token)
            Case Else
                Throw New Exception("expecting " & state.ToString() & ", not '" & token & "'")
        End Select
    End Function

    <RPLWord("tostr")> Sub _tostr()
        _IP += 1
        _DS.Push(tostr(_DS.Pop))
    End Sub

    Function tostr(ob As Object) As String
        Dim ty As Type = ob.GetType
        Dim ts As String = ""
        For Each k As String In type_specifier.Keys
            If type_specifier(k) = ty Then
                ts = k
                Exit For
            End If
        Next
        If ts.Length > 0 Then Return ts & " " & ob.ToString

        If ty = GetType(Action) Then
            Dim act As Action = DirectCast(ob, Action)
            If act.Method IsNot Nothing Then
                Dim mi As MethodInfo = act.Method
                Dim asRPLWord As RPLWord
                asRPLWord = act.Method.GetCustomAttribute(GetType(RPLWord))
                If asRPLWord IsNot Nothing Then Return If(asRPLWord.WordName.Length = 0, mi.Name, asRPLWord.WordName)
            End If
            Return act.ToString
        End If

        If ty = GetType(Type) Then Return "<" & DirectCast(ob, Type).FullName & ">"
        If ty = GetType(Secondary) Then Return String.Join(" ", DirectCast(ob, Secondary).ConvertAll(Function(o) tostr(o)))

        Return ob.ToString & ", <" & ty.FullName & ">"
    End Function


    ' a delegate that can be overwritten so that the calling code can set it to whatever
    Public W As Action(Of String) = Sub(s) Debug.WriteLine(s)

End Module
