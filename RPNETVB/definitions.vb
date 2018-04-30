
Imports System.Diagnostics

Partial Module RPNETVB

    <AttributeUsage(AttributeTargets.Method, Inherited:=False, AllowMultiple:=True)> Class RPLWord
        Inherits Attribute
        Public WordName As String
        Public Sub New(Optional name As String = "")
            WordName = name
        End Sub
    End Class

    Class StackList(Of T)
        Inherits List(Of T)
        <DebuggerStepThrough> Public Function Pop() As T
            Pop = MyBase.Item(0)
            MyBase.RemoveAt(0)
        End Function
        <DebuggerStepThrough> Public Function Peek() As T
            Peek = MyBase.Item(0)
        End Function
        <DebuggerStepThrough> Public Sub Push(v As T)
            MyBase.Insert(0, v)
        End Sub
    End Class

    MustInherit Class Composite
        Inherits List(Of Object)
    End Class

    Class Secondary
        Inherits Composite
        <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
            If l IsNot Nothing Then MyBase.AddRange(l)
        End Sub
    End Class

    Class Symbolic
        Inherits Composite
        <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
            If l IsNot Nothing Then MyBase.AddRange(l)
        End Sub
    End Class

    Class ObList ' i need an explicit list(of object) as a separate type/class
        Inherits Composite
        <DebuggerStepThrough> Sub New(Optional l As IEnumerable(Of Object) = Nothing)
            If l IsNot Nothing Then MyBase.AddRange(l)
        End Sub
    End Class

End Module
