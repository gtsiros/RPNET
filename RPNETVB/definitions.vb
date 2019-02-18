Imports System
Imports System.Collections.Generic
Imports System.Diagnostics

Partial Module RPNETVB

    <AttributeUsage(AttributeTargets.Method, Inherited:=False, AllowMultiple:=False)> Class TRPLWord
        Inherits Attribute
        Public wordName As String
        Public Sub New(Optional name As String = "")
            Me.wordName = name
        End Sub
    End Class

    Class TStackList(Of T)
        Inherits List(Of T)
        <DebuggerStepThrough> Public Function Pop() As T
            Pop = Me.Item(0)
            Me.RemoveAt(0)
        End Function
        <DebuggerStepThrough> Public Function Peek() As T
            Peek = Me.Item(0)
        End Function
        <DebuggerStepThrough> Public Sub Push(v As T)
            Me.Insert(0, v)
        End Sub
    End Class

    Class TComposite
        Inherits List(Of Object)
        Public head As Action
        Public Shared Operator +(a As TComposite, b As Object) As TComposite
            a.Add(b)
            Return a
        End Operator
        Public Shared Operator +(a As TComposite, b As TComposite) As TComposite
            a.AddRange(b)
            Return a
        End Operator
        Public Shared Operator +(a As Object, b As TComposite) As TComposite
            b.Insert(0, a)
            Return b
        End Operator
        Sub New(head As Action, Optional l As IEnumerable(Of Object) = Nothing)
            Me.head = head
            If l IsNot Nothing Then Me.AddRange(l)
        End Sub
    End Class

    'Class Secondary
    '    Inherits Composite
    '    <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
    '        If l IsNot Nothing Then MyBase.AddRange(l)
    '    End Sub
    'End Class

    'Class Symbolic
    '    Inherits Composite
    '    <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
    '        If l IsNot Nothing Then MyBase.AddRange(l)
    '    End Sub
    'End Class

    'Class ObList ' i need an explicit list(of object) as a separate type/class
    '    Inherits Composite
    '    <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
    '        If l IsNot Nothing Then MyBase.AddRange(l)
    '    End Sub
    'End Class
End Module
