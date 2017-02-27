Option Strict Off
Imports System.Text
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Imports System.Management
Imports WbemScripting

Namespace HardwareEnumerator
    Public Class GSMModemPorts
        Private sName As String
        Public Property Name() As String
            Get
                Return sName
            End Get
            Set(ByVal Value As String)
                sName = Value
            End Set
        End Property

        Private nPort As String
        Public Property Port() As String
            Get
                Return nPort
            End Get
            Set(ByVal Value As String)
                nPort = Value
            End Set
        End Property

        Private nDeviceID As String
        Public Property DeviceID() As String
            Get
                Return nDeviceID
            End Get
            Set(ByVal Value As String)
                nDeviceID = Value
            End Set
        End Property
    End Class

    Public Class DeviceInfo
        Private Shared m_CommPort As New System.IO.Ports.SerialPort

        Public Const DIGCF_PRESENT As Integer = (&H2)
        Public Const MAX_DEV_LEN As Integer = 1000
        Public Const SPDRP_FRIENDLYNAME As Integer = (&HC)        ' FriendlyName (R/W)
        Public Const SPDRP_DEVICEDESC As Integer = (&H0)        ' DeviceDesc (R/W)

        'Mine
        Public Const SPDRP_LOCATION_INFORMATION As Integer = (&HE)        ' FriendlyName (R/W)
        Public Const SPDRP_ENUMERATOR_NAME As Integer = (&H17)        ' FriendlyName (R/W)
        Public Const SPDRP_PHYSICAL_DEVICE_OBJECT_NAME As Integer = (&HF)        ' FriendlyName (R/W)
        Public Const SPDRP_DEVTYPE As Integer = (&H1A)        ' FriendlyName (R/W)
        Public Const SPDRP_INSTALL_STATE As Integer = (&H23)        ' FriendlyName (R/W)
        Public Const SPDRP_SERVICE As Integer = (&H5)        ' FriendlyName (R/W)
        'Mine

        <StructLayout(LayoutKind.Sequential)> _
        Public Class SP_DEVINFO_DATA
            Public cbSize As Integer
            Public ClassGuid As Guid
            Public DevInst As Integer
            ' DEVINST handle
            Public Reserved As ULong
        End Class

        '
        <DllImport("setupapi.dll")> _
        Public Shared Function SetupDiClassGuidsFromNameA(ByVal ClassN As String, ByRef guids As Guid, ByVal ClassNameSize As UInt32, ByRef ReqSize As UInt32) As [Boolean]
        End Function

        'result HDEVINFO
        <DllImport("setupapi.dll")> _
        Public Shared Function SetupDiGetClassDevsA(ByRef ClassGuid As Guid, ByVal Enumerator As UInt32, ByVal hwndParent As IntPtr, ByVal Flags As UInt32) As IntPtr
        End Function

        <DllImport("setupapi.dll")> _
        Public Shared Function SetupDiEnumDeviceInfo(ByVal DeviceInfoSet As IntPtr, ByVal MemberIndex As UInt32, ByVal DeviceInfoData As SP_DEVINFO_DATA) As [Boolean]
        End Function

        <DllImport("setupapi.dll")> _
        Public Shared Function SetupDiDestroyDeviceInfoList(ByVal DeviceInfoSet As IntPtr) As [Boolean]
        End Function

        <DllImport("setupapi.dll")> _
        Public Shared Function SetupDiGetDeviceRegistryPropertyA(ByVal DeviceInfoSet As IntPtr, ByVal DeviceInfoData As SP_DEVINFO_DATA, ByVal [Property] As UInt32, ByVal PropertyRegDataType As UInt32, ByVal PropertyBuffer As StringBuilder, ByVal PropertyBufferSize As UInt32, _
        ByVal RequiredSize As IntPtr) As [Boolean]
        End Function



        Public Shared Function EnumerateDevices(ByVal DeviceIndex As UInt32, ByVal ClassName As String, ByVal DeviceName As StringBuilder) As Integer
            Dim RequiredSize As UInt32 = 0
            Dim guid__1 As Guid = Guid.Empty
            Dim guids As Guid() = New Guid(0) {}
            Dim NewDeviceInfoSet As IntPtr
            Dim DeviceInfoData As New SP_DEVINFO_DATA()


            Dim res As Boolean = SetupDiClassGuidsFromNameA(ClassName, guids(0), RequiredSize, RequiredSize)

            If RequiredSize = 0 Then
                'incorrect class name:
                DeviceName = New StringBuilder("")
                Return -2
            End If

            If Not res Then
                guids = New Guid(CInt(RequiredSize - 1)) {}
                res = SetupDiClassGuidsFromNameA(ClassName, guids(0), RequiredSize, RequiredSize)

                If Not res OrElse RequiredSize = 0 Then
                    'incorrect class name:
                    DeviceName = New StringBuilder("")
                    Return -2
                End If
            End If

            'get device info set for our device class
            NewDeviceInfoSet = SetupDiGetClassDevsA(guids(0), 0, IntPtr.Zero, DIGCF_PRESENT)
            Try
                If NewDeviceInfoSet.ToInt32() = -1 Then
                    If Not res Then
                        'device information is unavailable:
                        DeviceName = New StringBuilder("")
                        Return -3
                    End If
                End If
            Catch ex As System.OverflowException
                If Not res Then
                    'device information is unavailable:
                    DeviceName = New StringBuilder("")
                    Return -3
                End If
            End Try
            DeviceInfoData.cbSize = 28
            'is devices exist for class
            DeviceInfoData.DevInst = 0
            DeviceInfoData.ClassGuid = System.Guid.Empty
            DeviceInfoData.Reserved = 0

            res = SetupDiEnumDeviceInfo(NewDeviceInfoSet, DeviceIndex, DeviceInfoData)
            If Not res Then
                'no such device:
                SetupDiDestroyDeviceInfoList(NewDeviceInfoSet)
                DeviceName = New StringBuilder("")
                Return -1
            End If



            DeviceName.Capacity = MAX_DEV_LEN
            If Not SetupDiGetDeviceRegistryPropertyA(NewDeviceInfoSet, DeviceInfoData, SPDRP_FRIENDLYNAME, 0, DeviceName, MAX_DEV_LEN, IntPtr.Zero) Then
                res = SetupDiGetDeviceRegistryPropertyA(NewDeviceInfoSet, DeviceInfoData, SPDRP_DEVICEDESC, 0, DeviceName, MAX_DEV_LEN, IntPtr.Zero)
                If Not res Then
                    'incorrect device name:
                    SetupDiDestroyDeviceInfoList(NewDeviceInfoSet)
                    DeviceName = New StringBuilder("")
                    Return -4
                End If
            End If
            Return 0
        End Function

        Public Shared Function GetModemsComPorts(ByVal ClassName As String) As List(Of GSMModemPorts)
            Dim DevicesList As New List(Of GSMModemPorts)
            Dim devices As New StringBuilder("")
            Dim Index As UInt32 = 0
            Dim result As Integer = 0

            If ClassName.Length < 1 Then
                'DevicesList.Add("Invalid name of Class = " & ClassName)
                Return Nothing
            End If

            Dim GSMModem As GSMModemPorts
            Dim OrigDeviceName As String = ""
            Dim OrigDevicePort As String = ""
            While True
                result = EnumerateDevices(Index, ClassName, devices)
                Index = CUInt(Index + 1)
                If result = -2 Then
                    'DevicesList.Add("Incorrect name of Class = " & ClassName)
                    Exit While
                End If
                If result = -1 Then
                    Exit While
                End If
                If result = 0 Then
                    OrigDeviceName = devices.ToString
                    If InStr(OrigDeviceName, "(com", CompareMethod.Text) > 0 Then
                        OrigDevicePort = Split(OrigDeviceName, "(com", -1, CompareMethod.Text)(1)
                        OrigDevicePort = Split(OrigDevicePort, ")", -1, CompareMethod.Text)(0)
                        OrigDevicePort = "COM" & OrigDevicePort

                        OrigDeviceName = Split(OrigDeviceName, "(com", -1, CompareMethod.Text)(0)

                        GSMModem = New GSMModemPorts
                        GSMModem.Name = OrigDeviceName
                        GSMModem.DeviceID = OrigDeviceName
                        GSMModem.Port = OrigDevicePort
                        If CheckPort(Mid(GSMModem.Port, 4)) Then
                            DevicesList.Add(GSMModem)
                        End If
                    End If
                    'OrigDeviceName = devices.ToString.ToLower
                    'If OrigDeviceName.Contains("(com") Then
                    '    OrigDevicePort = Split(OrigDeviceName, "(com")(1)
                    '    OrigDevicePort = Split(OrigDevicePort, ")")(0)
                    '    OrigDevicePort = "COM" & OrigDevicePort

                    '    OrigDeviceName = Split(OrigDeviceName, "(com")(0)

                    '    GSMModem = New GSMModemPorts
                    '    GSMModem.Name = OrigDeviceName
                    '    GSMModem.Port = OrigDevicePort
                    '    DevicesList.Add(GSMModem)
                    'End If
                End If
            End While

            Return DevicesList
        End Function
        '<STAThread()> _
        'Private Shared Sub Main(ByVal args As String())
        '    Dim devices As New StringBuilder("")
        '    Dim Index As UInt32 = 0
        '    Dim result As Integer = 0

        '    If args.Length <> 1 Then
        '        Console.WriteLine("command line format:")
        '        Console.WriteLine("DevInfo <CLASSNAME>")
        '        Return
        '    End If

        '    While True
        '        result = EnumerateDevices(Index, args(0), devices)
        '        Index = CUInt(Index + 1)
        '        If result = -2 Then
        '            Console.WriteLine("Incorrect name of Class = {0}", args(0))
        '            Exit While
        '        End If
        '        If result = -1 Then
        '            Exit While
        '        End If
        '        If result = 0 Then
        '            Console.WriteLine("Device{0} is {1}", Index, devices)
        '        End If
        '    End While

        'End Sub

        Public Shared Function GetModemDevices() As List(Of GSMModemPorts)
            Dim pc As String = "." 'local
            Dim wmi As Object = GetObject("winmgmts:\\" & pc & "\root\cimv2")
            Dim DevicesList As New List(Of GSMModemPorts)
            Dim GSMModem As GSMModemPorts
            Dim devices As SWbemObjectSet = wmi.ExecQuery("SELECT * FROM Win32_POTSModem WHERE Status = 'OK'")


            For Each d As SWbemObject In devices
                GSMModem = New GSMModemPorts
                GSMModem.Name = d.Name.ToString
                GSMModem.DeviceID = d.Name.ToString
                GSMModem.Port = d.attachedto.ToString
                DevicesList.Add(GSMModem)
                'If CheckPort(Mid(GSMModem.Port, 4)) Then
                '    DevicesList.Add(GSMModem)
                'End If
            Next

            Return DevicesList
        End Function

        Public Shared Function EnumeratModemDevices() As List(Of GSMModemPorts)
            Dim DriversList_1 As New List(Of GSMModemPorts)
            DriversList_1 = GetModemDevices()

            Dim DriversList_2 As New List(Of GSMModemPorts)
            DriversList_2 = GetModemsComPorts("ports")

            Dim DriversList As New List(Of GSMModemPorts)
            If Not DriversList_1 Is Nothing And DriversList_1.Count > 0 Then
                DriversList.AddRange(DriversList_1)
            End If
            If Not DriversList_2 Is Nothing And DriversList_2.Count > 0 Then
                DriversList.AddRange(DriversList_2)
            End If
            Return DriversList
        End Function

        Public Shared Function IsPortAvailable(ByVal port As Integer) As Boolean
            Try
                m_CommPort.PortName = "COM" & port
                m_CommPort.Open()
                ' If it makes it to here, then the Comm Port is available.
                m_CommPort.Close()
                Return True
            Catch
                If Err.Number = 57 Then
                    'Port Existed and InUse
                    Return True
                End If
                ' If it gets here, then the attempt to open the Comm Port
                '   was unsuccessful.
                Return False
            End Try
        End Function

        Private Shared Function IsPortAModem(ByVal port As Integer) As Boolean

            ' Always wrap up working with Comm Ports in exception handlers.
            Try
                ' Attempt to open the port.
                m_CommPort.PortName = "COM" & port
                m_CommPort.Open()

                ' Write an AT Command to the Port.
                m_CommPort.Write("AT" & Chr(13))
                ' Sleep long enough for the modem to respond.
                System.Threading.Thread.Sleep(200)
                Application.DoEvents()
                ' Try to get info from the Comm Port.
                Try
                    Dim Result As String = m_CommPort.ReadExisting()
                    m_CommPort.Close()
                    If Result.ToLower.Contains("ok") Then
                        Return True
                    Else
                        Return False
                    End If
                Catch exc As Exception
                    ' Nothing to read from the Comm Port, so set to False
                    m_CommPort.Close()
                    Return False
                End Try
            Catch exc As Exception
                If Err.Number = 57 Then
                    'Port Existed and InUse
                    Return True
                End If
                ' Port could not be opened or written to.
                Return False
            End Try
        End Function

        Private Shared Function CheckPort(ByVal Port As Integer) As Boolean
            If IsPortAvailable(Port) Then
                ' Check if port responds to an AT command.
                If IsPortAModem(Port) Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return False
            End If
        End Function
    End Class

End Namespace