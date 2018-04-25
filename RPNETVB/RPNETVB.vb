Imports System.Reflection
Imports System.Diagnostics

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
    Private _DoCol As Action = AddressOf DoCol ' just so i don't carry the cast around
    Private _DoSemi As Action = AddressOf DoSemi
    Private _DoList As Action = AddressOf DoList
    Private _DoSymb As Action = AddressOf DoSymb ' same thing, actually
    Private _Begin As Action = AddressOf Begin
    Private _Again As Action = AddressOf Again
    Private _Until As Action = AddressOf __Until
    Private _QuitQ As Action = AddressOf QuitQ
    Private _Drop As Action = AddressOf Drop ' some trivial words 
    Private _Dup As Action = AddressOf Dup
    Private _Eval As Action = Sub() Eval(_DS.Pop())
    Private _StrTo As Action = Sub() Eval(StrTo(_DS.Pop()))
    Private _Read As Action = AddressOf Read
    Private _Print As Action = AddressOf Print
    Sub Dup()
        _IP += 1
        _DS.Push(_DS.Peek())
    End Sub
    Sub Read()
        _IP += 1
        _DS.Push(Console.ReadLine())
    End Sub
    Sub Print()
        _IP += 1
        Console.WriteLine(_DS.Pop().ToString)
    End Sub
    Sub Drop()
        _IP += 1
        _DS.Pop()
    End Sub
    Enum Estate
        token
        whitespace
        escape
        cstring
        comment
    End Enum
    Dim escapes As New Dictionary(Of Char, Char) From {{"\"c, "\"c}, {"n"c, Chr(10)}, {"r"c, Chr(13)}, {"t"c, Chr(9)}, {""""c, """"c}}
    Dim literals As New Dictionary(Of String, Type) From {{"#", GetType(Integer)}, {"%", GetType(Single)}, {"%%", GetType(Double)}, {"$", GetType(String)}, {"@", GetType(Identifier)}}
    Dim words As New Dictionary(Of String, Object) From {
         {"::", _DoCol},
         {";", _DoSemi},
         {"{", _DoList},
         {"}", _DoSemi}, ' yes, it has to be this way, this marks the end of the list
         {"begin", _Begin},
         {"again", _Again},
         {"until", _Until},
         {"quitq", _QuitQ},
         {"drop", _Drop},
         {"read", _Read},
         {"print", _Print},
         {"dup", _Dup},
         {"eval", _Eval}
    }

    Class Secondary
        Inherits List(Of Object)
        <DebuggerStepThrough> Sub New(l As IEnumerable(Of Object))
            MyBase.AddRange(l)
        End Sub
    End Class

    Class Symbolic
        Inherits List(Of Object)
        <DebuggerStepThrough> Sub New(l As IEnumerable(Of Object))
            MyBase.AddRange(l)
        End Sub
    End Class

    Class Identifier
        Public name As String
        Public Shared Widening Operator CType(s As String) As Identifier
            Return New Identifier With {.name = s}
        End Operator
        ' TODO: add constructor that checks for sane name values

    End Class

    Sub DoCol()
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
    Sub DoSemi()
        ' no need to IP++ at the start since we're replacing the current secondary anyway
        ' pop the top secondary from the runstream, and make it the current one
        _SECO = _RS.Pop()
        ' restore the inner secondary's IP
        _IP = _STK.Pop
    End Sub
    'doubt we'll ever get here, but why not
    Sub DoSymb()
    End Sub

    Sub DoList()
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
    Sub DoQuote()
        ' first find what this object is
        Dim startIndex As Integer = _IP + 1
        ' SkipOb takes care of skipping over any kind of object (composite or atomic)
        SkipOb()
        If _IP - startIndex > 1 Then
            ' if we skipped over more than one IP it means we're pushing a composite

            Dim ob As Object = _SECO(startIndex)
            If _OB.Equals(_DoCol) Then
                ' ignore the "prologue" keep the semi 
                _DS.Push(New Secondary(_SECO.GetRange(startIndex, _IP - startIndex)))
            ElseIf _OB.Equals(_DoSymb) Then
                ' ignore the "prologue" keep the semi 
                _DS.Push(New Symbolic(_SECO.GetRange(startIndex, _IP - startIndex)))
            ElseIf _OB.Equals(_DoList) Then
                ' ignore "prologue" and semi
                _DS.Push(New List(Of Object)(_SECO.GetRange(startIndex + 1, _IP - startIndex - 1)))
            Else
                Throw New Exception("unknown composite")
            End If
        Else
            _DS.Push(_SECO(startIndex))
        End If
    End Sub

    ' this marks the beginning of a loop
    Sub Begin()
        _IP += 1
        _STK.Push(_IP)
    End Sub

    ' this marks the end of an infinite (not indefinite) loop
    ' currently there is no way to exit this kind of loop.
    ' it would require a way to directly pop the STK
    Sub Again()
        _IP = _STK.Peek
    End Sub

    ' pops a bool off of the data stack and does Again if it is false
    ' so
    ' #0 begin dup #1 + dup #10 == until
    ' pushes #0 to #10 on the data stack
    Sub __Until()
        If DirectCast(_DS.Pop, Boolean) Then
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
    Sub Cola()
        _IP += 1
        Dim ob As Object = _SECO(_IP)
        DoSemi()
        _SECO.Insert(_IP, ob)
    End Sub
    Sub QuitQ()
        _IP += 1
        If DirectCast(_DS.Pop, String) = "" Then _IP = _SECO.Count
        ' yes, a total cop-out
    End Sub

    Sub Main()
        ' trivial loop
        ' echoes back what is written until nothing is entered
        Dim str As String = " begin read dup quitq print again ;"

        _SECO = StrTo(str)
        While _IP < _SECO.Count
            _OB = _SECO(_IP)
            Eval()
        End While
        Console.WriteLine("done")
        Console.ReadKey()
    End Sub

    Sub Eval(Optional ob As Object = Nothing)
        If ob Is Nothing Then ob = _OB
        Dim t As Type = ob.GetType
        If t Is GetType(Secondary) Then
            _SECO.Insert(_IP, _DoCol)
            _SECO.InsertRange(_IP + 1, DirectCast(ob, Secondary))
        ElseIf t Is GetType(Symbolic) Then
            _DS.Push(ob)
        ElseIf t Is GetType(Action) Then
            DirectCast(ob, Action)()
        Else
            _IP += 1
            _DS.Push(ob)
        End If
    End Sub

    Dim type_specifier As New Dictionary(Of String, Type) From {
         {"#", GetType(Integer)}, ' #123 or # 123 or maybe even #b #s #i #l to indicate byte, short, integer, long
         {"%", GetType(Single)}, ' %123 or % 123
         {"%%", GetType(Double)},
         {"@", GetType(Identifier)},
         {"$", GetType(String)}
    }
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
                If t IsNot Nothing Then
                    tokens.Add(t)
                Else
                    Throw New Exception("can't find Type for '" & TypeName)
                End If
            Else
                Dim ob As Object = Nothing
                If words.TryGetValue(term, ob) Then
                    tokens.Add(ob)
                Else
                    Throw New Exception("unknown term '" + term + "'")
                End If
            End If
        Next
        Return New Secondary(tokens)
    End Function
    Function Split(str As String) As List(Of String)
        Split = New List(Of String)

        Dim state As Estate = Estate.whitespace
        Dim token As String = ""
        For Each c As Char In str
            Select Case state
                Case Estate.whitespace
                    If """"c = c Then
                        state = Estate.cstring
                        token = ""
                    ElseIf "`"c = c Then
                        state = Estate.comment
                    ElseIf Not Char.IsWhiteSpace(c) Then
                        state = Estate.token
                        token = c.ToString
                    End If
                Case Estate.token
                    If Char.IsWhiteSpace(c) Then
                        Split.Add(token)
                        token = ""
                        state = Estate.whitespace
                    Else
                        token &= c
                    End If
                Case Estate.cstring
                    If "\"c = c Then
                        state = Estate.escape
                    ElseIf """"c = c Then
                        Split.Add(token)
                        state = Estate.whitespace
                    Else
                        token &= c
                    End If
                Case Estate.escape
                    If escapes.ContainsKey(c) Then
                        token &= escapes(c)
                        state = Estate.cstring
                    Else
                        Throw New Exception("bad escape char '" & c & "'")
                    End If
                Case Estate.comment
                    If Chr(10) = c OrElse Chr(13) = c Then
                        state = Estate.whitespace
                    End If
            End Select
        Next
        Select Case state
            Case Estate.comment
            Case Estate.whitespace
            'all is ok
            Case Estate.token
                Split.Add(token)
            Case Else
                Throw New Exception("expecting " & state.ToString() & ", not '" & token & "'")
        End Select
    End Function
    ' a delegate that can be overwritten so that the calling code can set it to whatever
    Public W As Action(Of String) = Sub(s) Debug.WriteLine(s)


End Module
