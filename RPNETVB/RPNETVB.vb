Imports System.Reflection
Imports System.Diagnostics
Imports System.Text

Module RPNETVB
    ''parts of the runtime

    'the current OBject being executed
    Private _OB As Object

    ' the Data Stack. It's where arguments are popped from, results pushed to and the user sees as general purpose I/O
    Private _DS As New Stack(Of Object)

    'the "current" secondary. Out of the stack mainly for convenience
    Private _SECO As Secondary

    ' The RunStream. This is where the deeper secondaries are pushed
    Private _RS As New Stack(Of List(Of Object))

    ' return STacK. This is where the deeper IPs are pushed
    Private _STK As New Stack(Of Integer)

    ' index into the current secondary (the one on the top of the runstream) 
    Private _IP As Integer = 0

    ' this list is supposed to be 1) dynamic 2) the entire dictionary of available commands
    ' handled with attributes, but some are still needed 
    Private _DoCol As Action = AddressOf DoCol ' just so i don't carry the cast around
    Private _DoSemi As Action = AddressOf DoSemi
    Private _DoList As Action = AddressOf DoList
    Private _DoSymb As Action = AddressOf DoSymb ' same thing, actually

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

    <RPLWord("::")> Sub DoCol()
        ' at this point, IP points to this here object (DoCol, actually _DoCol)
        ' we start by keeping the index of the next object right after ::
        Dim startIndex As Integer = _IP + 1
        ' make _IP point just past this seco
        SkipOb()
        ' now _IP points to the object right after this secondary's matching semi (;)
        _RS.Push(_SECO.GetRange(startIndex, _IP - startIndex))
        ' the current secondary will continue at this _IP after the new secondary completes execution
        _STK.Push(_IP)
        ' the new secondary begins at the beginning
        _IP = 0
    End Sub

    <RPLWord(";")> Sub DoSemi()
        ' no need to IP++ at the start since we're replacing the current secondary anyway
        ' pop the top secondary from the runstream, and make it the current one
        _SECO = _RS.Pop()
        ' restore the inner secondary's IP
        _IP = _STK.Pop
    End Sub

    'doubt we'll ever get here, but why not
    <RPLWord("sym")> Sub DoSymb()
    End Sub

    <RPLWord("{")> Sub DoList()
        'same deal as with DoCol the only thing that changes is that the object is pushed on the data stack instead 
        Dim startIndex As Integer = _IP + 1 'keep it, before SkipOb rapes it
        SkipOb()
        _DS.Push(_SECO.GetRange(startIndex, _IP - startIndex)) 'ignore DoList AND DoSemi (it's a list)
    End Sub

    Sub SkipOb()
        ' this one iterates over objects, increasing the depth for each prologue that starts a composite
        ' And decreasing it for every semi
        '        1 : 2 2 : 2 2 ; 3 3 { 4 4 { 5 : 6 ; } : 7 ; } ; 
        ' depth: 0 1  1 1 2  2 2 1 1 1 2 2 2 3 3 4  4 3 2 3  3 2 1 0
        ' so in the above case, only the initial '1' (which makes it the "current object") will be skipped

        Dim depth As Integer = 0
        Dim endIndex As Integer = _SECO.Count - 1
        Do
            _OB = _SECO(_IP)
            _IP += 1
            If _OB.Equals(_DoSemi) Then
                depth -= 1
            ElseIf _OB.Equals(_DoCol) OrElse _OB.Equals(_DoList) OrElse _OB.Equals(_DoSymb) Then
                depth += 1
            End If
        Loop While _IP < endIndex AndAlso depth > 0
        If _IP >= endIndex OrElse depth <> 0 Then Throw New Exception("unmatched semi")
    End Sub

    ' this pushes the next object to the data stack And skips over it in the runstream.
    ' in other words, instead of executing it, it pushes it on the stack.
    ' that way the program becomes data
    <RPLWord("'")> Sub DoQuote()
        ' first find what this object is
        Dim startIndex As Integer = _IP + 1
        ' SkipOb takes care of skipping over any kind of object (composite or atomic)
        SkipOb()
        If _IP - startIndex > 1 Then
            ' if we skipped over more than one IP it means we skipped over a composite
            ' which we will push on the DS

            Dim ob As Object = _SECO(startIndex)
            If _OB.Equals(_DoCol) Then
                ' ignore the "prologue" keep the semi 
                _DS.Push(New Secondary(_SECO.GetRange(startIndex, _IP - startIndex)))
            ElseIf _OB.Equals(_DoSymb) Then
                ' ignore the "prologue" keep the semi 
                _DS.Push(New Symbolic(_SECO.GetRange(startIndex, _IP - startIndex)))
            ElseIf _OB.Equals(_DoList) Then
                ' ignore "prologue" and semi
                _DS.Push(New ObList(_SECO.GetRange(startIndex + 1, _IP - startIndex - 1)))
            Else
                Throw New Exception("unknown composite")
            End If
        Else
            ' push atomic object
            _DS.Push(_SECO(startIndex))
        End If
    End Sub

    ' this marks the beginning of a loop
    <RPLWord> Sub begin()
        _IP += 1
        _STK.Push(_IP)
    End Sub

    ' this marks the end of an infinite (not indefinite) loop
    ' currently there is no way to exit this kind of loop.
    ' it would require a way to directly pop the STK
    <RPLWord> Sub again()
        _IP = _STK.Peek
    End Sub

    ' pops a bool off of the data stack and does Again if it is false
    ' so
    ' #0 begin dup #1 + dup #10 == until
    ' pushes #0 to #10 on the data stack
    <RPLWord("until")> Sub _Until()
        If _DS.Pop Then
            _STK.Pop()
            _IP += 1
        Else
            _IP = _STK.Peek
        End If
    End Sub

    ' this is an interesting word
    ' removes the next object from the runstream
    ' pops the runstream
    ' and inserts the object in the inner secondary at the position of its IP
    ' its purpose is improving tail recursion efficiency
    <RPLWord> Sub cola()
        _IP += 1
        Dim ob As Object = _SECO(_IP)
        DoSemi()
        _SECO.Insert(_IP, ob)
    End Sub

    <RPLWord("quit?")> Sub quitq()
        If _DS.Pop Then _IP = _SECO.Count Else _IP += 1
        ' yes, a total cop-out
    End Sub

    <RPLWord("==")> Sub eq()
        _IP += 1
        _DS.Push(_DS.Pop = _DS.Pop)
    End Sub

    Sub Main()
        ' fill the words Dictionary
        Dim asRPLWord As RPLWord
        For Each mi As MethodInfo In GetType(RPNETVB).GetMethods
            asRPLWord = mi.GetCustomAttribute(GetType(RPLWord))
            If asRPLWord IsNot Nothing Then
                words.Add(If(asRPLWord.WordName.Length = 0, mi.Name, asRPLWord.WordName), mi.CreateDelegate(GetType(Action)))
            End If
        Next

        ' trivial loop
        ' echoes back what is written until nothing is entered
        Dim sb As New StringBuilder
        For i As Integer = 0 To 1000000
            sb.Append(" swap")
        Next
        Dim str As String = "ticks # 1 # 2" & sb.ToString & "  drop drop ticks swap - print"  ' "begin read dup $ ""Length"" ? # 0 == quit? parse eval print again ;"
        _SECO = StrTo(str)
        Dim nt As Long = Now.Ticks
        While _IP < _SECO.Count
            _OB = _SECO(_IP)
            Eval()
        End While
        Console.WriteLine(Now.Ticks - nt)
        Console.WriteLine("done")
        Console.ReadKey()
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
            _RS.Push(_SECO)
            _STK.Push(_IP)
            _SECO = _OB
            _IP = 0 ' implicit prolog
        ElseIf TypeOf _OB Is Action Then
            _DS.Pop()
            DirectCast(_OB, Action)()
        End If
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
    ' it should actually process what kind of object it is being generated
    ' so that whatever object is described in it, will be pushed on the stack
    ' instead of being executed.
    ' also, arguments like " # 1 # 2 " which describe *two* objects should be an error condition
    ' this will be different from how the command line will be treated.
    ' the command line will implicitly be a secondary, that is, the command line will be implicitly prepended with ":: "
    ' appended with " ;", parsed and evaluated.
    ' this is different from the original behavior of STR->, which is more or less equivalent to entering its argument on the command line.

    Dim escapes As New Dictionary(Of Char, Char) From {{"\"c, "\"c}, {"n"c, Chr(10)}, {"r"c, Chr(13)}, {"t"c, Chr(9)}, {""""c, """"c}}
    Dim literals As New Dictionary(Of String, Type) From {{"#", GetType(Integer)}, {"%", GetType(Single)}, {"%%", GetType(Double)}, {"$", GetType(String)}, {"@", GetType(Identifier)}}

    Dim type_specifier As New Dictionary(Of String, Type) From {
         {"#", GetType(Integer)}, ' #123 or # 123 or maybe even #b #s #i #l to indicate byte, short, integer, long
         {"%", GetType(Single)}, ' %123 or % 123
         {"%%", GetType(Double)},
         {"id", GetType(Identifier)},
         {"$", GetType(String)}
    }

    ' this will be filled at initialization from the <RplWord> methods in this class
    Dim words As New Dictionary(Of String, Object) From {
         {"}", _DoSemi} ' yes, it has to be this way, this marks the end of the list
    }

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
        Dim l1 As Object = _DS.Pop
        Dim l2 As Object = _DS.Pop
        _DS.Push(l1)
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

    ' a delegate that can be overwritten so that the calling code can set it to whatever
    Public W As Action(Of String) = Sub(s) Debug.WriteLine(s)

End Module
