﻿Imports System.IO
Imports System.Reflection
Imports System.Reflection.Emit
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Text

Public Class Form1
    Dim rpn As New RpNetLib With {.W = Sub(s) tinf.Text = s}
    Dim cmd_history As New StringBuilder
    Dim aFormatter As IFormatter = New BinaryFormatter
    Dim sw As New Stopwatch
    Sub DispDataStack()
        'W("data stack:")
        tstk.Clear()
        If rpn.DS.Count = 0 Then
            tstk.Text = "0:"
        Else
            For i As Integer = rpn.DS.Count - 1 To 0 Step -1
                Dim str As String = rpn.ToStr(rpn.DS(i))
                'If str.Length > 100 Then str = str.Substring(0, 100) & "..."
                tstk.AppendText((i + 1) & ": " & str & vbNewLine)
            Next
        End If
    End Sub

    'Dim W As Action(Of String) = Sub(s) tinf.Text = s
    Sub Cmd()
        '	Console.Write("> ")
        Dim cmdLine As String = tcmd.Text.Trim
        Try
            If cmdLine.Length = 0 Then
                rpn.DS.Push(rpn.DS(0))
                cmd_history.Append("dup " & vbNewLine)
            Else
                cmd_history.Append(cmdLine & vbNewLine)
                sw.Restart()
                '  rpn.Eval(rpn.Parse(":: " & cmdLine & " ;"))
                rpn.Eval(rpn.Parse(cmdLine))
            End If
            tcmd.Clear()
        Catch ex As Exception
            rpn.W(ex.Message & "(" & ex.StackTrace.Replace(vbNewLine, " ").Replace(vbLf, " "c).Replace(vbCr, " "c) & ")")
        End Try
        DispDataStack()
    End Sub

    Sub PreviewKeyDown_tin(o As Object, e As PreviewKeyDownEventArgs) Handles tcmd.PreviewKeyDown
        If e.KeyCode = Keys.Tab Then e.IsInputKey = True
        If e.KeyCode = Keys.Down AndAlso tcmd.Text.Trim.Length = 0 Then
            tcmd.Text = rpn.ToStr(rpn.DS(0))
        End If
    End Sub

    Sub KeyUp_tcmd(o As Object, e As KeyEventArgs) Handles tcmd.KeyUp
        'Select Case e.KeyCode
        '	Case Keys.Tab
        '		If tcmd.SelectionLength = 0 Then
        '			e.Handled = True
        '
        '		End If
        'End Select
    End Sub

    Sub KeyDown_tcmd(o As Object, e As KeyEventArgs) Handles tcmd.KeyDown
        Select Case e.KeyCode
            Case Keys.Return
                If e.Shift Then
                    Cmd()
                    e.Handled = True
                End If
            Case Keys.Escape
                Close()
                e.Handled = True
                'DS = undoDS
                'DispDataStack()
        End Select

    End Sub
    Sub ToggleOnTop() Handles Me.Click
        TopMost = Not TopMost
        If TopMost Then BackColor = Color.Black Else BackColor = SystemColors.Control
    End Sub

    Private Sub HistoryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HistoryToolStripMenuItem.Click
        tcmd.Text = cmd_history.ToString
    End Sub

End Class
