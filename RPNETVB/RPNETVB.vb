Imports System.Reflection
Imports System.Diagnostics
Imports System.Text
Imports System.Windows
Imports System.IO

Public Module RPNETVB

    Public words As New Dictionary(Of String, Object) ' the list of commands supported by the runtime

    Friend W As Action(Of String) = Sub(s) outstring &= s & vbNewLine ' for changing the system input/outputs
    Friend R As Func(Of String) = Function() Console.ReadLine()

    Public _OB As Object ' the currently (or "next", depending how you view it ) executed OBject

    Public _DS As New StackList(Of Object) ' the Data Stack, it is a custom class that shares functionality of a stack and a list

    Public _RS As Composite ' the current RunStream object (always a program)

    Public _RSSTK As New StackList(Of Composite) ' the return stack for objects

    Public _IPSTK As New StackList(Of Integer) ' and its matching stack for Interpreter Pointers

    Public _IP As Integer = 0 ' the current program's Interpreter Pointer

    Public quit As Boolean = False ' a temporary way to end the current instance of RPNET

    Public _LOOPstart, _LOOPend, _LOOPindex, _LOOPip As Integer ' a temporary way to implement definite loops
    Public _LOOPSTK As New StackList(Of Integer)

    Function IsCompositeHead(ByRef ob As Object) As Boolean
        Return ob.Equals(_DoCol) OrElse ob.Equals(_DoList) OrElse ob.Equals(_DoSym)
    End Function

    Public Sub SkipOb() ' a basic piece of code for RPL, skips over the current object
        Dim depth As Integer = 0
        Dim endIndex As Integer = _RS.Count - 1
        Do
            ' the way this is handled, it is impossible to quote a semi ( like ' ; )
            ' not sure why you would want to do something like that
            ' but the basic idea is that you should be able to do it.
            ' you can't, not yet.
            Dim ob As Object = _RS(_IP)
            _IP += 1
            If ob.Equals(_DoSemi) Then ' it means we just found the end of a composite object
                depth -= 1
            ElseIf IsCompositeHead(ob) Then ' it means we just found the start of a composite object
                depth += 1
            End If
        Loop While _IP < endIndex AndAlso depth > 0
        If _IP > endIndex AndAlso depth <> 0 Then Throw New Exception("unmatched semi") ' if so, it means there is an unmatched number of starts and ends
    End Sub

    ' thinking of removing this and letting the user decide
    Const defaultOuterLoop As String = ":: begin read parse ' eval ' print ifte depth # 0 == ' :: $ ""0:"" print ; ' :: depth # 1 swap do depth # 1 + i? - pick tostr depth i? - tostr $ "": "" + swap + { # 2 } $ Substring @ print loop ; ifte again ;"
    Dim ExecCmdLine As Composite
    Sub New()
        'adds all the methods of this module that are marked with "RPLWord", to the dictionary
        For Each mi As MethodInfo In GetType(RPNETVB).GetMethods
            Dim asRPLWord As RPLWord = mi.GetCustomAttribute(GetType(RPLWord))
            If asRPLWord Is Nothing Then Continue For
            words.Add(If(asRPLWord.WordName.Length = 0, mi.Name, asRPLWord.WordName), mi.CreateDelegate(GetType(Action)))
        Next
        words.Add("}", GetType(RPNETVB).GetMethod("DoSemi").CreateDelegate(GetType(Action))) ' common for secondary/symbolic and list, otherwise one would have to write { # 1 # 2 ; instead of { # 1 # 2 }
        words.Add("true", True)
        words.Add("false", False)
        ExecCmdLine = New Composite(_DoCol, New Object() {words("parse"), words("'"), words("eval"), words("'"), words("print"), words("ifte"), words("end")})
    End Sub

    Public outstring As String = ""

    Function Exec(cmd As String) As List(Of String) ' for now the "outer loop" is in VB. 
        outstring = ""
        _DS.Push(cmd)
        _RS = ExecCmdLine
        _IP = 0
        _OB = _RS(0)
        Do ' this is the "inner loop"
            If TypeOf _OB Is Action Then
                DirectCast(_OB, Action)()
            ElseIf TypeOf _OB Is Composite Then ' for when a composite is evaluated from the data stack
                ' we evaluate its *head* without touching the _OB. that way the head can read the ob and do what it has to do
                DirectCast(_OB, Composite).head()
            Else
                _DS.Push(_OB)
                _IP += 1
                _OB = _RS(_IP)
            End If
        Loop Until quit
        quit = False
        Exec = _DS.ConvertAll(Function(ob) tostr(ob))
    End Function

End Module
