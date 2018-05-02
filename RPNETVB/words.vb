Imports System.Linq
Imports System.Reflection
Partial Module RPNETVB
    Public _DoCol As Action = AddressOf DoCol ' just so i don't carry the cast around
    Public _DoList As Action = AddressOf DoList
    Public _DoSym As Action = AddressOf DoSym ' same thing, actually
    Public _DoSemi As Action = AddressOf DoSemi

    ''' <summary>
    ''' pops the data stack into the next object to be evaluated
    ''' </summary>
    <RPLWord("eval")> Public Sub rpleval()
        _OB = _DS.Pop
        'Eval()
    End Sub

    <RPLWord("tostr")> Sub _tostr()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(tostr(_DS.Pop))
    End Sub

    <RPLWord> Sub begin()
        _IP += 1
        _OB = _RS(_IP)
        _IPSTK.Push(_IP)
    End Sub

    <RPLWord> Sub again()
        _IP = _IPSTK.Peek
        _OB = _RS(_IP)
    End Sub

    <RPLWord("i?")> Sub _idx()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(_LOOPindex)
    End Sub

    <RPLWord("words")> Sub listwords()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(New Composite(_DoList, words.Values.ToList))
    End Sub

    <RPLWord("==")> Sub eq()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(_DS.Pop = _DS.Pop)
    End Sub

    <RPLWord("+")> Sub _add()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(_DS(1) + _DS(0))
        _DS.RemoveRange(1, 2)
    End Sub

    <RPLWord> Sub ticks()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(Now.Ticks)
    End Sub

    <RPLWord("and")> Sub _and()
        _IP += 1
        _DS.Push(_DS(1) And _DS(0))
        _DS.RemoveRange(1, 2)
        _OB = _RS(_IP)
    End Sub

    <RPLWord("or")> Sub _or()
        _IP += 1
        _DS.Push(_DS(1) And _DS(0))
        _DS.RemoveRange(1, 2)
        _OB = _RS(_IP)
    End Sub

    <RPLWord("not")> Sub _not()
        _IP += 1
        _DS.Push(Not _DS.Pop)
        _OB = _RS(_IP)
    End Sub

    <RPLWord("-")> Sub _sub()
        _IP += 1
        _DS.Push(_DS(1) - _DS(0))
        _DS.RemoveRange(1, 2)
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub cola()
        _IP += 1
        Dim ob As Object = _RS(_IP)
        DoSemi()
        _RS.Insert(_IP, ob)
        _OB = _RS(_IP)
    End Sub

    <RPLWord("end")> Sub _end()
        quit = True
    End Sub

    <RPLWord("debug")> Sub _debug()
        Stop
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <RPLWord("::")> Sub DoCol()
        If _OB.Equals(_DoCol) Then ' then it means we're running in the runstream
            Dim startIndex As Integer = _IP + 1
            SkipOb()
            _RSSTK.Push(_RS)
            _IPSTK.Push(_IP)
            _RS = New Composite(_DoCol, _RS.GetRange(startIndex, _IP - startIndex))
            _IP = 0
            _OB = _RS(0)
        Else ' it means we're being evaluated from the datastack
            _IPSTK.Push(_IP + 1) 'implicit DoCol
            _IP = 0
            _RSSTK.Push(_RS)
            _RS = _OB
            _OB = _RS(_IP)
        End If
    End Sub

    <RPLWord("{")> Sub DoList()
        If _OB.Equals(_DoList) Then
            Dim startIndex As Integer = _IP + 1 'keep it, before SkipOb rapes it
            SkipOb()
            _DS.Push(New Composite(_DoList, _RS.GetRange(startIndex, _IP - startIndex - 1))) 'ignore DoList AND DoSemi (it's a list)
            _OB = _RS(_IP)
        Else
            ' nothing... a list is just a list... all it does is push itself on the stack, which accomplishes nothing so it does nothing
            _IP += 1
            _OB = _RS(_IP)
        End If
    End Sub

    <RPLWord> Sub parse()
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

    <RPLWord(";")> Sub DoSemi()
        _IP = _IPSTK.Pop()
        _RS = _RSSTK.Pop
        _OB = _RS(_IP)
    End Sub

    <RPLWord("'")> Sub DoQuote()
        _IP += 1
        Dim startIndex As Integer = _IP
        SkipOb()
        If _IP - startIndex > 1 Then
            Dim ob As Object = _RS(startIndex)
            ' i should really factor this out, have one composite class and the type as a parameter or something, because basically all composites are the same
            ' it's just the header that changes
            If ob.Equals(_DoCol) OrElse ob.Equals(_DoList) OrElse ob.Equals(_DoSym) Then ' really need anotherway to check if it starts a composite
                _DS.Push(New Composite(ob, _RS.GetRange(startIndex + 1, _IP - startIndex - 1)))
            Else
                Throw New Exception("unknown composite")
            End If
        Else
            _DS.Push(_RS(startIndex))
        End If
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub dup()
        _IP += 1
        _DS.Push(_DS.Peek())
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub read()
        _IP += 1
        _DS.Push(R)
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub print()
        _IP += 1
        _OB = _RS(_IP)
        If TypeOf _DS.Peek Is String Then W(_DS.Pop) Else W(tostr(_DS.Pop()))
    End Sub

    <RPLWord> Sub drop()
        _IP += 1
        _DS.Pop()
        _OB = _RS(_IP)
    End Sub

    <RPLWord("sym")> Sub DoSym()
        Dim startIndex As Integer = _IP + 1 'keep it, before SkipOb rapes it
        SkipOb()
        _DS.Push(_RS.GetRange(startIndex, _IP - startIndex)) 'ignore DoList AND DoSemi (it's a list)
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub depth()
        _IP += 1
        _DS.Push(_DS.Count)
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub swap() ' something as trivial as this takes so much time. it's just two pointers. 
        _IP += 1
        _DS.Push(_DS(1))
        _DS.RemoveAt(2)
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub pick()
        _IP += 1
        _DS.Push(_DS(_DS.Pop - 1)) ' in RPL, the top level stack is numbered "1" so # 1 pick is equivalent to dup
        _OB = _RS(_IP)
    End Sub

    <RPLWord> Sub ifte()
        '_IP += 1
        _OB = If(_DS(2), _DS(1), _DS(0))
        _DS.RemoveRange(0, 3)
    End Sub

    <RPLWord("do")> Sub _do()
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

    <RPLWord("loop")> Sub _loop()
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

    <RPLWord()> Sub clear()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Clear()
    End Sub


    ' could probably factor out the common code of these two
    ' yeah imma do that right now

    Sub CreateComposite(head As Action)
        _IP += 1
        _OB = _RS(_IP)
        Dim count As Integer = _DS.Pop
        Dim comp As New Composite(head)
        _DS.Push(comp)
        If count > 0 Then
            comp.AddRange(_DS.Skip(1).Take(count))
            _DS.RemoveRange(1, count)
        End If
        comp.Add(_DoSemi) ' you don't have to add them explicitly
    End Sub
    <RPLWord("::n")> Sub createSecondary()
        CreateComposite(_DoCol)
    End Sub

    <RPLWord("{}n")> Sub createObList()
        CreateComposite(_DoList)
    End Sub

    <RPLWord> Sub innercomp()
        _IP += 1
        _OB = _RS(_IP)
        Dim comp As Composite = _DS.Pop
        For Each ob As Object In comp
            _DS.Push(ob)
        Next
        _DS.Push(comp.Count)
    End Sub

    ' copout for now
    <RPLWord("substr")> Sub _substr()
        _IP += 1
        _OB = _RS(_IP)
        _DS.Push(DirectCast(_DS(1), String).Substring(_DS(0)))
        _DS.RemoveRange(1, 2)
    End Sub

    <RPLWord> Sub define()
        _IP += 1
        _OB = _RS(_IP)
        If words.ContainsKey(_DS(0)) Then words(_DS(0)) = _DS(1) Else words.Add(_DS(0), _DS(1))
        _DS.RemoveRange(0, 2)
    End Sub

    <RPLWord> Sub rcl()
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <RPLWord("@")> Sub methodcall()
        Dim methodname As String = _DS.Pop
        Dim args() As Object = New Object() {}
        Dim argtypes() As Type = New Type() {}
        If TypeOf _DS(0) Is Composite Then
            args = DirectCast(_DS.Pop, Composite).ToArray
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
    <RPLWord("?")> Sub fieldrecall()
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
            _DS.Push(Interaction.CallByName(ob, fieldname, CallType.Get))
        End If
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <RPLWord("!")> Sub bang()
        Dim fieldname As String = _DS.Pop
        Dim newvalue As Object = _DS.Pop
        Dim ob As Object = _DS.Pop
        Interaction.CallByName(ob, fieldname, CallType.Set, newvalue)
        _IP += 1
        _OB = _RS(_IP)
    End Sub

    <RPLWord("new")> Sub _new()
        Dim args() As Object = New Object() {}
        Dim argtypes() As Type = New Type() {}
        If TypeOf _DS(0) Is Composite Then
            args = DirectCast(_DS.Pop, Composite).ToArray
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
