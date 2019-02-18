Imports System.ComponentModel

Public Class TSimpleUI
    Friend cmdq As New Queue(Of String)

    Friend WithEvents Gbw As New BackgroundWorker With {.WorkerSupportsCancellation = True}

    Sub Foo() Handles Me.Load
        Me.inp.Select()
    End Sub

    Sub Kd(ob As Object, args As KeyEventArgs) Handles inp.KeyDown
        If args.KeyData <> (Keys.Return Or Keys.Shift) Then Exit Sub
        args.SuppressKeyPress = True
        Dim result As List(Of String) = Exec(Me.inp.Text)
        Me.hist.AppendText(Me.inp.Text & vbNewLine)
        Me.inp.Clear()
        Me.stk.Clear()
        If result.Count > 0 Then
            Dim padlength As Int32 = result.Count.ToString.Length ' haha.
            For i = result.Count To 1 Step -1
                Me.stk.AppendText(i.ToString.PadLeft(padlength, " ") & ": " & result(i - 1) & vbNewLine)
            Next
        Else
            Me.stk.AppendText("empty")
        End If
        Me.outp.AppendText(RPNETVB.outstring)
        args.Handled = True
    End Sub

    Sub Me_KeyDown(ob As Object, args As KeyEventArgs) Handles Me.KeyDown
        args.SuppressKeyPress = args.KeyData = Keys.F10
    End Sub

End Class