Imports System.IO
Imports System.Reflection
Imports System.Reflection.Emit
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Text

Public Structure AsmInfo
    Public Fullname As String
    Public version() As Integer
End Structure

Public Enum Tok
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

Public Enum Lex
    white
    cstri
    token
    escap
    comme
End Enum

Public Class RpNetLib
    Public Shared Function Higher(ByRef a() As Integer, ByRef b() As Integer) As Boolean
        Higher = False
        Dim undecided As Boolean = False
        Dim i As Integer = 0
        Do
            Higher = a(i) > b(i)
            undecided = a(i) = b(i)
            i += 1
        Loop While undecided AndAlso i < 4
    End Function



    Public deps As Dictionary(Of String, List(Of String))
    Dim delims As New Dictionary(Of String, Tok) From {{"::", Tok.SecondaryOpen}, {";", Tok.SecondaryClose}, {"{", Tok.CurlyOpen}, {"}", Tok.CurlyClose}, {"(", Tok.ParenOpen}, {")", Tok.ParenClose}, {"[", Tok.BracketOpen}, {"]", Tok.BracketClose}, {"#", Tok.delim_bint}, {"%", Tok.delim_single}, {"%%", Tok.delim_double}, {"$", Tok.delim_cstring}, {"id", Tok.delim_identifier}}
    Dim types As New Dictionary(Of Tok, Type) From {{Tok.delim_bint, GetType(Integer)}, {Tok.delim_single, GetType(Single)}, {Tok.delim_double, GetType(Double)}, {Tok.delim_cstring, GetType(String)}, {Tok.delim_identifier, GetType(String)}}
    Dim itypes As New Dictionary(Of Type, Integer) From {{GetType(Integer), 0}, {GetType(Single), 1}, {GetType(Double), 2}, {GetType(String), 3}, {GetType(Secondary), 4}, {GetType(Type), 5}, {GetType(Boolean), 7}}
    Dim escapes As New Dictionary(Of Char, Char) From {{"\"c, "\"c}, {"n"c, Chr(10)}, {"r"c, Chr(13)}, {"t"c, Chr(9)}, {""""c, """"c}}

    Public Class Flerm
        Inherits List(Of Object)

        Public Function Pop() As Object
            Pop = Item(0)
            RemoveAt(0)
        End Function
        Public Sub Drop(ByVal n As Integer)
            If n < 1 Then Exit Sub
            Do
                RemoveAt(0)
                n -= 1
            Loop While n > 0
        End Sub
        Public ReadOnly Property Peek As Object
            Get
                Peek = Item(0)
            End Get
        End Property
        Public Shadows Sub Push(ob As Object)
            Insert(0, ob)
        End Sub
        Public Sub Swap()
            Insert(0, Item(1))
            RemoveAt(2)
        End Sub
        Public Sub Rot()
            Insert(0, Item(2))
            RemoveAt(3)
        End Sub
        Public Sub Roll(i As Integer)
            Insert(0, Item(i - 1))
            RemoveAt(i)
        End Sub

    End Class

    Public DS As New Flerm
    Public RS As New Stack(Of StackFrame)

    Dim vars As New Dictionary(Of String, Object)
    Dim loops As New Stack(Of Integer)

    <Serializable> Class StackFrame
        Sub New(i As Integer, o As Secondary)
            Me.I = i
            Me.Secondary = o
        End Sub
        Public Secondary As Secondary
        Public I As Integer = 0
    End Class

    'Dim R As Func(Of String) = Function() Console.ReadLine

    <Serializable> Class Identifier
        Public name As String
        Sub New(idname As String)
            name = idname
        End Sub
    End Class

    <Serializable> Class Secondary
        Inherits List(Of Object)
        '		Implements ICloneable
        Sub New()
            MyBase.New
        End Sub
        Sub New(ByVal sec As IEnumerable)
            MyBase.New(sec)
        End Sub


        Shared Shadows Operator +(ByVal a As Secondary, ByVal b As Secondary) As Secondary
            Dim foo As New Secondary
            foo.AddRange(a)
            foo.AddRange(b)

            Return foo
        End Operator
    End Class

    <Serializable> Class Algebraic
        Inherits Secondary
    End Class

    '<Serializable> Structure Word
    '	Public Identifier As String
    '	Public Code As Object
    '	Sub New(identifier As String, code As Object)
    '		Me.Identifier = identifier
    '		Me.Code = code
    '	End Sub
    'End Structure


    Dim words As New Dictionary(Of String, Object) From { '$ system.io.directory totype { } $ GetCurrentDirectory @
        {"=", Sub() DS.Push(DS.Pop.Equals(DS.Pop))},
        {"?i", Sub() DS.Push(loops.Peek)},
        {"{}", New List(Of Object)},
        {"?n", Sub() DS.Push(loops(DirectCast(DS.Pop, Integer)))}, '{"[]", Sub() DS.Push(Array.CreateInstance(DS.Pop, 0))}, ' can be done in phase 2
        {"or", Sub() DS.Push(DS.Pop Or DS.Pop)},
        {"not", Sub() DS.Push(Not DS.Pop)},
        {"xor", Sub() DS.Push(DS.Pop Xor DS.Pop)},
        {"clear", Sub() DS.Clear()},
        {"depth", Sub() DS.Push(DS.Count)},
        {"dir", Sub() DS.Push(Directory.EnumerateFiles(Directory.GetCurrentDirectory).ToList.ConvertAll(Function(s) DirectCast(s, Object)))},
        {"drop", Sub() DS.Pop()},
        {"dup", Sub() DS.Push(DS.Peek)},
        {"end", Sub() Stop},
        {"errorout", Sub() Throw New Exception(DirectCast(DS.Pop, String))},
        {"eval", Sub() Eval(DS.Pop)},
        {"false", False}, ' -> true
        {"import", Sub() DS.Push(Assembly.Load(DirectCast(DS.Pop, String)))}, 'DS.Push(asm.GetTypes.ToList.ConvertAll(Of Object)(Function(a) CType(a, Object)))
        {"load", Sub() DS.Push(Assembly.LoadFile(Directory.GetCurrentDirectory() & "\" & DirectCast(DS.Pop, String)))},
        {"num", Sub() DS.Push(Asc(DirectCast(DS.Pop, Char)))},
        {"over", Sub() DS.Push(DS(1))},
        {"print", Sub() W(ToStr(DS.Pop()))},
        {"roll", Sub() DS.Roll(DirectCast(DS.Pop, Integer))},
        {"rot", Sub() DS.Rot()},
        {"self", Sub() DS.Push(Me)},
        {"stop", Sub() Stop},
        {"strto", Sub() DS.Push(Parse(DirectCast(DS.Pop, String)))},
        {"swap", Sub() DS.Swap()},
        {"tostr", Sub() DS.Push(ToStr(DS.Pop))}, '$ io.file totype { $ ..\..\extrawords.txt } $ ReadAllText @ strto eval
        {"throw", Sub() Throw New Exception(DirectCast(DS.Pop, String))}, '  -> true
        {"true", True}, '  -> true
        {"vars", Sub() DS.Push(Array.ConvertAll(vars.Keys.ToArray, Function(k) New Identifier(k)).Cast(Of Object).ToList())},
        {"words", Sub() DS.Push(words.Values.ToList)},
        {"ift", Sub() If DS(1) Then Eval(DS.Pop) Else DS.Pop()},
        {"[]n", Sub() ' ob.1 ... ob.n n -> array(ob1...obn)
                    If DS.Count - 1 < DS.Peek Then Throw New ArgumentException("bad argument count")
                    Dim new_array As Array = DS.Skip(1).Take(DirectCast(DS.Pop, Integer)).Reverse.ToArray
                    Eval(words("ndrop"))
                    DS.Push(new_array)
                End Sub},
        {"new", Sub()
                    Dim argument_list As List(Of Object)
                    If TypeOf DS.Peek Is List(Of Object) Then argument_list = DirectCast(DS.Pop, List(Of Object)) Else argument_list = New List(Of Object)
                    Dim found_type As Type = Nothing
                    If TypeOf DS.Peek Is String Then Eval(words("totype"))
                    If TypeOf DS.Peek Is Type Then found_type = DirectCast(DS.Pop, Type)
                    If found_type IsNot Nothing Then DS.Push(Activator.CreateInstance(found_type, argument_list.ToArray))
                End Sub},
        {"ndrop", Sub()
                      Dim i As Integer = DirectCast(DS.Pop, Integer)
                      While i > 0
                          DS.Pop()
                          i -= 1
                      End While
                  End Sub},
        {"!", Sub() ' ob str ->
                  Dim name As Object = DS.Pop
                  Dim obj As Object = DS.Pop
                  Dim t As Type = name.GetType
                  Select Case t
                      Case GetType(String) ' ob str -> 
                          CallByName(DS.Peek, DirectCast(name, String), CallType.Set, New Object() {obj})
                      Case GetType(Identifier) ' ob id -> 
                          Dim id As String = DirectCast(name, Identifier).name
                          If vars.ContainsKey(id) Then vars(id) = obj Else vars.Add(id, obj)
                      Case GetType(Integer) ' ob2 ob1 n -> ob2
                          Dim collection_type As Type = DS.Peek.GetType
                          Select Case collection_type
                              Case GetType(Array)
                                  DirectCast(DS.Peek, Array).SetValue(DirectCast(name, Integer), obj)
                              Case GetType(List(Of Object))
                                  Dim l As List(Of Object) = DirectCast(DS.Peek, List(Of Object))
                                  l.RemoveAt(name)
                                  l.Insert(name, obj)
                              Case GetType(Secondary)
                                  Dim s As Secondary = DS.Peek
                                  s.RemoveAt(name)
                                  s.Insert(name, obj)
                          End Select
                  End Select
              End Sub},
        {"?", Sub()
                  Dim t As Type = DS.Peek.GetType
                  Select Case t
                      Case GetType(Secondary)
                          DS.Push(New Secondary(DirectCast(DS.Pop, Secondary)))
                      Case GetType(String)
                          Dim pname As String = DS.Pop
                          Dim ob_type As Type
                          If TypeOf DS.Peek Is Type Then ob_type = DirectCast(DS.Peek, Type) Else ob_type = DS.Peek.GetType
                          DS.Push(ob_type.GetProperty(pname).GetValue(DS.Pop))
                      Case GetType(Identifier)
                          Dim id As Identifier = DS.Pop
                          If vars.ContainsKey(id.name) Then DS.Push(vars(id.name)) Else DS.Push(Nothing)
                      Case GetType(Integer)
                          Dim i As Integer = DS.Pop
                          DS.Push(DirectCast(DS.Pop, IEnumerable)(i))
                  End Select
              End Sub},
        {"@", Sub() ' change so that it does not check if type. otherwise can't call GetEvent etc
                  Dim method_name As String = DS.Pop
                  Dim argument_list As List(Of Object) = DS.Pop
                  Dim ob As Object = DS.Pop
                  Dim argument_array() As Object = argument_list.ToArray
                  Dim argument_types() As Type = Array.ConvertAll(argument_array, Function(o) o.GetType)
                  Dim object_type As Type
                  Dim bWasType As Boolean = TypeOf ob Is Type
                  If bWasType Then object_type = DirectCast(ob, Type) Else object_type = ob.GetType
                  Dim method_info As MethodInfo = object_type.GetRuntimeMethod(method_name, argument_types)
                  'Stop
                  If method_info Is Nothing Then Throw New Exception("no such method")
                  Dim return_object As Object
                  If bWasType Then return_object = method_info.Invoke(Nothing, argument_array) Else return_object = method_info.Invoke(ob, argument_array)
                  If method_info.ReturnType = GetType(Void) Then Exit Sub
                  If method_info.ReturnType = GetType(Array) AndAlso return_object IsNot Nothing Then DS.Push(New List(Of Object)(DirectCast(return_object, Array))) Else DS.Push(return_object)
              End Sub},
        {"disp", Sub()
                     Dim f As New Form() With {.FormBorderStyle = FormBorderStyle.SizableToolWindow}
                     Dim p = New PictureBox With {.Dock = DockStyle.Fill, .Image = DS.Pop, .SizeMode = PictureBoxSizeMode.CenterImage}
                     f.Controls.Add(p)
                     f.Show()
                 End Sub},
        {"reinit", Sub()
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
                                   If asms.Keys.Contains(parts(0)) Then If Higher(nums, asms(parts(0)).version) Then asms.Remove(parts(0)) Else bAdd = False
                                   If bAdd Then asms.Add(parts(0), New AsmInfo With {.Fullname = l, .version = nums})
                               End While
                           Catch ex As Exception
                               W(ex.Message & vbNewLine & ex.StackTrace)
                           End Try
                       End While
                       pr.WaitForExit()
                       'Array.ForEach(asms.Keys.ToArray, Sub(s) W(s))
                       Dim deps As New Dictionary(Of String, List(Of String))
                       Dim sw As New Stopwatch
                       sw.Start()
                       W("loading " & asms.Count & " assemblies")


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
                   End Sub},
        {"totype", Sub()
                       Dim wantedTypeName As String = DirectCast(DS.Peek, String).ToLower
                       For Each one In deps.Keys
                           Dim iot As Integer = deps(one).FindIndex(Function(l) l.ToLower.EndsWith(wantedTypeName))
                           If iot < 0 Then Continue For
                           DS.Pop()
                           DS.Push(Assembly.Load(one).GetType(deps(one)(iot)))
                           Exit Sub
                       Next
                       W("no such type")
                   End Sub},
        {"findtypes", Sub()
                          Dim wantedTypeName As String = DirectCast(DS.Pop, String).ToLower
                          Dim ls As New List(Of Object)
                          For Each one In deps.Keys
                              Dim arr As List(Of String) = deps(one).FindAll(Function(l) l.ToLower.EndsWith(wantedTypeName))
                              If arr.Count > 0 Then ls.AddRange(arr)
                          Next
                          DS.Push(ls)
                      End Sub},
        {"until", Sub()
                      Dim o As Object = DS.Pop
                      loops.Push(0)
                      Do
                          Eval(o)
                          loops.Push(loops.Pop + 1)
                      Loop Until DS.Pop
                      loops.Pop()
                  End Sub},
        {"while", Sub()
                      Dim o As Object = DS.Pop
                      loops.Push(0)
                      Do
                          Eval(o)
                          loops.Push(loops.Pop + 1)
                      Loop While DS.Pop
                      loops.Pop()
                  End Sub},
        {"for", Sub()
                    Dim iEnd As Integer = DS.Pop
                    Dim iStart As Integer = DS.Pop
                    Dim o As Object = DS.Pop
                    loops.Push(iStart)
                    For i As Integer = iStart To iEnd
                        Eval(o)
                        loops.Push(loops.Pop + 1)
                    Next
                    loops.Pop()
                End Sub},
        {"seq", Sub()
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
                End Sub},
        {"toqualifiedname", Sub()
                                Dim wantedTypeName As String = DirectCast(DS.Pop, String).ToLower
                                Dim t As String = ""
                                Dim asm_name As String = ""
                                For Each one In deps.Keys
                                    t = deps(one).FirstOrDefault(Function(l) l.ToLower.EndsWith(wantedTypeName))
                                    asm_name = one
                                    If t <> "" Then Exit For
                                Next
                                If t <> "" Then
                                    DS.Push(t)
                                    DS.Push(asm_name)
                                Else
                                    DS.Push(Nothing)
                                End If
                            End Sub},
        {"pick", Sub()
                     Dim l As Integer = DS.Pop
                     DS.Push(DS(l - 1))
                 End Sub},
        {"ifte", Sub()
                     Dim b As Boolean = DS.Pop
                     Dim oFalse As Object = DS.Pop
                     Dim oTrue As Object = DS.Pop
                     If b Then Eval(oTrue) Else Eval(oFalse)
                 End Sub},
        {"innercomp", Sub()
                          Dim o As List(Of Object) = DS.Pop
                          o.ForEach(Sub(i) DS.Push(i))
                          DS.Push(o.Count)
                      End Sub},
        {"{}n", Sub()
                    Dim nl As New List(Of Object)
                    Dim count As Integer = DS.Pop
                    nl.AddRange(DS.Take(count).Reverse)
                    While count > 0
                        DS.Pop()
                        count -= 1
                    End While
                    DS.Push(nl)
                End Sub},
        {"::n", Sub()
                    Dim ns As New Secondary
                    Dim count As Integer = DS.Pop
                    ns.AddRange(DS.Take(count).Reverse)
                    If count > 0 Then
                        For i As Integer = 0 To count - 1
                            DS.Pop()
                        Next
                    End If
                    DS.Push(ns)
                End Sub},
        {"+", Sub()
                  Dim l0 As Object = DS.Pop
                  Dim l1 As Object = DS.Pop
                  DS.Push(l1 + l0)
              End Sub},
        {"-", Sub()
                  Dim l0 As Object = DS.Pop
                  Dim l1 As Object = DS.Pop
                  DS.Push(l1 - l0)
              End Sub},
        {">", Sub()
                  Dim l0 As Object = DS.Pop
                  Dim l1 As Object = DS.Pop
                  DS.Push(l1 > l0)
              End Sub},
        {"<", Sub()
                  Dim l0 As Object = DS.Pop
                  Dim l1 As Object = DS.Pop
                  DS.Push(l1 < l0)
              End Sub},
        {"*", Sub()
                  Dim l0 As Object = DS.Pop
                  Dim l1 As Object = DS.Pop
                  DS.Push(l1 * l0)
              End Sub},
        {"/", Sub()
                  Dim l0 As Object = DS.Pop
                  Dim l1 As Object = DS.Pop
                  DS.Push(l1 / l0)
              End Sub},
        {"and", Sub()
                    Dim l0 As Object = DS.Pop
                    Dim l1 As Object = DS.Pop
                    DS.Push(l1 And l0)
                End Sub},
        {"mod", Sub()
                    Dim l0 As Object = DS.Pop
                    Dim l1 As Object = DS.Pop
                    DS.Push(l1 Mod l0)
                End Sub},
        {"'", Sub()
                  Dim currentFrame As StackFrame = RS.Peek
                  DS.Push(currentFrame.Secondary(currentFrame.I))
                  currentFrame.I += 1
              End Sub},
        {"'r", Sub()
                   Dim previousFrame As StackFrame = RS(1)
                   DS.Push(previousFrame.Secondary(previousFrame.I))
                   previousFrame.I += 1
               End Sub},
        {"dolist", Sub()
                       Dim act As Object = DS.Pop
                       'Dim lcount As Integer = DS.Pop
                       'If lcount <> 1 Then Throw New Exception("not implemented for lcount <> 1")
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
                   End Sub},
        {"def", Sub()
                    words.Add(DS(0), DS(1))
                    DS.Pop()
                    DS.Pop()
                End Sub},
        {"undef", Sub() If TypeOf DS.Peek Is String AndAlso words.ContainsKey(DirectCast(DS.Peek, String)) Then words.Remove(DirectCast(DS.Pop, String))}
    }


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
            ToStr = words.FirstOrDefault(Function(o) o.Value.Equals(ob)).Key
            If ToStr = "" Then ToStr = DirectCast(ob, Secondary).Aggregate(":: ", Function(a, b) a & ToStr(b) & " ", Function(c) c & ";")
        ElseIf TypeOf ob Is String Then
            ToStr = "$ "
            Dim escaped As Boolean = False
            Dim in_str As String = ob
            Dim out_str As String = in_str
            'Dim escaped_char As Char = vbNullChar
            For Each c As Char In escapes.Keys
                'escaped_char = in_str.Contains(c) 'FirstOrDefault(Function(cc) cc.Value = c).Key
                If in_str.Contains(escapes(c)) Then
                    escaped = True
                    out_str = out_str.Replace(escapes(c), "\" & c)
                End If
            Next
            If escaped OrElse out_str.Contains(" "c) Then ToStr &= """"c & out_str & """"c Else ToStr &= out_str
        ElseIf TypeOf ob Is List(Of Object) Then
            'ElseIf GetType(IList).IsAssignableFrom(ob.GetType) Then
            Dim l As List(Of Object) = DirectCast(ob, List(Of Object))
            If l.Count > 10 Then
                ToStr = l.Take(10).Aggregate("{ ", Function(a, b) a & ToStr(b) & " ", Function(c) c & "(+ " & l.Count - 10 & " more)}")
            Else
                ToStr = l.Aggregate("{ ", Function(a, b) a & ToStr(b) & " ", Function(c) c & "}")
            End If
        ElseIf TypeOf ob Is StackFrame Then
            Dim sf As StackFrame = DirectCast(ob, StackFrame)
            ToStr = ToStr(sf.Secondary) & ", " & sf.I & " (" & ToStr(sf.Secondary(sf.I))
        ElseIf ob.GetType.BaseType Is GetType(MulticastDelegate) Then
            Dim k As String = words.FirstOrDefault(Function(o) o.Value.Equals(ob)).Key
            If k.Length > 0 Then ToStr = k Else ToStr = "External"
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
        ElseIf TypeOf ob Is Bitmap Then
            ToStr = "Bitmap " & DirectCast(ob, Image).Width & " x " & DirectCast(ob, Image).Height
        ElseIf TypeOf ob Is Array Then
            Dim a As Array = DirectCast(ob, Array)
            ToStr = "[ "
            If a.Length > 0 Then
                For i As Integer = 0 To a.Length - 1
                    ToStr &= ToStr(a(i)) & " "
                Next
            End If
            ToStr &= "]"
        Else
            ToStr = ob.ToString & " <" & ob.GetType.Name & ">"
        End If
    End Function

    Dim stepping As Boolean = False

    Sub Eval(p0 As Object)
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
        ElseIf t = GetType(DynamicMethod) Then
            Stop
        ElseIf t = GetType(Identifier) Then
            Eval(vars(DirectCast(p0, Identifier).name))
        Else
            DS.Push(p0)
        End If
    End Sub

    Function WhatIs(s As String) As Tok
        WhatIs = Tok.none
        If words.ContainsKey(s) Then Return Tok.word
        If delims.ContainsKey(s) Then Return delims(s)
    End Function

    Sub AppendToTopLevel(ob As Object)
        If st.Count > 0 Then
            If TypeOf st.Peek Is Secondary Then DirectCast(st.Peek, Secondary).Add(ob) Else DirectCast(st.Peek, List(Of Object)).Add(ob)
        End If
    End Sub

    Dim st As Stack(Of Object)

    Function Split(s As String) As List(Of String)
        Split = New List(Of String)
        Dim current_token As String = ""
        Dim t As Lex = Lex.white
        For Each c As Char In s
            Select Case t
                Case Lex.white
                    If """"c = c Then
                        t = Lex.cstri
                        current_token = ""
                    ElseIf c = "`"c Then
                        t = Lex.comme
                    ElseIf Not Char.IsWhiteSpace(c) Then
                        t = Lex.token
                        current_token = c
                    End If
                Case Lex.token
                    If Char.IsWhiteSpace(c) Then
                        Split.Add(current_token)
                        current_token = ""
                        t = Lex.white
                    Else
                        current_token &= c
                    End If
                Case Lex.cstri
                    If "\"c = c Then
                        t = Lex.escap
                    ElseIf """"c = c Then
                        Split.Add(current_token)
                        t = Lex.white
                    Else
                        current_token &= c
                    End If
                Case Lex.escap
                    If escapes.ContainsKey(c) Then
                        current_token &= escapes(c)
                        t = Lex.cstri
                    Else
                        Throw New Exception("bad escape char '" & c & "'")
                    End If
                Case Lex.comme
                    If c = vbLf OrElse c = vbCr Then t = Lex.white
            End Select
        Next
        Select Case t
            Case Lex.cstri, Lex.escap
                Throw New Exception("badly terminated string & '" & current_token & "'")
            Case Lex.token
                Split.Add(current_token)
        End Select
    End Function


    Function Parse(src As String) As Secondary
        Parse = New Secondary()
        Dim tokens As List(Of String) = Split(src)
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
                                o = tokens(pos)
                            ElseIf expect = Tok.delim_identifier Then
                                o = New Identifier(tokens(pos))
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
                                AppendToTopLevel(words(tokens(pos)))
                            Case Tok.none
                                AppendToTopLevel(New Identifier(tokens(pos)))
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

    Public W As Action(Of String) = Sub(s) Debug.WriteLine(s)
End Class
