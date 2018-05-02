Imports System.ComponentModel
Imports System.IO
Imports System.Threading

Public Class SimpleUI
    Friend cmdq As New Queue(Of String)
    Friend WithEvents se As New Semaphore(0, 1)
    'Friend WithEvents up As New Semaphore(1, 1)
    'Friend WithEvents down As New Semaphore(0, 1)

    Friend WithEvents Gbw As New BackgroundWorker With {.WorkerSupportsCancellation = True}

    Sub foo() Handles Me.Load
        inp.Select()
        Gbw.RunWorkerAsync()
    End Sub

    Sub kd(ob As Object, args As KeyEventArgs) Handles inp.KeyDown
        If args.KeyData <> (Keys.Return Or Keys.Shift) Then Exit Sub
        args.SuppressKeyPress = True
        Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " handler waiting for semaphore")
        se.WaitOne()
        Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " handler got semaphore")

        SyncLock cmdq
            Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " handler in SyncLock")
            cmdq.Enqueue(inp.Text)
        End SyncLock
        Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " handler ended SyncLock")
        se.Release()
        Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " handler released semaphore")
        'ThreadPool.QueueUserWorkItem(AddressOf RplDo, New String(inp.Text))
        inp.Clear()
        outp.Clear()
        args.Handled = True
    End Sub

    Sub RplDo(ob As Object)
        Debug.WriteLine(ob.GetType.Name)
    End Sub

    Sub DoWork(ob As Object, args As DoWorkEventArgs) Handles Gbw.DoWork
        Dim cmdline As String = ""
        Dim done As Boolean = False
        Do
            Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker waiting for semaphore")
            se.WaitOne()

            Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker got semaphore")

            SyncLock cmdq
                Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker in SyncLock")
                If cmdq.Count > 0 Then
                    cmdline = cmdq.Dequeue
                    Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker found '" & cmdline.Trim & "'")
                Else
                    Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker found no string")
                End If
            End SyncLock
            Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker ended SyncLock")
            se.Release()
            Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker released semaphore")
        Loop Until done OrElse Gbw.CancellationPending

        If done Then Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker is done")
        If Gbw.CancellationPending Then Debug.WriteLine(Now.Ticks.ToString.PadLeft(20, " "c) & " worker was cancelled")

    End Sub

    Sub RunWorkerCompleted(ob As Object, args As RunWorkerCompletedEventArgs) Handles Gbw.RunWorkerCompleted

    End Sub

    Sub Me_KeyDown(ob As Object, args As KeyEventArgs) Handles Me.KeyDown
        args.SuppressKeyPress = args.KeyData = Keys.F10
    End Sub

    Sub FKeys(ob As Object, args As PreviewKeyDownEventArgs) Handles inp.PreviewKeyDown
        Debug.WriteLine("args.KeyCode: " & args.KeyCode.ToString & vbNewLine)
    End Sub

End Class