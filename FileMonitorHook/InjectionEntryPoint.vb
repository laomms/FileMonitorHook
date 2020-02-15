Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading.Tasks


Public Class InjectionEntryPoint
		Implements EasyHook.IEntryPoint

		Private _server As ServerInterface = Nothing
		Private _messageQueue As New Queue(Of String)()
		Public Sub New(ByVal context As EasyHook.RemoteHooking.IContext, ByVal channelName As String)
			_server = EasyHook.RemoteHooking.IpcConnectClient(Of ServerInterface)(channelName)
			_server.Ping()
		End Sub

		Public Sub Run(ByVal context As EasyHook.RemoteHooking.IContext, ByVal channelName As String)
			_server.IsInstalled(EasyHook.RemoteHooking.GetCurrentProcessId())

			Dim createFileHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "CreateFileW"), New CreateFile_Delegate(AddressOf CreateFile_Hook), Me)


			Dim readFileHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "ReadFile"), New ReadFile_Delegate(AddressOf ReadFile_Hook), Me)


			Dim writeFileHook = EasyHook.LocalHook.Create(EasyHook.LocalHook.GetProcAddress("kernel32.dll", "WriteFile"), New WriteFile_Delegate(AddressOf WriteFile_Hook), Me)


			createFileHook.ThreadACL.SetExclusiveACL(New Int32() { 0 })
			readFileHook.ThreadACL.SetExclusiveACL(New Int32() { 0 })
			writeFileHook.ThreadACL.SetExclusiveACL(New Int32() { 0 })

			_server.ReportMessage("CreateFile, ReadFile and WriteFile hooks installed")


			EasyHook.RemoteHooking.WakeUpProcess()

			Try

				Do
					System.Threading.Thread.Sleep(500)

					Dim queued() As String = Nothing

					SyncLock _messageQueue
						queued = _messageQueue.ToArray()
						_messageQueue.Clear()
					End SyncLock


					If queued IsNot Nothing AndAlso queued.Length > 0 Then
						_server.ReportMessages(queued)
					Else
						_server.Ping()
					End If
				Loop
			Catch

			End Try


			createFileHook.Dispose()
			readFileHook.Dispose()
			writeFileHook.Dispose()


			EasyHook.LocalHook.Release()
		End Sub


		<DllImport("Kernel32.dll", SetLastError := True, CharSet := CharSet.Auto)>
		Private Shared Function GetFinalPathNameByHandle(ByVal hFile As IntPtr, <MarshalAs(UnmanagedType.LPTStr)> ByVal lpszFilePath As StringBuilder, ByVal cchFilePath As UInteger, ByVal dwFlags As UInteger) As UInteger
		End Function

		#Region "CreateFileW Hook"


		<UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet := CharSet.Unicode, SetLastError := True)>
		Private Delegate Function CreateFile_Delegate(ByVal filename As String, ByVal desiredAccess As UInt32, ByVal shareMode As UInt32, ByVal securityAttributes As IntPtr, ByVal creationDisposition As UInt32, ByVal flagsAndAttributes As UInt32, ByVal templateFile As IntPtr) As IntPtr


		<DllImport("kernel32.dll", CharSet := CharSet.Unicode, SetLastError := True, CallingConvention := CallingConvention.StdCall)>
		Private Shared Function CreateFileW(ByVal filename As String, ByVal desiredAccess As UInt32, ByVal shareMode As UInt32, ByVal securityAttributes As IntPtr, ByVal creationDisposition As UInt32, ByVal flagsAndAttributes As UInt32, ByVal templateFile As IntPtr) As IntPtr
		End Function


		Private Function CreateFile_Hook(ByVal filename As String, ByVal desiredAccess As UInt32, ByVal shareMode As UInt32, ByVal securityAttributes As IntPtr, ByVal creationDisposition As UInt32, ByVal flagsAndAttributes As UInt32, ByVal templateFile As IntPtr) As IntPtr
			Try
				SyncLock Me._messageQueue
					If Me._messageQueue.Count < 1000 Then
						Dim mode As String = String.Empty
						Select Case creationDisposition
							Case 1
								mode = "CREATE_NEW"
							Case 2
								mode = "CREATE_ALWAYS"
							Case 3
								mode = "OPEN_ALWAYS"
							Case 4
								mode = "OPEN_EXISTING"
							Case 5
								mode = "TRUNCATE_EXISTING"
						End Select

						   Me._messageQueue.Enqueue(String.Format("[{0}:{1}]: CREATE ({2}) ""{3}""", EasyHook.RemoteHooking.GetCurrentProcessId(), EasyHook.RemoteHooking.GetCurrentThreadId(), mode, filename))
					End If
				End SyncLock
			Catch

			End Try


			Return CreateFileW(filename, desiredAccess, shareMode, securityAttributes, creationDisposition, flagsAndAttributes, templateFile)
		End Function

		#End Region

		#Region "ReadFile Hook"


		<UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError := True)>
		Private Delegate Function ReadFile_Delegate(ByVal hFile As IntPtr, ByVal lpBuffer As IntPtr, ByVal nNumberOfBytesToRead As UInteger, ByRef lpNumberOfBytesRead As UInteger, ByVal lpOverlapped As IntPtr) As Boolean


		<DllImport("kernel32.dll", SetLastError := True, CallingConvention := CallingConvention.StdCall)>
		Private Shared Function ReadFile(ByVal hFile As IntPtr, ByVal lpBuffer As IntPtr, ByVal nNumberOfBytesToRead As UInteger, ByRef lpNumberOfBytesRead As UInteger, ByVal lpOverlapped As IntPtr) As Boolean
		End Function

		Private Function ReadFile_Hook(ByVal hFile As IntPtr, ByVal lpBuffer As IntPtr, ByVal nNumberOfBytesToRead As UInteger, ByRef lpNumberOfBytesRead As UInteger, ByVal lpOverlapped As IntPtr) As Boolean
			Dim result As Boolean = False
			lpNumberOfBytesRead = 0

			 result = ReadFile(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, lpOverlapped)

			Try
				SyncLock Me._messageQueue
					If Me._messageQueue.Count < 1000 Then

						Dim filename As New StringBuilder(255)
						GetFinalPathNameByHandle(hFile, filename, 255, 0)


						Me._messageQueue.Enqueue(String.Format("[{0}:{1}]: READ ({2} bytes) ""{3}""", EasyHook.RemoteHooking.GetCurrentProcessId(), EasyHook.RemoteHooking.GetCurrentThreadId(), lpNumberOfBytesRead, filename))
					End If
				End SyncLock
			Catch

			End Try

			Return result
		End Function

		#End Region

		#Region "WriteFile Hook"


		<UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet := CharSet.Unicode, SetLastError := True)>
		Private Delegate Function WriteFile_Delegate(ByVal hFile As IntPtr, ByVal lpBuffer As IntPtr, ByVal nNumberOfBytesToWrite As UInteger, ByRef lpNumberOfBytesWritten As UInteger, ByVal lpOverlapped As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean

		<DllImport("kernel32.dll", CharSet := CharSet.Unicode, SetLastError := True, CallingConvention := CallingConvention.StdCall)>
		Private Shared Function WriteFile(ByVal hFile As IntPtr, ByVal lpBuffer As IntPtr, ByVal nNumberOfBytesToWrite As UInteger, ByRef lpNumberOfBytesWritten As UInteger, ByVal lpOverlapped As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
		End Function

		Private Function WriteFile_Hook(ByVal hFile As IntPtr, ByVal lpBuffer As IntPtr, ByVal nNumberOfBytesToWrite As UInteger, ByRef lpNumberOfBytesWritten As UInteger, ByVal lpOverlapped As IntPtr) As Boolean
			Dim result As Boolean = False

			result = WriteFile(hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, lpOverlapped)

			Try
				SyncLock Me._messageQueue
					If Me._messageQueue.Count < 1000 Then
						Dim filename As New StringBuilder(255)
						GetFinalPathNameByHandle(hFile, filename, 255, 0)

						Me._messageQueue.Enqueue(String.Format("[{0}:{1}]: WRITE ({2} bytes) ""{3}""", EasyHook.RemoteHooking.GetCurrentProcessId(), EasyHook.RemoteHooking.GetCurrentThreadId(), lpNumberOfBytesWritten, filename))
					End If
				End SyncLock
			Catch

			End Try

			Return result
		End Function

		#End Region
	End Class

