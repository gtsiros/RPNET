Imports System.IO
Imports System.Reflection
Imports System.Reflection.Emit
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary


Public Class Form1
	Enum Tok
		CurlyOpen
		CurlyClose
		SecondaryOpen
		SecondaryClose
		BracketOpen
		BracketClose
		ParenOpen
		ParenClose
		word
		delim_bint
		delim_single
		delim_double
		delim_cstring
		delim_identifier
		none
	End Enum

	Dim delims As New Dictionary(Of String, Tok) From {{"::", Tok.SecondaryOpen}, {";", Tok.SecondaryClose}, {"{", Tok.CurlyOpen}, {"}", Tok.CurlyClose}, {"(", Tok.ParenOpen}, {")", Tok.ParenClose}, {"[", Tok.BracketOpen}, {"]", Tok.BracketClose}, {"#", Tok.delim_bint}, {"%", Tok.delim_single}, {"%%", Tok.delim_double}, {"$", Tok.delim_cstring}, {"id", Tok.delim_identifier}}
	Dim types As New Dictionary(Of Tok, Type) From {{Tok.delim_bint, GetType(Integer)}, {Tok.delim_single, GetType(Single)}, {Tok.delim_double, GetType(Double)}, {Tok.delim_cstring, GetType(String)}, {Tok.delim_identifier, GetType(String)}}

	Dim itypes As New Dictionary(Of Type, Integer) From {{GetType(Integer), 0}, {GetType(Single), 1}, {GetType(Double), 2}, {GetType(String), 3}, {GetType(Secondary), 4}, {GetType(Type), 5}, {GetType(Word), 6}, {GetType(Boolean), 7}}


	Dim DS As New Stack(Of Object)
	Dim undoDS As New Stack(Of Object)
	Dim RS As New Stack(Of StackFrame)

	<Serializable> Class Identifier
		Public name As String
	End Class

	<Serializable> Class Secondary
		Inherits List(Of Object)
		Shared Shadows Operator +(ByVal a As Secondary, ByVal b As Secondary) As Secondary
			Dim foo As New Secondary
			foo.AddRange(a)
			foo.AddRange(b)
			Return foo
		End Operator
	End Class

	<Serializable> Structure Word
		Public Identifier As String
		Public Code As Object
		Sub New(identifier As String, code As Object)
			Me.Identifier = identifier
			Me.Code = code
		End Sub
	End Structure

	Dim vars As New Dictionary(Of String, Object)
	Dim loops As New Stack(Of Integer)
	' $ forms.form findtypes # 1 get innercomp drop import new 

	Dim words As New List(Of Word) From {
		New Word("true", Sub() DS.Push(True)),
		New Word("false", Sub() DS.Push(False)),
		New Word("[]", Sub() DS.Push(Array.CreateInstance(DS.Pop, 0))),
		New Word("[]n", Sub()
							Dim element_count As Integer = DS.Pop
							Dim new_array As Array
							If element_count > 0 Then new_array = Array.CreateInstance(DS.Peek.GetType, element_count) Else new_array = Array.CreateInstance(GetType(Object), 0)
							While element_count > 0
								new_array.SetValue(DS.Pop, element_count - 1)
								element_count -= 1
							End While
							DS.Push(new_array)
						End Sub),
		New Word("redim", Sub()
							  Dim new_length As Integer = DS.Pop
							  Dim o As Array = DS.Pop
							  Dim old_length As Integer = o.Length
							  Dim c As Array = Array.CreateInstance(o.GetType.GetElementType(), new_length)
							  If Not new_length < old_length Then o.CopyTo(c, 0)
							  DS.Push(c)
						  End Sub),
		New Word("@i", Sub() DS.Push(loops.Peek)),
		New Word("@n", Sub() DS.Push(loops(DS.Pop))),
		New Word("end", Sub() End),
		New Word("=", Sub() DS.Push(DS.Pop.Equals(DS.Pop))),
		New Word("chr", Sub() DS.Push(Chr(DS.Pop()).ToString)),
		New Word("clear", Sub() DS.Clear()),
		New Word("depth", Sub() DS.Push(DS.Count)),
		New Word("drop", Sub() DS.Pop()),
		New Word("dup", Sub() DS.Push(DS.Peek)),
		New Word("eval", Sub() Eval(DS.Pop)),
		New Word("load", Sub() DS.Push(Assembly.LoadFile(Directory.GetCurrentDirectory() & "\" & DirectCast(DS.Pop, String)))),
		New Word("type", Sub() DS.Push(DS.Pop.GetType)),
		New Word("ift", Sub() If DS.Pop Then Eval(DS.Pop) Else DS.Pop()),
		New Word("new", Sub() DS.Push(Activator.CreateInstance(Type.GetType(DS.Pop)))),
		New Word("num", Sub() DS.Push(Asc(DS.Pop()))),
		New Word("print", Sub() W(ToStr(DS.Pop()))),
		New Word("stop", Sub() Stop),
		New Word("strto", Sub() DS.Push(Parse(DS.Pop))),
		New Word("tostr", Sub() DS.Push(ToStr(DS.Pop))),
		New Word("words", Sub() DS.Push(words.ConvertAll(Function(w As Word) CType(w, Object)))),
		New Word("!", Sub()
						  Dim name As Object = DS.Pop
						  Dim obj As Object = DS.Pop
						  Dim t As Type = name.GetType

						  Select Case t
							  Case GetType(String)
								  CallByName(DS.Peek, name, CallType.Set, New Object() {obj})
							  Case GetType(Identifier)
								  Dim id As String = DirectCast(name, Identifier).name
								  If vars.ContainsKey(id) Then vars(id) = obj Else vars.Add(id, obj)
							  Case GetType(Integer)
								  Dim collection_type As Type = DS.Peek.GetType
								  Select Case collection_type
									  Case GetType(Array)
										  Dim a As Array = DS.Peek
										  a.SetValue(name, obj)
									  Case GetType(List(Of Object))
										  Dim l As List(Of Object) = DS.Peek
										  l.RemoveAt(name)
										  l.Insert(name, obj)
									  Case GetType(Secondary)
										  Dim s As Secondary = DS.Peek
										  s.RemoveAt(name)
										  s.Insert(name, obj)
								  End Select
						  End Select


					  End Sub),
		New Word("@", Sub()
						  Dim t As Type = DS.Peek.GetType
						  Select Case t
							  Case GetType(String)
								  Dim pname As String = DS.Pop
								  DS.Push(DS.Peek.GetType.InvokeMember(pname, BindingFlags.GetProperty, Nothing, DS.Peek, Nothing))
							  Case GetType(Identifier)
								  Dim id As Identifier = DS.Pop
								  If vars.ContainsKey(id.name) Then DS.Push(vars(id.name)) Else DS.Push(Nothing)
							  Case GetType(Word)
								  Dim w As Word = DS.Pop
								  DS.Push(w.Code)
							  Case GetType(Integer)
								  Dim i As Integer = DS.Pop
								  Dim collection_type As Type = DS.Peek.GetType
								  Select Case collection_type
									  Case GetType(Array)
										  Dim a As Array = DS.Pop
										  DS.Push(a.GetValue(i))
									  Case GetType(List(Of Object))
										  Dim l As List(Of Object) = DS.Pop
										  DS.Push(l(i))
									  Case GetType(Secondary)
										  Dim s As Secondary = DS.Pop
										  DS.Push(s(i))
								  End Select
						  End Select
					  End Sub),
		New Word("vars", Sub()
							 Dim vl As New List(Of Object)
							 For Each k As String In vars.Keys
								 vl.Add(New Identifier With {.name = k})
							 Next
							 DS.Push(vl)
						 End Sub),
		New Word("methods", Sub()
								Dim mis() As MethodInfo = DS.Pop.GetType.GetRuntimeMethods()
								Dim misl As List(Of MethodInfo) = mis.ToList
								DS.Push(misl.ConvertAll(Function(a) CType(a, Object)))
							End Sub),
		New Word("method", Sub()
							   Dim mname As String = DS.Pop
							   DS.Push(DS.Pop.GetType.GetRuntimeMethods().FirstOrDefault(Function(mis) mis.Name.ToLower = mname))
						   End Sub),
		New Word("properties", Sub()
								   Dim pis() As PropertyInfo = DS.Pop.GetType.GetRuntimeProperties
								   Dim pisl As List(Of PropertyInfo) = pis.ToList
								   DS.Push(pisl.ConvertAll(Function(a) CType(a, Object)))
							   End Sub),
		New Word("property", Sub()
								 Dim pname As String = DS.Pop
								 DS.Push(DS.Peek.GetType.GetRuntimeProperties.FirstOrDefault(Function(pis) pis.Name.ToLower = pname))
							 End Sub),
		New Word("members", Sub()
								Dim mis() As MemberInfo = DS.Pop.GetType.GetMembers()
								Dim misl As List(Of MemberInfo) = mis.ToList
								DS.Push(misl.ConvertAll(Function(a) CType(a, Object)))
							End Sub),
		New Word("import", Sub()
							   Dim asm As Assembly = Assembly.Load(DS.Pop) 'DS.Push(asm.GetTypes.ToList.ConvertAll(Of Object)(Function(a) CType(a, Object)))
						   End Sub),
		New Word("read", Sub() If File.Exists(DS.Peek) Then DS.Push(File.ReadAllText(DS.Pop)) Else DS.Push("no such file")),
		New Word("write", Sub()
							  Dim fname As String = DS.Pop
							  If File.Exists(fname) Then DS.Push("file exists") Else File.WriteAllText(fname, DS.Pop)
						  End Sub),
		New Word("reinit", Sub()
							   Dim asmlist As New List(Of String)
							   Dim pi As New ProcessStartInfo("C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\gacutil.exe", "-l") With {
									.CreateNoWindow = True,
									.WindowStyle = ProcessWindowStyle.Hidden,
									.UseShellExecute = False,
									.RedirectStandardOutput = True,
									.RedirectStandardError = True}
							   Dim pr As Process = Process.Start(pi)
							   Dim asm As Assembly = Nothing
							   deps = New Dictionary(Of String, List(Of String))
							   While Not pr.StandardOutput.EndOfStream
								   Try
									   Dim l As String = pr.StandardOutput.ReadLine().Trim
									   If l.Contains("irectX") Then Continue While
									   asm = Assembly.Load(l)
									   Dim tys As New List(Of String)
									   For Each ty In asm.GetTypes
										   tys.Add(ty.AssemblyQualifiedName)
									   Next
									   deps.Add(asm.FullName, tys)
									   W(asm.FullName)
								   Catch ex As Exception
									   W(ex.Message)
								   End Try
							   End While
							   pr.WaitForExit()
							   Dim aFormatter As IFormatter = New BinaryFormatter
							   File.Delete("../test.bin")
							   Dim aStream As Stream = New FileStream("../test.bin", FileMode.Create, FileAccess.Write, FileShare.None)
							   aFormatter.Serialize(aStream, deps)
							   aStream.Close()
						   End Sub),
		New Word With {
			.Identifier = "findtypes",
			.Code = Sub()
						Dim wantedTypeName As String = DirectCast(DS.Pop, String).ToLower
						Dim tl As New List(Of Object)
						For Each one In deps.Keys
							Dim iot As Integer = deps(one).FindIndex(Function(l) l.Substring(0, l.IndexOf(","c)).ToLower.EndsWith(wantedTypeName))
							If iot >= 0 Then tl.Add(New List(Of Object) From {deps(one)(iot), one})
						Next
						DS.Push(tl)
					End Sub
		},
		New Word With {
			.Identifier = "until",
			.Code = Sub()
						Dim o As Object = DS.Pop
						loops.Push(0)
						Do
							Eval(o)
							loops.Push(loops.Pop + 1)
						Loop Until DS.Pop
						loops.Pop()
					End Sub
		},
		New Word With {
			.Identifier = "while",
			.Code = Sub()
						Dim o As Object = DS.Pop
						loops.Push(0)
						Do
							Eval(o)
							loops.Push(loops.Pop + 1)
						Loop While DS.Pop
						loops.Pop()
					End Sub
		},
		New Word With {
			.Identifier = "for",
			.Code = Sub()
						Dim iEnd As Integer = DS.Pop
						Dim iStart As Integer = DS.Pop
						Dim o As Object = DS.Pop
						loops.Push(iStart)
						For i As Integer = iStart To iEnd
							Eval(o)
							loops.Push(loops.Pop + 1)
						Next
						loops.Pop()
					End Sub
		},
		New Word With {
			.Identifier = "seq",
			.Code = Sub()
						Dim iEnd As Integer = DS.Pop
						Dim iStart As Integer = DS.Pop
						Dim o As Object = DS.Pop
						loops.Push(iStart)
						Dim nl As New List(Of Object)
						For i As Integer = iStart To iEnd
							Dim depthBefore As Integer = DS.Count
							Eval(o)
							Dim depthAfter As Integer = DS.Count
							If depthAfter - depthBefore = 1 Then
								nl.Add(DS.Pop)
							ElseIf depthAfter - depthBefore > 1 Then
								Dim sl As New List(Of Object)
								For j As Integer = depthBefore To depthAfter - 1
									sl.Add(DS.Pop())
								Next
								sl.Reverse()
								nl.Add(sl)
							End If
							loops.Push(loops.Pop + 1)
						Next
						loops.Pop()
						DS.Push(nl)
					End Sub
		},
		New Word With {
			.Identifier = "toqualifiedname",
			.Code = Sub()
						Dim wantedTypeName As String = DirectCast(DS.Pop, String).ToLower
						Dim t As String = ""
						For Each one In deps.Keys
							t = deps(one).FirstOrDefault(Function(l) l.Substring(0, l.IndexOf(","c)).ToLower.EndsWith(wantedTypeName))
							If t <> "" Then Exit For
						Next
						If t <> "" Then DS.Push(t) Else DS.Push(Nothing)
					End Sub
		},
		New Word With {
			.Identifier = "invoke",
			.Code = Sub()
						Dim method_name As String = DS.Pop
						Dim argument_list As List(Of Object) = DS.Pop
						Dim argument_types() As Type = argument_list.ConvertAll(Of Type)(Function(o) o.GetType).ToArray
						'convert list to array
						Dim argument_array() As Object = argument_list.ToArray
						Dim object_type As Type = DS.Peek.GetType
						Dim method_info As MethodInfo = object_type.GetRuntimeMethod(method_name, argument_types)
						If method_info Is Nothing Then
							DS.Push("no such method")
							Exit Sub
						End If
						Dim return_object As Object = object_type.InvokeMember(method_name, BindingFlags.InvokeMethod, Nothing, DS.Peek, argument_array)
						'find if it returns anything
						If method_info.ReturnType <> GetType(System.Void) Then DS.Push(return_object)
					End Sub
		},
		New Word With {
			.Identifier = "purge",
			.Code = Sub()
						Dim id As Identifier = DS.Pop
						If vars.ContainsKey(id.name) Then vars.Remove(id.name)
					End Sub
	},
		New Word With {
			.Identifier = "pick",
			.Code = Sub()
						Dim l As Integer = DS.Pop
						DS.Push(DS(l - 1))
					End Sub
		},
		New Word With {
			.Identifier = "ifte",
			.Code = Sub()
						Dim b As Boolean = DS.Pop
						Dim oFalse As Object = DS.Pop
						Dim oTrue As Object = DS.Pop
						If b Then Eval(oTrue) Else Eval(oFalse)
					End Sub
		},
		New Word With {
			.Identifier = "innercomp",
			.Code = Sub()
						Dim o As List(Of Object) = DS.Pop
						o.ForEach(Sub(i) DS.Push(i))
						DS.Push(o.Count)
					End Sub
		},
		New Word With {
			.Identifier = "{}n",
			.Code = Sub()
						Dim nl As New List(Of Object)
						Dim count As Integer = DS.Pop
						nl.AddRange(DS.Take(count).Reverse)
						While count > 0
							DS.Pop()
							count -= 1
						End While
						DS.Push(nl)
					End Sub
		},
		New Word With {
			.Identifier = "::n",
			.Code = Sub()
						Dim ns As New Secondary
						Dim count As Integer = DS.Pop
						ns.AddRange(DS.Take(count).Reverse)
						If count > 0 Then
							For i As Integer = 0 To count - 1
								DS.Pop()
							Next
						End If
						DS.Push(ns)
					End Sub
		},
		New Word("add", Sub()
							Dim l0 As Object = DS.Pop
							Dim l1 As Object = DS.Pop
							DS.Push(l1 + l0)
						End Sub),
		New Word("sub", Sub()
							Dim l0 As Object = DS.Pop
							Dim l1 As Object = DS.Pop
							DS.Push(l1 - l0)
						End Sub),
		New Word("mul", Sub()
							Dim l0 As Object = DS.Pop
							Dim l1 As Object = DS.Pop
							DS.Push(l1 * l0)
						End Sub),
		New Word("div", Sub()
							Dim l0 As Object = DS.Pop
							Dim l1 As Object = DS.Pop
							DS.Push(l1 / l0)
						End Sub),
		New Word("'", Sub()
						  Dim currentFrame As StackFrame = RS.Peek
						  DS.Push(currentFrame.Secondary(currentFrame.I))
						  currentFrame.I += 1
					  End Sub),
		New Word With {
			.Identifier = "swap",
			.Code = Sub()
						Dim l0 As Object = DS.Pop
						Dim l1 As Object = DS.Pop
						DS.Push(l0)
						DS.Push(l1)
					End Sub
		},
		New Word("dolist", Sub()
							   Dim act As Object = DS.Pop
							   Dim lcount As Integer = DS.Pop
							   If lcount <> 1 Then Throw New Exception("not implemented for lcount <> 1")
							   Dim input_list As List(Of Object) = DirectCast(DS.Pop, List(Of Object))
							   Dim output_list As New List(Of Object)
							   For i As Integer = 0 To input_list.Count - 1
								   Dim depth_before As Integer = DS.Count
								   DS.Push(input_list(i))
								   Eval(act)
								   Dim depth_after As Integer = DS.Count
								   If depth_after - depth_before > 1 Then
									   Dim sub_result_list As New List(Of Object)
									   sub_result_list.AddRange(DS.Take(depth_after - depth_before))
									   output_list.Add(sub_result_list)
								   ElseIf depth_after - depth_before = 1 Then
									   output_list.Add(DS.Pop)
								   End If
								   While DS.Count > depth_before
									   DS.Pop()
								   End While
							   Next
							   If output_list.Count > 0 Then DS.Push(output_list)
						   End Sub),
		New Word With {
		.Identifier = "define",
		.Code = Sub()
					words.Add(New Word(DS(0), DS(1)))
					DS.Pop()
					DS.Pop()
				End Sub
		}
	}
	Dim aFormatter As IFormatter = New BinaryFormatter

	<Serializable> Class StackFrame
		Sub New(i As Integer, o As Secondary)
			Me.I = i
			Me.Secondary = o
		End Sub
		Public Secondary As Secondary
		Public I As Integer = 0
	End Class

	'Dim R As Func(Of String) = Function() Console.ReadLine
	Dim W As Action(Of String) = Sub(s) tinf.Text = s

	Sub DispDataStack()
		'W("data stack:")
		tstk.Clear()
		If DS.Count = 0 Then
			tstk.Text = "0:"
		Else
			For i As Integer = DS.Count - 1 To 0 Step -1
				Dim str As String = ToStr(DS(i))
				'If str.Length > 100 Then str = str.Substring(0, 100) & "..."
				tstk.AppendText((i + 1) & ": " & str & vbNewLine)
			Next
		End If
	End Sub

	Function TypeToInt(ByRef o As Object) As Integer
		TypeToInt = -1

	End Function
	Sub DispReturnStack()
		W("return stack:")
		If RS.Count = 0 Then
			W("empty")
		Else
			For i As Integer = DS.Count - 1 To 0
				W(i & ": " & ToStr(RS(i)))
			Next
		End If
	End Sub

	Function ToStr(ob As Object) As String

		ToStr = ""
		If ob Is Nothing Then
			ToStr = "<nothing>"
		ElseIf TypeOf ob Is Secondary Then
			ToStr = DirectCast(ob, Secondary).Aggregate(":: ", Function(a, b) a & ToStr(b) & " ", Function(c) c & ";")
		ElseIf TypeOf ob Is String Then
			ToStr = "$ " & DirectCast(ob, String).Replace("_", "\_").Replace(" "c, "_"c)
		ElseIf TypeOf ob Is List(Of Object) Then
			'ElseIf GetType(IList).IsAssignableFrom(ob.GetType) Then
			Dim l As List(Of Object) = DirectCast(ob, List(Of Object))
			ToStr = l.Take(IIf(l.Count > 10, 10, l.Count)).Aggregate("{ ", Function(a, b) a & ToStr(b) & " ", Function(c) c & IIf(l.Count > 10, "(+ " & l.Count - 10 & " more)", "") & "}")
		ElseIf TypeOf ob Is StackFrame Then
			Dim sf As StackFrame = DirectCast(ob, StackFrame)
			ToStr = ToStr(sf.Secondary) & ", " & sf.I & " (" & ToStr(sf.Secondary(sf.I))
		ElseIf TypeOf ob Is Word Then
			ToStr = DirectCast(ob, Word).Identifier
		ElseIf TypeOf ob Is Integer Then
			ToStr = "# " & ob.ToString
		ElseIf TypeOf ob Is Single Then
			ToStr = "% " & ob.ToString
		ElseIf TypeOf ob Is MethodInfo Then
			ToStr = DirectCast(ob, MethodInfo).ToString
		ElseIf TypeOf ob Is Double Then
			ToStr = "%% " & ob.ToString
		ElseIf TypeOf ob Is Identifier Then
			ToStr = "id " & DirectCast(ob, Identifier).name
		ElseIf TypeOf ob Is Array Then
			Dim a As Array = ob
			ToStr = "[ "
			Dim i As Integer = a.Length
			While i > 0
				ToStr &= ToStr(a.GetValue(i - 1)) & " "
				i -= 1
			End While
			ToStr &= "]"
		Else
			ToStr = ob.ToString & "[" & ob.GetType.Name & "]"
		End If
	End Function

	Sub Eval(p0 As Object)
		'DispDataStack()
		'DispReturnStack()
		Dim t As Type = p0.GetType
		If t.BaseType Is GetType(MulticastDelegate) Then
			DirectCast(p0, MulticastDelegate).DynamicInvoke()
		ElseIf t = GetType(Secondary) Then
			Dim seco As Secondary = DirectCast(p0, Secondary)
			If seco.Count = 0 Then Exit Sub
			RS.Push(New StackFrame(0, seco))
			Dim currentStackFrame As StackFrame = RS.Peek
			Dim O As Object
			Do
				O = currentStackFrame.Secondary(currentStackFrame.I)
				currentStackFrame.I += 1
				Eval(O)
			Loop While currentStackFrame.I < currentStackFrame.Secondary.Count
			RS.Pop()
		ElseIf t = GetType(Word) Then
			Eval(DirectCast(p0, Word).Code)
		ElseIf t = GetType(DynamicMethod) Then
			Stop
		ElseIf t = GetType(Identifier) Then
			Eval(vars(DirectCast(p0, Identifier).name))
		Else
			DS.Push(p0)
		End If
	End Sub

	Sub ExecStep()
		DispDataStack()
	End Sub

	Function WhatIs(s As String) As Tok
		WhatIs = Tok.none
		If words.FindIndex(Function(w) w.Identifier = s) >= 0 Then Return Tok.word
		If delims.ContainsKey(s) Then Return delims(s)
	End Function

	Sub AppendToTopLevel(ob As Object)
		If st.Count > 0 Then
			If TypeOf st.Peek Is Secondary Then DirectCast(st.Peek, Secondary).Add(ob) Else DirectCast(st.Peek, List(Of Object)).Add(ob)
		End If
	End Sub

	Dim st As Stack(Of Object)

	Function Parse(src As String) As Secondary
		Parse = New Secondary()
		Dim tokens As New List(Of String)
		tokens.AddRange(src.Split(New String() {vbNewLine, vbLf, vbCr, " "c, vbTab}, StringSplitOptions.RemoveEmptyEntries))
		st = New Stack(Of Object)
		If tokens.Count > 0 Then
			Dim pos As Integer = 0
			st.Push(Parse)
			Dim expect As Tok = Tok.none
			Try
				While pos < tokens.Count
					If expect <> Tok.none Then
						Try
							Dim o As Object
							If expect = Tok.delim_cstring Then
								o = tokens(pos).Replace("\_", vbLf).Replace("_"c, " "c).Replace(vbLf, "_")
							ElseIf expect = Tok.delim_identifier Then
								o = New Identifier With {.name = tokens(pos)}
							Else
								o = CTypeDynamic(tokens(pos), types(expect))
							End If
							If o Is Nothing Then Throw New Exception()
							AppendToTopLevel(o)
						Catch ex As Exception
							Throw New Exception("can Not represent a " & types(expect).ToString)
						End Try
						expect = Tok.none
					Else
						Dim wi As Tok = WhatIs(tokens(pos))
						Select Case wi
							Case Tok.CurlyOpen
								st.Push(New List(Of Object))
							Case Tok.SecondaryOpen
								st.Push(New Secondary)
							Case Tok.CurlyClose
								If TypeOf st.Peek IsNot List(Of Object) Then Throw New Exception("mismatched list")
								AppendToTopLevel(st.Pop)
							Case Tok.SecondaryClose
								If TypeOf st.Peek IsNot Secondary Then Throw New Exception("mismatched secondary")
								AppendToTopLevel(st.Pop)
							Case Tok.BracketOpen
							Case Tok.BracketClose
							'	If TypeOf st.Peek IsNot Array Then Throw New Exception("mismatched array")
							Case Tok.ParenOpen
							Case Tok.ParenClose
							Case Tok.delim_bint, Tok.delim_single, Tok.delim_double, Tok.delim_cstring, Tok.delim_identifier
								expect = wi
							Case Tok.word
								Dim asWord As Word = words.FirstOrDefault(Function(w) w.Identifier = tokens(pos))
								AppendToTopLevel(asWord)
							Case Tok.none
								Throw New Exception("unknown term")
						End Select
					End If
					pos += 1
				End While

				If st.Count = 0 Then
					Throw New Exception("something went terribly wrong")
				ElseIf st.Count > 1 Then
					Dim resp As String = ""
					Do
						resp &= ToStr(st.Pop) & vbNewLine
					Loop While st.Count > 0
					Throw New Exception(resp)
				End If
			Catch ex As Exception
				W(tokens(pos) & "(" & pos & ") " & ex.Message)
			End Try
		End If
	End Function
	Dim deps As Dictionary(Of String, List(Of String))

	Sub Main(sender As Object, e As EventArgs) Handles MyBase.Load
		If File.Exists("../test.bin") Then
			Dim aFormatter As IFormatter = New BinaryFormatter
			Dim aStream As Stream = New FileStream("../test.bin", FileMode.Open, FileAccess.Read, FileShare.Read)
			deps = DirectCast(aFormatter.Deserialize(aStream), Dictionary(Of String, List(Of String)))
			aStream.Close()
			Dim types As Integer = 0
			For Each a As List(Of String) In deps.Values
				types += a.Count
			Next
			W("loaded " & deps.Count & " assemblies, defining " & types & " types")
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
	Public Function GetAssemblyForType(typeName As String) As Assembly
		Return AppDomain.CurrentDomain.GetAssemblies.FirstOrDefault(Function(a) a.GetType(typeName, False, True) IsNot Nothing)
	End Function
	Function FindAssembliesForType(typeName As String) As String()
		If Not typeName.StartsWith(".") Then typeName = "." & typeName
		For Each one In deps.Keys
			Dim iot As Integer = deps(one).FindIndex(Function(l) l.EndsWith(typeName))
			If iot >= 0 Then
				W(deps(one)(iot) & " found in " & one)
			End If
		Next

		Return Nothing
	End Function

	Sub Cmd(Optional b As Boolean = True)
		'	Console.Write("> ")
		Dim cmdLine As String = tcmd.Text.Trim
		Try
			If cmdLine.Length = 0 Then DS.Push(DS.Peek) Else Eval(Parse(":: " & cmdLine & " ;"))
			If b Then tcmd.Clear()
		Catch ex As Exception
			W(ex.Message)
		End Try
		DispDataStack()
	End Sub

	Sub PreviewKeyDown_tin(o As Object, e As PreviewKeyDownEventArgs) Handles tcmd.PreviewKeyDown
		If e.KeyCode = Keys.Tab Then e.IsInputKey = True
		If e.KeyCode = Keys.Down AndAlso tcmd.Text.Trim.Length = 0 Then
			tcmd.Text = ToStr(DS.Peek)
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


End Class
