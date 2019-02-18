Imports System
Imports System.Linq
Imports System.Reflection
Imports Microsoft.VisualBasic

Partial Module RPNETVB
    Public _DoCol As Action = AddressOf RplDOCOL ' just so i don't carry the cast around
    Public _DoList As Action = AddressOf RplDOLIST
    Public _DoSym As Action = AddressOf RplDOSYM ' same thing, actually
    Public _DoSemi As Action = AddressOf RplDOSEMI

    ''' <summary>
    ''' pops the data stack into the next object to be evaluated
    ''' </summary>
    <TRPLWord("EVAL")> Public Sub RplEval()
        _OB = _DS.Pop
        'Eval()
    End Sub

    <TRPLWord("TOSTR")> Sub RplToString()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(Tostr(_DS.Pop))
    End Sub

    <TRPLWord("BEGIN")> Sub RplBegin()
        _IP += 1
        _OB = _RS(_IP)
        _IPSTK.Push(_IP)
    End Sub

    <TRPLWord("AGAIN")> Sub RplAgain()
        _IP = _IPSTK.Peek
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("LOOPI")> Sub RplRecallIndex()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(_LOOPindex)
    End Sub

    <TRPLWord("WORDS")> Sub RplListWords()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(New TComposite(_DoList, words.Values.ToList))
    End Sub

    <TRPLWord("==")> Sub RplEquality()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(_DS.Pop.Equals(_DS.Pop))
    End Sub

    <TRPLWord("+")> Sub _RplAddition()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(_DS(1) + _DS(0))
        _DS.RemoveRange(1, 2)
    End Sub

    <TRPLWord("TICKS")> Sub RplClockTicks()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(Now.Ticks)
    End Sub

    <TRPLWord("AND")> Sub RplAnd()
        _IP += 1
        _DS.Push(_DS(1) And _DS(0))
        _DS.RemoveRange(1, 2)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("OR")> Sub RplOr()
        _IP += 1
        _DS.Push(_DS(1) Or _DS(0))
        _DS.RemoveRange(1, 2)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("NOT")> Sub RplNot()
        _IP += 1
        _DS.Push(Not _DS.Pop)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("-")> Sub RplSubtract()
        _IP += 1
        _DS.Push(_DS(1) - _DS(0))
        _DS.RemoveRange(1, 2)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("COLA")> Sub RplCola()
        _IP += 1
        Dim ob As Object = _RS(_IP)
        RplDOSEMI()
        _RS.Insert(_IP, ob)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("END")> Sub RplLameQuit()
        quit = True
    End Sub

    <TRPLWord("debug")> Sub _debug() '' don't remember what this does
        Stop
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("::")> Sub RplDOCOL()
        If _OB.Equals(_DoCol) Then ' then it means we're running in the runstream
            Dim startIndex As Int32 = _IP + 1
            SkipOb()
            _IPSTK.Push(_IP)
            _RSSTK.Push(_RS)
            _RS = New TComposite(_DoCol, _RS.GetRange(startIndex, _IP - startIndex - 2)) ' skip the semi from the runstream
        Else ' it means we're being evaluated from the datastack
            _IPSTK.Push(_IP + 1) 'implicit DoCol
            _RSSTK.Push(_RS)
            _RS = _OB ' remember the composite on the stack does not contain an ending semi
        End If
        _RS.Add(_DoSemi)
        _IP = 0
        _OB = _RS(0)
    End Sub

    <TRPLWord("{")> Sub RplDOLIST()
        If _OB.Equals(_DoList) Then ' being evaluated from the runstream
            Dim startIndex As Int32 = _IP + 1 'keep it, before SkipOb rapes it
            SkipOb()
            _DS.Push(New TComposite(_DoList, _RS.GetRange(startIndex, _IP - startIndex - 2))) 'ignore DoList AND DoSemi (it's a list)
        Else ' being evaluated from the stack
            _IP += 1
            ' nothing... a list is just a list... all it does is push itself on the stack, which accomplishes nothing so it does nothing
        End If
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("STRTO")> Sub RplParse()
        _IP += 1
        Try
            Dim ob As Object = StrTo(_DS.Pop)
            _DS.Push(ob)
            _DS.Push(True)
        Catch ex As Exception
            _DS.Push(ex.Message)
            _DS.Push(False)
        End Try
        _OB = _RS(_IP)
    End Sub

    <TRPLWord(";")> Sub RplDOSEMI()
        _IP = _IPSTK.Pop()
        _RS = _RSSTK.Pop
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("'")> Sub RplQUOTE()
        _IP += 1
        Dim startIndex As Int32 = _IP
        SkipOb()
        If _IP - startIndex > 1 Then
            Dim ob As Object = _RS(startIndex)
            ' i should really factor this out, have one composite class and the type as a parameter or something, because basically all composites are the same
            ' it's just the header that changes
            If IsCompositeHead(ob) Then ' really need anotherway to check if it starts a composite
                _DS.Push(New TComposite(ob, _RS.GetRange(startIndex + 1, _IP - startIndex - 2))) ' the semi doesn't exist explicitly in the list of objects
            Else
                Throw New Exception("unknown composite")
            End If
        Else
            _DS.Push(_RS(startIndex))
        End If
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("DUP")> Sub RplDup()
        _IP += 1
        _DS.Push(_DS.Peek())
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("INPUT")> Sub RplReadLine()
        _IP += 1
        _DS.Push(r)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("PRINT")> Sub RplPrint()
        _IP += 1
        _OB = _RS(_IP)
        If TypeOf _DS.Peek Is String Then w(_DS.Pop) Else w(Tostr(_DS.Pop()))
    End Sub

    <TRPLWord("DROP")> Sub RplDrop()
        _IP += 1
        _DS.Pop()
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("sym")> Sub RplDOSYM()
        Dim startIndex As Int32 = _IP + 1 'keep it, before SkipOb rapes it
        SkipOb()
        _DS.Push(_RS.GetRange(startIndex, _IP - startIndex)) 'ignore DoList AND DoSemi (it's a list)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("DEPTH")> Sub RplDataStackDepth()
        _IP += 1
        _DS.Push(_DS.Count)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("SWAP")> Sub RplDataStackSwap() ' something as trivial as this takes so much time. it's just two pointers. 
        _IP += 1
        _DS.Push(_DS(1))
        _DS.RemoveAt(2)
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("PICK")> Sub RplDataStackPick()
        _IP += 1
        _DS.Push(_DS(_DS.Pop - 1)) ' in RPL, the top level stack is numbered "1" so # 1 pick is equivalent to dup
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("IFTE")> Sub RplIFTE()
        '_IP += 1
        _OB = If(_DS(2), _DS(1), _DS(0))
        _DS.RemoveRange(0, 3)
    End Sub

    <TRPLWord("DO")> Sub RplDo()
        _IP += 1
        _OB = _RS(_IP)
        _LOOPSTK.Push(_LOOPstart)
        _LOOPSTK.Push(_LOOPend)
        _LOOPSTK.Push(_LOOPip)
        _LOOPSTK.Push(_LOOPindex)
        _LOOPend = _DS.Pop
        _LOOPstart = _DS.Pop
        _LOOPip = _IP
        _LOOPindex = _LOOPstart
    End Sub

    <TRPLWord("LOOP")> Sub RplLoop()
        _LOOPindex += 1
        If _LOOPindex <= _LOOPend Then
            _IP = _LOOPip
        Else
            _LOOPindex = _LOOPSTK.Pop
            _LOOPip = _LOOPSTK.Pop
            _LOOPend = _LOOPSTK.Pop
            _LOOPstart = _LOOPSTK.Pop
            _IP += 1
        End If
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("CLEAR")> Sub RplDataStackClear()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Clear()
    End Sub

    Sub CreateComposite(head As Action)
        _IP += 1
        _OB = _RS(_IP)
        Dim count As Int32 = _DS.Pop
        Dim comp As New TComposite(head)
        _DS.Push(comp)
        If count > 0 Then
            comp.AddRange(_DS.Skip(1).Take(count))
            _DS.RemoveRange(1, count)
        End If
    End Sub

    <TRPLWord("::n")> Sub RplCreateSecondary()
        CreateComposite(_DoCol)
    End Sub

    <TRPLWord("{}n")> Sub RplCreateList()
        CreateComposite(_DoList)
    End Sub

    <TRPLWord("INNERCOMP")> Sub RplInnercomp()
        _IP += 1
        _OB = _RS(_IP)
        Dim comp As TComposite = _DS.Pop
        For Each ob As Object In comp
            _DS.Push(ob)
        Next
        _DS.Push(comp.Count)
    End Sub

    ' copout for now
    <TRPLWord("SUB")> Sub RplSubstring()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(DirectCast(_DS(1), String).Substring(_DS(0)))
        _DS.RemoveRange(1, 2)
    End Sub

    <TRPLWord("DEFINE")> Sub RplDefine()
        _IP += 1
        _OB = _RS(_IP)
        If words.ContainsKey(_DS(0)) Then words(_DS(0)) = _DS(1) Else words.Add(_DS(0), _DS(1))
        _DS.RemoveRange(0, 2)
    End Sub

    <TRPLWord("RCL")> Sub RplRecallVariable() '' does nothing yet
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("@")> Sub RplMethodCall() '' doubt it's implemented properly yet
        Dim methodname As String = _DS.Pop
        Dim args() As Object = New Object() {}
        Dim argtypes() As Type = New Type() {}
        If TypeOf _DS(0) Is TComposite Then
            args = DirectCast(_DS.Pop, TComposite).ToArray
            argtypes = Type.GetTypeArray(args)
        End If
        Dim ob As Object = _DS.Pop
        Dim ty As Type
        Dim bf As BindingFlags = BindingFlags.Public Or BindingFlags.Instance
        If TypeOf ob Is Type Then
            ty = ob
            bf = BindingFlags.Public Or BindingFlags.Static
        Else
            ty = ob.GetType
        End If
        Dim mi As MethodInfo = ty.GetMethod(methodname, bf, Nothing, argtypes, Nothing)
        Dim resob As Object = mi.Invoke(ob, args)
        If mi.ReturnType IsNot GetType(Void) Then _DS.Push(resob)
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("?")> Sub RplFieldGetValue()
        Dim fieldname As String = _DS.Pop
        Dim ob As Object = _DS.Pop
        If TypeOf ob Is Type Then
            Dim ty As Type = DirectCast(ob, Type)
            Dim fi As FieldInfo = ty.GetField(fieldname, BindingFlags.Static Or BindingFlags.Public)
            If fi IsNot Nothing Then
                _DS.Push(fi.GetValue(ob))
            Else
                Dim pi As PropertyInfo = ty.GetProperty(fieldname, BindingFlags.Static Or BindingFlags.Public)
                If pi IsNot Nothing Then
                    _DS.Push(pi.GetValue(ob))
                Else
                    _DS.Push(Nothing)
                End If
            End If
        Else
            _DS.Push(CallByName(ob, fieldname, CallType.Get))
        End If
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("!")> Sub RplFieldSetValue()
        Dim fieldname As String = _DS.Pop
        Dim newvalue As Object = _DS.Pop
        Dim ob As Object = _DS.Pop
        CallByName(ob, fieldname, CallType.Set, newvalue)
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <TRPLWord("NEW")> Sub RplNew()
        Dim args() As Object = New Object() {}
        Dim argtypes() As Type = New Type() {}
        If TypeOf _DS(0) Is TComposite Then
            args = DirectCast(_DS.Pop, TComposite).ToArray
            argtypes = Type.GetTypeArray(args)
        End If
        Dim ty As Type = _DS.Pop
        Dim ob As Object = ty.Assembly.CreateInstance(ty.FullName, True, BindingFlags.CreateInstance, Nothing, args, Nothing, Nothing)
        _DS.Push(ob)
        _DS.Push(ob IsNot Nothing)
        _IP += 1
        _OB = _RS(_IP)
    End Sub
End Module
