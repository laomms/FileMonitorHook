Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks


Public Class ServerInterface
		Inherits MarshalByRefObject

		Public Sub IsInstalled(ByVal clientPID As Integer)
			Console.WriteLine("FileMonitor has injected FileMonitorHook into process {0}." & vbCrLf, clientPID)
		End Sub

		Public Sub ReportMessages(ByVal messages() As String)
			For i As Integer = 0 To messages.Length - 1
				Console.WriteLine(messages(i))
			Next i
		End Sub

		Public Sub ReportMessage(ByVal message As String)
			Console.WriteLine(message)
		End Sub

		Public Sub ReportException(ByVal e As Exception)
			Console.WriteLine("The target process has reported an error:" & vbCrLf & e.ToString())
		End Sub

		Private count As Integer = 0
		Public Sub Ping()
			Dim oldTop = Console.CursorTop
			Dim oldLeft = Console.CursorLeft
			Console.CursorVisible = False

			Dim chars = "\|/-"
			Console.SetCursorPosition(Console.WindowWidth - 1, oldTop - 1)
'INSTANT VB WARNING: An assignment within expression was extracted from the following statement:
'ORIGINAL LINE: Console.Write(chars[count++ % chars.Length]);
			Console.Write(chars.Chars(count Mod chars.Length))
			count += 1

			Console.SetCursorPosition(oldLeft, oldTop)
			Console.CursorVisible = True
		End Sub
	End Class

