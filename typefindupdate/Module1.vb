Imports System.IO
Imports System.Reflection
Imports System.Reflection.Emit
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Text
Imports RPNET

Module typefindupdate

	Sub Main()
		Dim asmlist As New List(Of String)
		Dim pi As New ProcessStartInfo("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\gacutil.exe", "-l") With {
									.CreateNoWindow = True,
									.WindowStyle = ProcessWindowStyle.Hidden,
									.UseShellExecute = False,
									.RedirectStandardOutput = True,
									.RedirectStandardError = True}
		Dim pr As Process = Process.Start(pi)
		Dim asms As New Dictionary(Of String, AsmInfo)
		While Not pr.StandardOutput.EndOfStream
			Try
				While Not pr.StandardOutput.EndOfStream
					Dim l As String = pr.StandardOutput.ReadLine().Trim
					If l.Length < 4 Then Continue While
					Dim parts() As String = l.Split(New Char() {" "c, ","c}, StringSplitOptions.RemoveEmptyEntries)
					If parts.Length < 2 OrElse Not parts(1).StartsWith("Version") OrElse parts(0).Contains("DirectX") Then Continue While
					Dim culture As String = parts.FirstOrDefault(Function(s) s.StartsWith("Culture")).Split("="c)(1)
					If culture <> "en" AndAlso culture <> "neutral" Then Continue While
					Dim arch As String = parts.FirstOrDefault(Function(s) s.StartsWith("processor")).Split("="c)(1)
					If arch <> "MSIL" AndAlso arch <> "x86" Then Continue While
					Dim version As String = parts.FirstOrDefault(Function(s) s.StartsWith("Version")).Split("="c)(1)
					Dim nums() As Integer = Array.ConvertAll(version.Split("."c), Function(s) Integer.Parse(s))
					Dim bAdd As Boolean = True
					If asms.Keys.Contains(parts(0)) Then If RpNetLib.Higher(nums, asms(parts(0)).version) Then asms.Remove(parts(0)) Else bAdd = False
					If bAdd Then asms.Add(parts(0), New AsmInfo With {.Fullname = l, .version = nums})
				End While
			Catch ex As Exception
				Debug.WriteLine(ex.Message & vbNewLine & ex.StackTrace)
			End Try
		End While
		pr.WaitForExit()
		'Array.ForEach(asms.Keys.ToArray, Sub(s) W(s))
		Dim deps As New Dictionary(Of String, List(Of String))
		Dim sw As New Stopwatch
		sw.Start()
		Debug.WriteLine("loading " & asms.Count & " assemblies")


		Dim i As Integer = 0
		Dim failed As Integer = 0
		Dim total As Integer = asms.Count

		Do
			Try
				Do
					Dim asmname As String = asms.Keys(i)
					Dim asm As Assembly = Assembly.Load(asms(asmname).Fullname)
					deps.Add(asms(asmname).Fullname, Array.ConvertAll(asm.GetTypes(), Function(a) a.FullName).ToList)
					i += 1
					If sw.ElapsedMilliseconds > 1000 Then
						Debug.WriteLine(Math.Round(i / total * 100.0, 2) & " %")
						sw.Restart()
					End If
				Loop While i < total
			Catch ex As Exception
				failed += 1
				i += 1
			End Try
		Loop While i < total

		Dim aFormatter As IFormatter = New BinaryFormatter
		File.Delete("../test.bin")
		Dim aStream As Stream = New FileStream("../test.bin", FileMode.Create, FileAccess.Write, FileShare.None)
		aFormatter.Serialize(aStream, deps)
		aStream.Close()

	End Sub

End Module
