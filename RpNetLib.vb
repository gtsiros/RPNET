Imports System.ComponentModel
Imports System.IO
Imports System.IO.Compression
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
    type
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

    ' change this to where gacutil.exe is 
    Const gacutil_path As String = "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.1 Tools\gacutil.exe"

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
        Inherits List(Of Object) ' i have no idea what i am doing. basically i want the same thing as a List(of Object) but with a different name. 
        'Implements ICloneable 
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
        {"same", Sub() DS.Push(DS.Pop.Equals(DS.Pop))}, ' most of these are quick and dirty assignments to corresponding operators in vb/.net
        {"==", Sub() DS.Push(DS.Pop = DS.Pop)},
        {"?i", Sub() DS.Push(loops.Peek)}, 'gets the topmost loop counter value
        {"{}", New List(Of Object)}, 'pushes a new empty list
        {"?n", Sub() DS.Push(loops(DirectCast(DS.Pop, Integer)))}, 'gets an inner loop counter
        {"or", Sub() DS.Push(DS.Pop Or DS.Pop)}, ' these three are q.n.d. assignments, too
        {"not", Sub() DS.Push(Not DS.Pop)},
        {"xor", Sub() DS.Push(DS.Pop Xor DS.Pop)},
        {"clear", Sub() DS.Clear()},
        {"depth", Sub() DS.Push(DS.Count)},
        {"dir", Sub() DS.Push(Directory.EnumerateFiles(Directory.GetCurrentDirectory).ToList.ConvertAll(Function(s) DirectCast(s, Object)))}, ' can be done via reflection too
        {"drop", Sub() DS.Pop()},
        {"dup", Sub() DS.Push(DS.Peek)},
        {"end", Sub() End},
        {"errorout", Sub() Throw New Exception(DirectCast(DS.Pop, String))},
        {"eval", Sub() Eval(DS.Pop)},
        {"false", False}, ' -> false
        {"import", Sub() DS.Push(Assembly.Load(DirectCast(DS.Pop, String)))}, ' can be done with reflection, too
        {"load", Sub() DS.Push(Assembly.LoadFile(Directory.GetCurrentDirectory() & "\" & DirectCast(DS.Pop, String)))}, ' this one too
        {"null", Sub() DS.Push(Nothing)},
        {"num", Sub() DS.Push(Asc(DirectCast(DS.Pop, Char)))}, ' maybe i should just add a 'cast' word that converts from type to type
        {"over", Sub() DS.Push(DS(1))},
        {"print", Sub() W(ToStr(DS.Pop()))},
        {"roll", Sub() DS.Roll(DirectCast(DS.Pop, Integer))},
        {"rot", Sub() DS.Rot()},
        {"self", Sub() DS.Push(Me)},
        {"stop", Sub() Stop},
        {"strto", Sub() DS.Push(Parse(DirectCast(DS.Pop, String)))},
        {"swap", Sub() DS.Swap()},
        {"tostr", Sub() DS.Push(ToStr(DS.Pop))}, '$ io.file totype { $ ..\..\extrawords.txt } $ ReadAllText @ strto eval
        {"totype", Sub() DS.Push(ToType(DirectCast(DS.Pop, String)))},
        {"throw", Sub() Throw New Exception(DirectCast(DS.Pop, String))}, '  -> true
        {"true", True}, '  -> true
        {"vars", Sub() DS.Push(Array.ConvertAll(vars.Keys.ToArray, Function(k) New Identifier(k)).Cast(Of Object).ToList())},
        {"words", Sub() DS.Push(words.Values.ToList)},
        {"ift", Sub() If DirectCast(DS(1), Boolean) Then Eval(DS.Pop) Else DS.Pop()},
        {"[]n", Sub() ' ob.1 ... ob.n n -> array(ob1...obn)
                    If DS.Count - 1 < DirectCast(DS.Peek, Integer) Then Throw New ArgumentException("bad argument count")
                    Dim new_array As Array = DS.Skip(1).Take(DirectCast(DS.Pop, Integer)).Reverse.ToArray
                    Eval(words("ndrop"))
                    DS.Push(new_array)
                End Sub},
        {"new", Sub()
                    Dim argument_list As List(Of Object)
                    If TypeOf DS.Peek Is List(Of Object) Then argument_list = DirectCast(DS.Pop, List(Of Object)) Else argument_list = New List(Of Object)
                    Dim found_type As Type = Nothing
                    If TypeOf DS.Pop Is String Then
                        found_type = ToType(DirectCast(DS.Pop, String))
                    ElseIf TypeOf DS.Peek Is Type Then
                        found_type = DirectCast(DS.Pop, Type)
                    End If
                    If found_type IsNot Nothing Then DS.Push(Activator.CreateInstance(found_type, argument_list.ToArray)) Else Throw New Exception("bad argument")
                End Sub},
        {"ndrop", Sub() DS.RemoveRange(0, DirectCast(DS.Pop, Integer))},
        {"sto", Sub() ' this is for storing into variables. Will be also used for temporary variables (called labmdas in RPL) later on
                    Dim id As Identifier = DirectCast(DS.Pop, Identifier)
                    If vars.ContainsKey(id.name) Then vars(id.name) = DS.Pop Else vars.Add(id.name, DS.Pop)
                End Sub},
        {"rcl", Sub() DS.Push(vars(DirectCast(DS.Pop, Identifier).name))}, 'this is for reading from variables. Will be also used for temporary variables (called labmdas in RPL) later on
        {"put", Sub()
                    Dim index As Integer = DirectCast(DS.Pop, Integer)
                    Dim ob As Object = DS.Pop
                    If TypeOf DS.Peek Is Array Then
                        DirectCast(DS.Peek, Array).SetValue(ob, index)
                    ElseIf TypeOf DS.Peek Is List(Of Object) Then ' there should be a better way to do this
                        Dim l As List(Of Object) = DirectCast(DS.Pop, List(Of Object))
                        l.RemoveAt(index)
                        l.Insert(index, ob)
                    Else
                        Throw New Exception("bad argument type")
                    End If

                End Sub},
        {"get", Sub()
                    Dim index As Integer = DirectCast(DS.Pop, Integer)
                    Dim ob As IEnumerable = DirectCast(DS.Pop, IEnumerable)
                    DS.Push(ob(index))
                End Sub},
        {"!", Sub() ' ob2 ob1 str -> ob2. it "bangs" ob1 into the ob2 property named str. leaves ob2 on the stack
                  Dim name As String = DirectCast(DS.Pop, String)
                  Dim obj As Object = DS.Pop
                  CallByName(DS.Peek, DirectCast(name, String), CallType.Set, New Object() {obj})
              End Sub},
        {"?", Sub() ' obA str - > obB, gets the property value named str of object obA 
                  Dim pname As String = DirectCast(DS.Pop, String)
                  Dim ob As Object = DS.Pop
                  DS.Push(ob.GetType.GetProperty(pname).GetValue(ob))
              End Sub},
        {"@", Sub() ' change so that it does not check if type. otherwise can't call GetEvent etc
                  Dim method_name As String = DirectCast(DS.Pop, String)
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
        {"disp", Sub() 'qnd way to show an image. 
                     Dim f As New Form() With {.FormBorderStyle = FormBorderStyle.SizableToolWindow}
                     Dim p = New PictureBox With {.Dock = DockStyle.Fill, .Image = DS.Pop, .SizeMode = PictureBoxSizeMode.CenterImage}
                     f.Controls.Add(p)
                     f.Show()
                 End Sub},
        {"reinit", Sub() ReInit()},
        {"findtypes", Sub()
                          Dim wantedTypeName As String = DirectCast(DS.Pop, String).ToLower
                          Dim ls As New List(Of Object)
                          For Each one In deps.Keys
                              Dim arr As List(Of String) = deps(one).FindAll(Function(l) l.ToLower.EndsWith(wantedTypeName))
                              If arr.Count > 0 Then ls.AddRange(arr)
                          Next
                          DS.Push(ls)
                      End Sub},
        {"loop", Sub() ' pops an object from the stack and evaluates it until it leaves a true on the stack. To use it as a "while" just add a not at the end of your command 
                     Dim o As Object = DS.Pop
                     loops.Push(0)
                     Do
                         Eval(o)
                         loops.Push(loops.Pop + 1)
                     Loop Until DS.Pop
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
        {"seq", Sub() ' ob # # -> {}, evaluates ob from # on L2 to # on L1 and puts any results in a list, nested if necessary. it creates a sequence. 
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
                            Dim sl As New List(Of Object)(DS.Take(depthAfter - depthBefore).Reverse)
                            nl.Add(sl)
                        End If
                        loops.Push(loops.Pop + 1)
                    Next
                    loops.Pop()
                    DS.Push(nl)
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
                    If count > 0 Then DS.RemoveRange(0, count)
                    DS.Push(nl)
                End Sub},
        {"::n", Sub()
                    Dim ns As New Secondary
                    Dim count As Integer = DS.Pop
                    ns.AddRange(DS.Take(count).Reverse)
                    If count > 0 Then DS.RemoveRange(0, count)
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
        {"'", Sub() ' instead of Eval() the next object, it pushes it onto the stack
                  Dim currentFrame As StackFrame = RS.Peek
                  DS.Push(currentFrame.Secondary(currentFrame.I))
                  currentFrame.I += 1
              End Sub},
        {"'r", Sub() ' takes the next object from the lower stackframe and pushes it onto the stack, e.g. :: :: # 1 # 2 'r - ; # 3 + ; would cause # 3 to be subtracted from # 2. yes you can make infix operators with this
                   Dim previousFrame As StackFrame = RS(1)
                   DS.Push(previousFrame.Secondary(previousFrame.I))
                   previousFrame.I += 1
               End Sub},
        {"dolist", Sub() ' aka "ForEach" but it will be able to operate on multiple lists etc. the indexing operator (?i) is not usable yet
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
                           If DS.Count > depth_before Then DS.RemoveRange(0, DS.Count - depth_before)
                       Next
                       If output_list.Count > 0 Then DS.Push(output_list)
                   End Sub},
        {"def", Sub()
                    words.Add(DirectCast(DS(0), String), DS(1))
                    DS.RemoveRange(0, 2)
                End Sub},
        {"undef", Sub() If TypeOf DS.Peek Is String AndAlso words.ContainsKey(DirectCast(DS.Peek, String)) Then words.Remove(DirectCast(DS.Pop, String))}
    }


    Sub ReInit()
        W("please wait")
        Dim asmlist As New List(Of String)
        Dim pi As New ProcessStartInfo(gacutil_path, "-l") With {
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
                    'Dim arch As String = parts.FirstOrDefault(Function(s) s.StartsWith("processor")).Split("="c)(1)
                    'If arch <> "MSIL" AndAlso arch <> "x86" Then Continue While
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

        Dim bw As New BackgroundWorker() With {.WorkerReportsProgress = True}
        Dim work As DoWorkEventHandler = Sub(ob, args)
                                             Dim worker As BackgroundWorker = DirectCast(ob, BackgroundWorker)
                                             Dim aasms As Dictionary(Of String, AsmInfo) = DirectCast(args.Argument, Dictionary(Of String, AsmInfo))
                                             Dim sw As New Stopwatch
                                             sw.Start()
                                             Dim i As Integer = 0
                                             Dim failed As Integer = 0
                                             Dim total As Integer = asms.Count
                                             Dim teps As New Dictionary(Of String, List(Of String))
                                             Do
                                                 Try
                                                     Do
                                                         Dim asmname As String = aasms.Keys(i)
                                                         Dim asm As Assembly = Assembly.Load(aasms(asmname).Fullname)
                                                         teps.Add(aasms(asmname).Fullname, Array.ConvertAll(asm.GetTypes(), Function(a) a.FullName).ToList)
                                                         i += 1
                                                         If sw.ElapsedMilliseconds > 100 Then
                                                             worker.ReportProgress(CType(Math.Ceiling(i / total * 100.0), Integer))
                                                             sw.Restart()
                                                         End If
                                                     Loop While i < total
                                                 Catch ex As Exception
                                                     failed += 1
                                                     i += 1
                                                 End Try
                                             Loop While i < total
                                             args.Result = teps
                                         End Sub
        AddHandler bw.DoWork, work
        Dim prog As ProgressChangedEventHandler = Sub(ob, args)
                                                      W(args.ProgressPercentage & " %")
                                                  End Sub
        AddHandler bw.ProgressChanged, prog
        Dim done As RunWorkerCompletedEventHandler = Sub(ob, args)
                                                         deps = DirectCast(args.Result, Dictionary(Of String, List(Of String)))
                                                         Dim aFormatter As IFormatter = New BinaryFormatter
                                                         File.Delete("../test.bin")
                                                         Using aStream As Stream = New FileStream("../test.bin", FileMode.Create, FileAccess.Write, FileShare.None),
                                                           gz As New GZipStream(aStream, CompressionLevel.Optimal)
                                                             aFormatter.Serialize(gz, deps)
                                                         End Using
                                                         Dim totaltypes As Integer = 0
                                                         For Each k In deps.Keys
                                                             totaltypes += deps(k).Count
                                                         Next
                                                         W("done (" & totaltypes & " types in " & deps.Count & " assemblies)")
                                                     End Sub
        AddHandler bw.RunWorkerCompleted, done
        bw.RunWorkerAsync(asms)
    End Sub

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

    Function ToType(s As String) As Type
        Dim wantedTypeName As String = s.ToLower
        ToType = Nothing
        For Each one In deps.Keys
            Dim iot As Integer = deps(one).FindIndex(Function(l) l.ToLower.EndsWith(wantedTypeName))
            If iot >= 0 Then Return Assembly.Load(one).GetType(deps(one)(iot))
        Next
    End Function

    Function ToStr(ob As Object) As String
        ToStr = ""
        If ob Is Nothing Then
            ToStr = "null"
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
        ElseIf TypeOf ob Is Type Then
            ToStr = "<" & DirectCast(ob, Type).FullName & ">"
        Else
            ToStr = ob.ToString & " <" & ob.GetType.Name & ">"
        End If
    End Function

    Dim stepping As Boolean = False ' here i tried to make a step-by-step execution thingie. i failed.

    Sub Eval(p0 As Object)
        If p0 Is Nothing Then Exit Sub
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
            If vars.ContainsKey(DirectCast(p0, Identifier).name) Then Eval(vars(DirectCast(p0, Identifier).name)) Else DS.Push(p0)
        Else
            DS.Push(p0)
        End If
    End Sub

    Function TokenType(s As String) As Tok
        TokenType = Tok.none
        If words.ContainsKey(s) Then Return Tok.word
        If delims.ContainsKey(s) Then Return delims(s)
        If s(0) = "<"c AndAlso s.EndsWith(">"c) Then Return Tok.type
    End Function

    Sub New()
        If File.Exists("../test.bin") Then
            Dim aFormatter As IFormatter = New BinaryFormatter
            Using aStream As Stream = New FileStream("../test.bin", FileMode.Open, FileAccess.Read, FileShare.Read),
                    ugz As New GZipStream(aStream, CompressionMode.Decompress)
                deps = DirectCast(aFormatter.Deserialize(ugz), Dictionary(Of String, List(Of String)))
            End Using
        End If
    End Sub
    Dim st As Stack(Of IList) 'maybe

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
        st = New Stack(Of IList)
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
                            st.Peek.Add(o)
                        Catch ex As Exception
                            Throw New Exception("can Not represent a " & types(expect).ToString)
                        End Try
                        expect = Tok.none
                    Else
                        Dim tt As Tok = TokenType(tokens(pos))
                        Select Case tt
                            Case Tok.CurlyOpen
                                st.Push(New List(Of Object))
                            Case Tok.SecondaryOpen
                                st.Push(New Secondary)
                            Case Tok.CurlyClose
                                If TypeOf st.Peek IsNot List(Of Object) Then Throw New Exception("mismatched list")
                                Dim ob As Object = st.Pop
                                st.Peek.Add(ob)
                            Case Tok.SecondaryClose
                                If TypeOf st.Peek IsNot Secondary Then Throw New Exception("mismatched secondary")
                                Dim ob As Object = st.Pop
                                st.Peek.Add(ob)
                            Case Tok.BracketOpen, Tok.BracketClose, Tok.ParenOpen, Tok.ParenClose ' this is why i can't parse arrays yet
                            Case Tok.delim_bint, Tok.delim_single, Tok.delim_double, Tok.delim_cstring, Tok.delim_identifier
                                expect = tt
                            Case Tok.word
                                st.Peek.Add(words(tokens(pos)))
                            Case Tok.type
                                st.Peek.Add(ToType(tokens(pos).Substring(1, tokens(pos).Length - 2)))
                            Case Tok.none
                                st.Peek.Add(New Identifier(tokens(pos)))
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

    Public W As Action(Of String) = Sub(s) Debug.WriteLine(s) ' so the using program can change it to whatever
End Class
