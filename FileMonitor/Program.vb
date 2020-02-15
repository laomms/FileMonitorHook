
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks


Friend Class Program
		Shared Sub Main(ByVal args() As String)
			Dim targetPID As Int32 = 0
			Dim targetExe As String = Nothing


			Dim channelName As String = Nothing
			ProcessArgs(args, targetPID, targetExe)
			If targetPID <= 0 AndAlso String.IsNullOrEmpty(targetExe) Then
				Return
			End If

		EasyHook.RemoteHooking.IpcCreateServer(Of ServerInterface)(channelName, System.Runtime.Remoting.WellKnownObjectMode.Singleton)
		Dim injectionLibrary As String = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "FileMonitorHook.dll")

			Try
				If targetPID > 0 Then
					Console.WriteLine("Attempting to inject into process {0}", targetPID)
					EasyHook.RemoteHooking.Inject(targetPID, injectionLibrary, injectionLibrary, channelName)
				ElseIf Not String.IsNullOrEmpty(targetExe) Then
					Console.WriteLine("Attempting to create and inject into {0}", targetExe)
					EasyHook.RemoteHooking.CreateAndInject(targetExe, "", 0, EasyHook.InjectionOptions.DoNotRequireStrongName, injectionLibrary, injectionLibrary, targetPID, channelName)
				End If
			Catch e As Exception
				Console.ForegroundColor = ConsoleColor.Red
				Console.WriteLine("There was an error while injecting into target:")
				Console.ResetColor()
				Console.WriteLine(e.ToString())
			End Try

			Console.ForegroundColor = ConsoleColor.DarkGreen
			Console.WriteLine("<Press any key to exit>")
			Console.ResetColor()
			Console.ReadKey()
		End Sub

		Private Shared Sub ProcessArgs(ByVal args() As String, ByRef targetPID As Integer, ByRef targetExe As String)
			targetPID = 0
			targetExe = Nothing

			Console.WriteLine()
			Console.ForegroundColor = ConsoleColor.DarkGreen
			Console.WriteLine("    monitor file operations in the target process")
			Console.WriteLine()
			Console.ResetColor()

			' Load any parameters
			Do While (args.Length <> 1) OrElse Not Int32.TryParse(args(0), targetPID) OrElse Not File.Exists(args(0))
				If targetPID > 0 Then
					Exit Do
				End If
				If args.Length <> 1 OrElse Not File.Exists(args(0)) Then
					Console.WriteLine("Usage: FileMonitor ProcessID")
					Console.WriteLine("   or: FileMonitor PathToExecutable")
					Console.WriteLine("")
					Console.WriteLine("e.g. : FileMonitor 1234")
					Console.WriteLine("          to monitor an existing process with PID 1234")
					Console.WriteLine("  or : FileMonitor ""C:\Windows\Notepad.exe""")
					Console.WriteLine("          create new notepad.exe process using RemoteHooking.CreateAndInject")
					Console.WriteLine()
					Console.WriteLine("Enter a process Id or path to executable")
					Console.Write("> ")

					args = New String() { Console.ReadLine() }

					Console.WriteLine()

					If String.IsNullOrEmpty(args(0)) Then
						Return
					End If
				Else
					targetExe = args(0)
					Exit Do
				End If
			Loop
		End Sub
	End Class

