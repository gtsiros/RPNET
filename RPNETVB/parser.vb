Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection
Imports Microsoft.VisualBasic
Partial Module RPNETVB
    Private escapes As New Dictionary(Of Char, Char) From {{"\"c, "\"c}, {"n"c, Chr(10)}, {"r"c, Chr(13)}, {"t"c, Chr(9)}, {""""c, """"c}}
    Private type_specifier As New Dictionary(Of String, Type) From {{"#", GetType(Int32)}, {"%", GetType(Single)}, {"%%", GetType(Double)}, {"$", GetType(String)}}

    Private Enum ELexerState
        token
        whitespace
        escape
        cstring
        comment
    End Enum

    Private Function StrTo(str As String) As Object
        StrTo = Nothing
        Dim tokens As New List(Of Object)
        Dim expect As Type = Nothing
        Dim ob As Object = Nothing
        For Each term As String In Split(str)
            If expect IsNot Nothing Then
                tokens.Add(CTypeDynamic(term, expect))
                expect = Nothing
            ElseIf type_specifier.TryGetValue(term, expect) Then ' if it succeeds i don't have to do anything else
            ElseIf term(0) = "<"c AndAlso term.EndsWith(">") Then
                Dim TypeName As String = term.Substring(1, term.Length - 2)
                Dim t As Type = Type.GetType(TypeName, False, True)
                If t Is Nothing Then
                    For Each asm As Assembly In AppDomain.CurrentDomain.GetAssemblies
                        t = asm.GetType(TypeName, False, True)
                        If t IsNot Nothing Then Exit For
                    Next
                End If
                If t IsNot Nothing Then tokens.Add(t) Else Throw New Exception("can't find Type for '" & TypeName & "'")
            ElseIf words.TryGetValue(term, ob) Then
                tokens.Add(ob)
            Else Throw New Exception("unknown term '" + term + "'")
            End If
        Next
        If tokens.Count = 0 Then Throw New Exception("null input")
        If tokens(0).Equals(_DoCol) OrElse tokens(0).Equals(_DoList) OrElse tokens(0).Equals(_DoSym) Then
            If tokens.Count < 2 Then Throw New Exception("expecting more tokens")
            If Not tokens(tokens.Count - 1).Equals(_DoSemi) Then Throw New Exception("expected semi/endlist, found '" & Tostr(tokens(tokens.Count - 1)) & "'")
            StrTo = New TComposite(tokens(0), tokens.Skip(1).Take(tokens.Count - 2)) ' skip its semi
        ElseIf tokens.Count > 1 Then
            Throw New Exception("composite implied, but can't start any with '" & Tostr(tokens(0)) & "'")
        Else
            StrTo = tokens(0)
        End If
    End Function

    Private Function Split(str As String) As List(Of String)
        Split = New List(Of String)
        Dim state As ELexerState = ELexerState.whitespace
        Dim token As String = ""
        For Each c As Char In str
            Select Case state
                Case ELexerState.whitespace
                    If """"c = c Then
                        state = ELexerState.cstring
                        token = ""
                    ElseIf "`"c = c Then
                        state = ELexerState.comment
                    ElseIf Not Char.IsWhiteSpace(c) Then
                        state = ELexerState.token
                        token = c.ToString
                    End If
                Case ELexerState.token
                    If Char.IsWhiteSpace(c) Then
                        Split.Add(token)
                        token = ""
                        state = ELexerState.whitespace
                    Else
                        token &= c
                    End If
                Case ELexerState.cstring
                    If "\"c = c Then
                        state = ELexerState.escape
                    ElseIf """"c = c Then
                        Split.Add(token)
                        state = ELexerState.whitespace
                    Else
                        token &= c
                    End If
                Case ELexerState.escape
                    If escapes.ContainsKey(c) Then
                        token &= escapes(c)
                        state = ELexerState.cstring
                    Else
                        Throw New Exception("bad escape char '" & c & "'")
                    End If
                Case ELexerState.comment
                    If Chr(10) = c OrElse Chr(13) = c Then
                        state = ELexerState.whitespace
                    End If
            End Select
        Next
        Select Case state
            Case ELexerState.comment ' all is ok
            Case ELexerState.whitespace

            Case ELexerState.token
                Split.Add(token)
            Case Else
                Throw New Exception("expecting " & state.ToString() & ", not '" & token & "'")
        End Select
    End Function

    Private Function Tostr(ob As Object) As String
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

        If TypeOf ob Is Action Then
            Dim act As Action = DirectCast(ob, Action)
            If act.Method IsNot Nothing Then
                Dim mi As MethodInfo = act.Method
                Dim asRPLWord As TRPLWord
                asRPLWord = act.Method.GetCustomAttribute(GetType(TRPLWord))
                If asRPLWord IsNot Nothing Then Return If(asRPLWord.wordName.Length = 0, mi.Name, asRPLWord.wordName)
            End If
            Return act.ToString
        End If

        If TypeOf ob Is Type Then Return "<" & DirectCast(ob, Type).FullName & ">"
        If TypeOf ob Is TComposite Then
            Dim comp As TComposite = ob
            'Return comp.Aggregate(tostr(comp.head), Function(a, b) a & " " & tostr(b))
            Tostr = comp.Aggregate(Tostr(comp.head), Function(a, b) a & " " & Tostr(b))
            Return Tostr & If(comp.head.Equals(_DoList), " }", " ;")
            'If comp.head.Equals(_DoList) Then ' only one that ends with } instead of ;
            '    Return Tostr & " }"
            'Else
            '    Return Tostr & " ;" ' do-hoho
            'End If
        End If
        Tostr = words.FirstOrDefault(Function(kvp) kvp.Value.Equals(ob)).Key
        Return If(Tostr, "<""" & ob.ToString & """>")
    End Function

End Module
