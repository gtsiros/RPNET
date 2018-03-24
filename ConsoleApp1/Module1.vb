Imports System.Reflection
Namespace Utilities


	Module Utilities

		Public Sub W(s As String)
			Debug.WriteLine(s)
		End Sub

		Public Function ToEnum(ty As Type, ByVal num As Integer) As String
			ToEnum = ""
			If Not ty.IsEnum Then Throw New Exception("not an enum type")
			If [Enum].IsDefined(ty, num) Then Return [Enum].GetName(ty, num)
			Dim i = 1
			Dim bMore As Boolean = False
			While num <> 0
				If 1 And num <> 0 Then ToEnum &= IIf(bMore, " or ", "") & [Enum].GetName(ty, i)
				i <<= 1
				num >>= 1
			End While
		End Function

	End Module
End Namespace
