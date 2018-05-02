Imports System.Reflection
Imports System.Diagnostics
Imports System.Text
Imports System.Windows

Module RPNETVB

    Dim words As New Dictionary(Of String, Object)
    Public W As Action(Of String) = Sub(s) Console.WriteLine(s)
    Private _OB As Object
    Private _DS As New StackList(Of Object)
    Private _RS As Secondary
    Private _RSSTK As New StackList(Of Secondary)
    Private _IPSTK As New StackList(Of Integer)
    Private _IP As Integer = 0
    Private quit As Boolean = False
    Private _LOOPstart, _LOOPend, _LOOPindex, _LOOPip As Integer
    Private _LOOPSTK As New StackList(Of Integer)

    Sub SkipOb()
        Dim depth As Integer = 0
        Dim endIndex As Integer = _RS.Count - 1
        Do
            Dim ob As Object = _RS(_IP)
            _IP += 1
            If ob.Equals(_DoSemi) Then
                depth -= 1
            ElseIf ob.Equals(_DoCol) OrElse ob.Equals(_DoList) OrElse ob.Equals(_DoSym) Then
                depth += 1
            End If
        Loop While _IP < endIndex AndAlso depth > 0
        If _IP > endIndex AndAlso depth <> 0 Then Throw New Exception("unmatched semi")
    End Sub

    Sub Main()
        'For Each asm As Assembly In AppDomain.CurrentDomain.GetAssemblies
        '    Debug.WriteLine(asm.FullName)
        'Next

        'adds all the methods marked RPLWord to the dictionary
        For Each mi As MethodInfo In GetType(RPNETVB).GetMethods
            Dim asRPLWord As RPLWord = mi.GetCustomAttribute(GetType(RPLWord))
            If asRPLWord Is Nothing Then Continue For
            words.Add(If(asRPLWord.WordName.Length = 0, mi.Name, asRPLWord.WordName), mi.CreateDelegate(GetType(Action)))
        Next
        words.Add("}", GetType(RPNETVB).GetMethod("DoSemi").CreateDelegate(GetType(Action)))

        Dim str As String = ""
        _RS = New Secondary({ ' this can be done much simpler, but i hacked it as i went. remember, we're changing the system while we're using it
            words("begin"),
            words("read"),
            words("parse"),
            words("'"),
            words("eval"),
            words("'"),
            words("print"),
            words("ifte"),
            words("depth"),
            0,
            words("=="),
            words("'"),
            words("::"),
            "0:",
            words("print"),
            words(";"),
            words("'"),
            words("::"),
            words("depth"),
            1,
            words("swap"),
            words("do"),
            words("depth"),
            1,
            words("+"),
            words("i?"),
            words("-"),
            words("pick"),
            words("tostr"),
            words("depth"),
            words("i?"),
            words("-"),
            words("tostr"),
            ": ",
            words("+"),
            words("swap"),
            words("+"),
            words("{"),
            2,
            words("}"),
            "Substring", ' have the ability to call methods, might as well use it...
            words("@"),
            words("print"),
            words("loop"),
            words(";"),
            words("ifte"),
            words("again")
        })
        _OB = _RS(_IP)
        Do ' this is the "inner loop"
            If TypeOf _OB Is Action Then
                DirectCast(_OB, Action)()
            ElseIf TypeOf _OB Is Secondary Then ' for when a secondary is evaluated from the data stack
                _IPSTK.Push(_IP + 1) 'implicit DoCol
                _IP = 0
                _RSSTK.Push(_RS)
                _RS = _OB
                _OB = _RS(_IP)
            Else
                _DS.Push(_OB)
                _IP += 1
                _OB = _RS(_IP)
            End If
        Loop Until quit
        Console.WriteLine("done")
        Console.ReadKey()
    End Sub


End Module
