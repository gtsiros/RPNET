Imports System.Linq
Imports System.Reflection
Partial Module RPNETVB
    Dim escapes As New Dictionary(Of Char, Char) From {{"\"c, "\"c}, {"n"c, Chr(10)}, {"r"c, Chr(13)}, {"t"c, Chr(9)}, {""""c, """"c}}
    Dim type_specifier As New Dictionary(Of String, Type) From {{"#", GetType(Integer)}, {"%", GetType(Single)}, {"%%", GetType(Double)}, {"$", GetType(String)}}

    Enum LexerState
        token
        whitespace
        escape
        cstring
        comment
    End Enum

    Function StrTo(str As String) As Object
        StrTo = Nothing
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
        If tokens.Count = 0 Then Throw New Exception("null input")
        If tokens(0).Equals(_DoCol) OrElse tokens.Equals(_DoList) OrElse tokens.Equals(_DoSym) Then
            If tokens.Count < 2 Then Throw New Exception("expecting more tokens")
            If Not tokens(tokens.Count - 1).Equals(_DoSemi) Then Throw New Exception("expected semi/endlist, found '" & tostr(tokens(tokens.Count - 1)) & "'")
            If tokens(0).Equals(_DoCol) Then
                StrTo = New Secondary(tokens.Skip(1))
            ElseIf tokens(0).Equals(_DoList) Then
                StrTo = New ObList(tokens.Skip(1))
            ElseIf tokens(0).Equals(_DoSym) Then
                StrTo = New Symbolic(tokens.Skip(1))
            End If
        ElseIf tokens.Count > 1 Then
            Throw New Exception("composite implied, but can't start with '" & tostr(tokens(0)) & "'")
        Else
            StrTo = tokens(0)
        End If
    End Function

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

    Function tostr(ob As Object) As String
        If ob Is Nothing Then Return "null"
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
        If ty = GetType(Secondary) Then Return ":: " & String.Join(" ", DirectCast(ob, Secondary).ConvertAll(Function(o) tostr(o)))

        Return ob.ToString & ", <" & ty.FullName & ">"
    End Function

End Module
