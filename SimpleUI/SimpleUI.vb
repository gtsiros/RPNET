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
        inp.Clear()
        outp.Clear()
        args.Handled = True
    End Sub

    Sub Me_KeyDown(ob As Object, args As KeyEventArgs) Handles Me.KeyDown
        args.SuppressKeyPress = args.KeyData = Keys.F10
    End Sub

End Class