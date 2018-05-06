Imports System.ComponentModel

Public Class SimpleUI
    Friend cmdq As New Queue(Of String)

    Friend WithEvents Gbw As New BackgroundWorker With {.WorkerSupportsCancellation = True}

    Sub foo() Handles Me.Load
        inp.Select()
    End Sub

    Sub kd(ob As Object, args As KeyEventArgs) Handles inp.KeyDown
        If args.KeyData <> (Keys.Return Or Keys.Shift) Then Exit Sub
        args.SuppressKeyPress = True
        Dim result As List(Of String) = Exec(inp.Text)
        inp.Clear()
        stk.Clear()
        If result.Count > 0 Then
            Dim padlength As Integer = result.Count.ToString.Length ' haha.
            For i = result.Count To 1 Step -1
                stk.AppendText(i.ToString.PadLeft(padlength, " ") & ": " & result(i - 1) & vbNewLine)
            Next
        Else
            stk.AppendText("empty")
        End If
        outp.AppendText(RPNETVB.outstring)
        args.Handled = True
    End Sub

    Sub Me_KeyDown(ob As Object, args As KeyEventArgs) Handles Me.KeyDown
        args.SuppressKeyPress = args.KeyData = Keys.F10
    End Sub

End Class