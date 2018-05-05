Imports System.Threading

Public Class Form1
    Friend cmdq As New Queue(Of String)

    Friend seUp As New Semaphore(1, 1)
    Friend seDown As New Semaphore(0, 1)

    Sub foo() Handles Me.Load
        inp.Select()
    End Sub

    Sub kd(ob As Object, args As KeyEventArgs) Handles inp.KeyDown
        If args.KeyData = (Keys.Return Or Keys.Shift) Then
            args.SuppressKeyPress = True
        End If
    End Sub

End Class