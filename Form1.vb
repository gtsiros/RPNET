Imports System.IO
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
		Sub Main(sender As Object, e As EventArgs) Handles MyBase.Load
			If File.Exists("../test.bin") Then
				Dim aFormatter As IFormatter = New BinaryFormatter
				Dim aStream As Stream = New FileStream("../test.bin", FileMode.Open, FileAccess.Read, FileShare.Read)
				rpn.deps = DirectCast(aFormatter.Deserialize(aStream), Dictionary(Of String, List(Of String)))
				aStream.Close()
				Dim types As Integer = 0
				Dim n As Long = Now.Ticks
				For Each a As List(Of String) In rpn.deps.Values
					types += a.Count
				Next
				Dim m As Long = Now.Ticks - n
				rpn.W("loaded " & rpn.deps.Count & " assemblies, defining " & types & " types in " & (m \ 10000) & " ms")
			End If

			'Do
			'	DispDataStack()
			'	Console.Write("> ")
			'	Dim cmdLine As String = R().Trim
			'	Try
			'		If cmdLine.Length = 0 Then DS.Push(DS.Peek) Else Eval(Parse(":: " & cmdLine & " ;"))
			'
			'	Catch ex As Exception
			'		W(ex.Message)
			'	End Try
			'Loop
			'Dim SecoToString As Func(Of Object, String) = Function(seco) seco.Aggregate(":: ", Function(a, b) a & IIf(TypeOf b Is Secondary, SecoToString(DirectCast(b, List(Of Object))), b.ToString) & " ", Function(c) c & " ;")

		End Sub
		'Dim W As Action(Of String) = Sub(s) tinf.Text = s
		Sub Cmd(Optional b As Boolean = True)
			'	Console.Write("> ")
			Dim cmdLine As String = tcmd.Text.Trim
			Try
				If cmdLine.Length = 0 Then
					rpn.DS.Push(rpn.DS.Peek)
				Else
					sw.Restart()
					rpn.Eval(rpn.Parse(":: " & cmdLine & vbLf & " ;"))
					rpn.W("exec: " & sw.ElapsedTicks.ToString & " ticks")
				End If
				cmd_history.Append(cmdLine & vbNewLine)
				If b Then tcmd.Clear()
			Catch ex As Exception
				rpn.W(ex.Message & "(" & ex.StackTrace.Replace(vbNewLine, " ").Replace(vbLf, " "c).Replace(vbCr, " "c) & ")")
			End Try
			DispDataStack()
		End Sub

		Sub PreviewKeyDown_tin(o As Object, e As PreviewKeyDownEventArgs) Handles tcmd.PreviewKeyDown
			If e.KeyCode = Keys.Tab Then e.IsInputKey = True
			If e.KeyCode = Keys.Down AndAlso tcmd.Text.Trim.Length = 0 Then
				tcmd.Text = rpn.ToStr(rpn.DS.Peek)
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
						Cmd(Not e.Control)
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
			BackColor = IIf(TopMost, Color.Black, Color.Gray)
		End Sub

		Private Sub HistoryToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HistoryToolStripMenuItem.Click
			tcmd.Text = cmd_history.ToString
		End Sub

	End Class
