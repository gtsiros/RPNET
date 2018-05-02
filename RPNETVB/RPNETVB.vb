Imports System.Reflection
Imports System.Diagnostics
Imports System.Text
Imports System.Windows

Module RPNETVB

    ' the list of commands supported by the runtime
    Dim words As New Dictionary(Of String, Object)

    ' for changing where the program outputs text
    Public W As Action(Of String) = Sub(s) Console.WriteLine(s)

    ' the currently (or "next", depending how you view it ) executed OBject
    Private _OB As Object

    ' the Data Stack, it is a custom class that shares functionality of a stack and a list
    Private _DS As New StackList(Of Object)

    ' the current RunStream object (always a program)
    Private _RS As Secondary

    ' the return stack for objects
    Private _RSSTK As New StackList(Of Secondary)

    ' and its matching stack for Interpreter Pointers
    Private _IPSTK As New StackList(Of Integer)

    ' the current program's Interpreter Pointer
    Private _IP As Integer = 0

    ' a temporary way to end the current instance of RPNET
    Private quit As Boolean = False

    ' a temporary way to implement definite loops
    Private _LOOPstart, _LOOPend, _LOOPindex, _LOOPip As Integer
    Private _LOOPSTK As New StackList(Of Integer)

    ' a basic piece of code for RPL, skips over the current object
    Sub SkipOb()
        Dim depth As Integer = 0
        Dim endIndex As Integer = _RS.Count - 1
        Do
            Dim ob As Object = _RS(_IP)
            _IP += 1
            If ob.Equals(_DoSemi) Then ' it means we just found the end of a composite object
                depth -= 1
            ElseIf ob.Equals(_DoCol) OrElse ob.Equals(_DoList) OrElse ob.Equals(_DoSym) Then ' it means we just found the start of a composite object
                depth += 1
            End If
        Loop While _IP < endIndex AndAlso depth > 0
        If _IP > endIndex AndAlso depth <> 0 Then Throw New Exception("unmatched semi") ' if so, it means there is an unmatched number of starts and ends
        ' this has the unwanted sideeffect that you can't do something like 
        '   :: ' ; ' ; ; 
        ' but that can be corrected later. It is a rare case where you want to push a bare semi on the stack
    End Sub

    Sub Main()

        'adds all the methods of this module that are marked with "RPLWord", to the dictionary
        For Each mi As MethodInfo In GetType(RPNETVB).GetMethods
            Dim asRPLWord As RPLWord = mi.GetCustomAttribute(GetType(RPLWord))
            If asRPLWord Is Nothing Then Continue For
            words.Add(If(asRPLWord.WordName.Length = 0, mi.Name, asRPLWord.WordName), mi.CreateDelegate(GetType(Action)))
        Next

        words.Add("}", GetType(RPNETVB).GetMethod("DoSemi").CreateDelegate(GetType(Action))) ' common for secondary/symbolic and list, otherwise one would have to write { # 1 # 2 ; instead of { # 1 # 2 }

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
            "Substring", ' have the ability to call methods, might as well use it.
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
