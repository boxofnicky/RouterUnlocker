Imports System.IO.Ports
Imports System.Management
Imports System.Threading
Imports Microsoft.Win32
Imports Microsoft.VisualBasic.Devices.Ports
Public Class Form1
    Public shellFile As String = ""
    Public shellParam As String = ""
    Dim atCommand As String = ""
    Dim comPORT As String
    Dim receivedData As String = ""
    Dim deviceQuery As String = ""
    Dim Query As New System.Management.ObjectQuery
    Dim holdTimer As Integer
    Dim PNPDeviceID As String
    Dim Scope As New ManagementScope("\\.\ROOT\cimv2")
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'Dim stream As StreamWriter
        Control.CheckForIllegalCrossThreadCalls = False

        comPORT = ""
        refreshPortList()
        AddHandler SerialPort1.DataReceived, AddressOf modemDataRecieved
        AddHandler SerialPort1.ErrorReceived, AddressOf modemErrorRecieved
        'Stream = New StreamWriter("Systemlog.txt", True)
        'stream.Write(ShowComPortsRegistry)
        'deviceListSerialPort()
        'stream.Write(searchPNPID)
        'stream.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        OpenFileDialog1.ShowDialog()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        OpenFileDialog2.ShowDialog()
    End Sub

    Private Sub OpenFileDialog1_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog1.FileOk
        TextBox1.Text = OpenFileDialog1.FileName
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        OpenFileDialog3.ShowDialog()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        OpenFileDialog4.ShowDialog()
    End Sub

    Private Sub OpenFileDialog4_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog4.FileOk

        TextBox4.Text = OpenFileDialog4.FileName
    End Sub

    Private Sub OpenFileDialog3_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog3.FileOk
        TextBox3.Text = OpenFileDialog3.FileName
    End Sub

    Private Sub OpenFileDialog2_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog2.FileOk
        TextBox2.Text = OpenFileDialog2.FileName
    End Sub
    Public Function executeShellCommand()
        Dim proc As Process
        Dim pHelp As New ProcessStartInfo
        pHelp.FileName = shellFile
        Dim output As String
        writeConsole(Application.ExecutablePath & "\" & shellFile & " " & shellParam, Color.Red)
        pHelp.UseShellExecute = True
        'pHelp.RedirectStandardOutput = True
        pHelp.WindowStyle = ProcessWindowStyle.Normal
        'If Not SerialPort1.IsOpen Then
        'SerialPort1.Open()
        'End If
        proc = Process.Start(pHelp)
        proc.WaitForExit()
        'output = proc.StandardOutput.ReadToEnd
        'writeConsole(output, Color.Aqua)

        Return True
    End Function
    Private Function executeAllCommands()
        Dim binFile As New List(Of String)
        binFile.Add("""" & TextBox1.Text & """")
        binFile.Add("""" & TextBox2.Text & """")
        binFile.Add("""" & TextBox3.Text & """")
        binFile.Add("""" & TextBox4.Text & """")
        If TextBox1.Text = "" Then
            GoTo nicky
        End If

        Dim stream As New StreamWriter("temp.bat", False)
        stream.Write("""" & Directory.GetCurrentDirectory & "\" & "connect.dll"" -p" & comPORT.Remove(0, 3) & " " & binFile.Item(0) & vbCrLf)
        stream.Close()

        shellFile = "temp.bat"
        'shellParam = "-p" & comPORT.Remove(0, 3) & " " & binFile.Item(0)
        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
        writeConsole("Executing bin file " & vbCrLf & shellFile & " " & shellParam & vbCrLf, Color.GreenYellow)
        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
        executeShellCommand()
        Dim tryCount As Integer = 0
nicky:
        holdTimer = 10
        Dim tString As String
        tString = ""
        writeConsole(tString, Color.OrangeRed)
        Dim d As Integer = CInt(Now().TimeOfDay.TotalSeconds)
        Timer1.Enabled = True
        Thread.Sleep(1000)
        Application.DoEvents()
        For i As Integer = 1 To 3 Step 1
            If binFile.Item(i).Replace("""", "") = "" Then
                Continue For
            End If
            'refreshPortList()
            If findPort("FC - PC UI Interface") = vbNullString Then
                writeConsole("Device not found: FC - PC UI Interface.", Color.Red)
                tryCount = tryCount + 1
                If tryCount < 3 Then
                    GoTo nicky
                End If
            Else
                comPORT = findPort("FC - PC UI Interface")
                comPort_ComboBox.SelectedItem = comPORT
            End If
            If i = 3 Then
                executeATCommand("AT^NVWREX=8268,0,12,1,0,0,0,2,0,0,0,A,0,0,0")
                disconnect_Port()
                executeATCommand("AT^GODLOAD")
                disconnect_Port()
                writeConsole(vbNewLine, Color.GreenYellow)
            End If
            stream = New StreamWriter("temp" & i & ".bat", False)
            stream.Write("""" & Directory.GetCurrentDirectory & "\" & "api.dll"" -p" & comPORT.Remove(0, 3) & " -g0 " & binFile.Item(i) & vbCrLf)
            stream.Close()
            shellFile = "temp" & i & ".bat"
            'shellParam = "-p" & comPORT.Remove(0, 3) & " -g0 " & binFile.Item(i)
            writeConsole("Executing routerunlock with bin file " & vbNewLine & binFile.Item(i) & vbNewLine, Color.GreenYellow)
            writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
            executeShellCommand()
        Next
        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
        Return True

    End Function

    Private Function executeATCommand(command As String)

        atCommand = command
        connect_Port()

        Return True
    End Function

    Private Function writeConsole(text As String, color As Color)
        Dim selStart As Integer
        selStart = RichTextBox1.TextLength
        RichTextBox1.AppendText(text)
        RichTextBox1.Select(selStart, RichTextBox1.TextLength)
        RichTextBox1.SelectionColor = color
        RichTextBox1.ScrollToCaret()
        Return True
    End Function


    Private Function writeConsole1(text As String, color As Color)
        Dim selStart As Integer
        selStart = RichTextBox2.TextLength
        RichTextBox2.AppendText(text)
        RichTextBox2.Select(selStart, RichTextBox2.TextLength)
        RichTextBox2.SelectionColor = color
        Return True
    End Function


    Private Function refreshPortList() As Integer
        RichTextBox2.Clear()
        devLabl.Text = ""
        comPort_ComboBox.Items.Clear()
        comPort_ComboBox.Text = ""
        If (CheckBox1.Checked) Then
                comPort_ComboBox.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames)
        Else
            Dim ModemList As List(Of HardwareEnumerator.GSMModemPorts)
            ModemList = HardwareEnumerator.DeviceInfo.EnumeratModemDevices
            For Each s As HardwareEnumerator.GSMModemPorts In ModemList
                comPort_ComboBox.Items.Add(s.Port)
                'nodeSubDevice.Tag = s
            Next
        End If

        If findPort("HUAWEI Mobile Connect - 3G PC UI Interface") = vbNullString Then
            'If findPort("ELTIMA Virtual Serial Port (COM1->COM2)") = vbNullString Then
            writeConsole("Device not found: HUAWEI Mobile Connect - 3G PC UI Interface", Color.Red)
        Else
            'comPort_ComboBox.SelectedItem = findPort("ELTIMA Virtual Serial Port (COM1->COM2)")
            comPORT = findPort("HUAWEI Mobile Connect - 3G PC UI Interface")
            comPort_ComboBox.SelectedItem = comPORT
        End If

        Query.QueryString = "SELECT * FROM Win32_POTSModem"
        'deviceList()
        'writeConsole(searchPNPID, Color.Red)
        'writeConsole(ShowComPortsRegistry, Color.Pink)
        Return My.Computer.Ports.SerialPortNames.Count
    End Function




    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        refreshPortList()
    End Sub


    Private Sub Button5_Click_1(sender As Object, e As EventArgs) Handles Button5.Click
        If comPORT = "" Then
            MsgBox("Select COM port first.")
        Else
            executeAllCommands()
        End If

    End Sub

    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click
        TextBox1.Clear()
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        TextBox2.Clear()
    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        TextBox3.Clear()
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        TextBox4.Clear()
    End Sub


    Private Sub Button12_Click(sender As Object, e As EventArgs) Handles Button12.Click
        If TextBox5.Text <> "" Then

            If comPort_ComboBox.SelectedItem <> "" Then
                For Each cmd As String In TextBox5.Text.ToString.Trim.Split(vbCrLf)
                    If cmd.Trim = "" Then
                        MsgBox("Invalid Command.")
                        Exit Sub
                    Else
                        executeATCommand(Trim(cmd))
                    End If

                Next
            Else
                MsgBox("Select a valid port...")
            End If

        End If

    End Sub



    Private Sub connect_BTN_Click(sender As Object, e As EventArgs) Handles connect_BTN.Click
        If (connect_BTN.Text = "Connect") Then

            executeATCommand("AT")
        Else
            disconnect_Port()
        End If

    End Sub
    Private Function disconnect_Port()
        If SerialPort1.IsOpen Then
            writeConsole("Bytes Recieved: " & SerialPort1.BytesToRead & vbCrLf, Color.GreenYellow)
        End If
        writeConsole("Connection Terminated" & vbNewLine, Color.GreenYellow)
        SerialPort1.Close()
        connect_BTN.Text = "Connect"
        Timer_LBL.Text = "Not Connected"
        Return True
    End Function




    Private Sub comPort_ComboBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comPort_ComboBox.SelectedIndexChanged
        If (comPort_ComboBox.SelectedItem <> "") Then
            Query.QueryString = "SELECT * FROM Win32_potsModem Where AttachedTo='" & comPort_ComboBox.SelectedItem & "'"
        Else
            Query.QueryString = "SELECT * FROM Win32_potsModem"
        End If
        comPORT = comPort_ComboBox.SelectedItem
        deviceList()
        writeConsole(deviceListSerialPort(), Color.Aqua)
    End Sub

    Private Sub clear_BTN_Click(sender As Object, e As EventArgs) Handles clear_BTN.Click
        RichTextBox1.Clear()
    End Sub

    Private Sub modemDataRecieved(sender As Object, e As SerialDataReceivedEventArgs)
        Dim sp As SerialPort = CType(sender, SerialPort)
        If sp.IsOpen Then
            Dim indata As String = sp.ReadExisting()
            receivedData = indata
        Else
            receivedData = "Error communicating with the device. Port closed"
        End If
        writeConsole(receivedData, Color.Aqua)
        sp.Close()
        sp.Dispose()
    End Sub

    Private Sub RichTextBox2_SelectionChanged(sender As Object, e As EventArgs) Handles RichTextBox2.SelectionChanged
        RichTextBox2.ScrollToCaret()
    End Sub


    Private Sub RichTextBox2_TextChanged(sender As Object, e As EventArgs) Handles RichTextBox2.TextChanged
        RichTextBox2.ScrollToCaret()
    End Sub

    Private Sub RichTextBox1_TextChanged(sender As Object, e As EventArgs) Handles RichTextBox1.TextChanged
        RichTextBox1.Refresh()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If holdTimer > 0 Then
            Label2.Visible = True
            Label2.Text = "Please wait : " & holdTimer
            holdTimer = holdTimer - 1
            Thread.Sleep(1000)
            Application.DoEvents()
        Else
            Label2.Text = "Please wait : " & holdTimer
            Label2.Visible = False
            Timer1.Enabled = False
            Timer1.Dispose()
        End If

    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        System.Diagnostics.Process.Start("http://www.routerunlock.com")
    End Sub


    Private Function connect_Port()

        If (comPORT <> "") Then
            With SerialPort1
                writeConsole("Initiating Connection:" & vbNewLine, Color.GreenYellow)
                ' Dial a number via an attached modem on COM1.
          
                Try
                    .Close()
                    .PortName = comPORT
                    .BaudRate = 9600
                    .DataBits = 8
                    .Parity = Parity.None
                    .StopBits = StopBits.One
                    .Handshake = Handshake.RequestToSend
                    .Encoding = System.Text.Encoding.Default
                    .ReadTimeout = 8000
                    .WriteTimeout = 8000
                    .DtrEnable = True
                    .NewLine = vbCrLf
                    .Open()
                    connect_BTN.Text = "Disconnect"
                    Timer_LBL.Text = "Reading Port"
                    If atCommand = "AT^NVWREX=8268,0,12,1,0,0,0,2,0,0,0,A,0,0,0" Then
                        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
                        writeConsole("Unlocking Device" & vbNewLine, Color.GreenYellow)
                        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
                    ElseIf atCommand = "AT^GODLOAD" Then
                        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
                        writeConsole("Putting device in download mode." & vbNewLine, Color.GreenYellow)
                        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
                    Else
                        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
                        writeConsole("Executing : " & atCommand & vbNewLine, Color.GreenYellow)
                        writeConsole("-----------------------------------------" & vbNewLine, Color.GreenYellow)
                    End If
                    .Write(atCommand)
                Catch ex As TimeoutException
                    Return False
                    writeConsole("Write Error: Serial Port read timed out." & vbCrLf, Color.GreenYellow)
                Catch ex As System.InvalidOperationException
                    Return False
                    writeConsole("Write Error: Port closed." & vbCrLf, Color.GreenYellow)

                End Try
            End With
        Else
            MsgBox("Select a COM port first.")
        End If
        Return True
    End Function

    Private Function deviceList()
        RichTextBox2.Clear()
        Dim mgmtSearcher As ManagementObjectSearcher
        mgmtSearcher = New ManagementObjectSearcher(Scope, Query)
        writeConsole1("Detected Modems: " & mgmtSearcher.Get.Count & vbCrLf, Color.Aqua)
        Dim propName As String = ""
        Dim propValue As String = ""
        'RichTextBox1.Clear()
        For Each mgmtObject As ManagementObject In mgmtSearcher.Get()
            For Each propItem As PropertyData In mgmtObject.Properties
                Try
                    propName = propItem.Name & ": "
                    writeConsole1(propName, Color.GreenYellow)
                    writeConsole1(propItem.Value.ToString & vbCrLf, Color.Aqua)
                    If propItem.Name = "AttachedTo" Then
                        propValue = propItem.Value
                        If comPort_ComboBox.Items.Contains(propItem.Value) Then
                        Else
                            comPort_ComboBox.Items.Add(propItem.Value.ToString)
                        End If
                    End If
                    If propItem.Name = "Description" Then
                        devLabl.Text = propItem.Value & vbNewLine
                    End If
                    If propItem.Name = "PNPDeviceID" Then
                        PNPDeviceID = propItem.Value.ToString.Replace("\", "\\").Replace("/", "//")
                        Query.QueryString = "SELECT * FROM Win32_PNPEntity WHERE PNPDeviceID = '" & PNPDeviceID & "'"
                        'writeConsole(searchPNPID, Color.Red)
                    End If
                Catch ex As Exception
                End Try
            Next
        Next


        writeConsole1("--------------------------------", Color.YellowGreen)
        RichTextBox2.Select(0, 0)
        Return True

    End Function
    Private Function searchPNPID() As String

        Try
            Dim searcher As New ManagementObjectSearcher(Scope, Query)
            Dim pnpEntities As String = ""
            For Each queryObj As ManagementObject In searcher.Get()
                pnpEntities = "-----------------------------------" & vbCrLf
                pnpEntities = pnpEntities & "Win32_PnPEntity instance" & vbCrLf
                pnpEntities = pnpEntities & "-----------------------------------" & vbCrLf
                For Each prop As PropertyData In queryObj.Properties
                    Try
                        pnpEntities = pnpEntities & prop.Name & ": "
                        pnpEntities = pnpEntities & prop.Value.ToString & vbCrLf
                    Catch ex As Exception
                        pnpEntities = pnpEntities & vbCrLf
                    End Try
                Next
            Next


            Return pnpEntities
        Catch err As ManagementException
            Return "An error occurred while querying for WMI data: " & err.Message
        End Try

    End Function
    Private Function deviceListSerialPort() As String

        'Get the namespace management scope
        Dim Scope As New ManagementScope("\\.\ROOT\cimv2")
        'Get a result of WML query 
        Dim querySerialPort As New ObjectQuery

        If comPort_ComboBox.SelectedItem = "" Then
            querySerialPort.QueryString = "SELECT * FROM Win32_SerialPort"
            writeConsole("Port Details: All Ports" & vbCrLf, Color.GreenYellow)
        Else
            writeConsole("Port Details: " & comPORT & ": " & vbCrLf & vbCrLf, Color.GreenYellow)
            querySerialPort.QueryString = "SELECT * FROM Win32_SerialPort where DeviceID='" & comPort_ComboBox.SelectedItem & "'"
        End If
        Dim searcher As New ManagementObjectSearcher(Scope, querySerialPort)
        writeConsole(searcher.Get.Count, Color.GreenYellow)
        Dim properties As String
        properties = ""
        For Each queryObj As ManagementObject In searcher.Get()
            For Each prop As PropertyData In queryObj.Properties
                Try
                    properties = properties & prop.Name & ": " & prop.Value & vbCrLf
                Catch ex As Exception
                    properties = properties & vbCrLf
                End Try
            Next
            writeConsole(properties, Color.Aqua)
            writeConsole("-----------------------------------------" & vbCrLf, Color.GreenYellow)
        Next
        Return properties
        RichTextBox1.Select(0, 0)
        RichTextBox1.ScrollToCaret()
    End Function

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        refreshPortList()
    End Sub

    Private Sub modemErrorRecieved(sender As Object, e As SerialErrorReceivedEventArgs)
        Throw New NotImplementedException
        writeConsole(e.ToString, Color.Red)
    End Sub


    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        Dim stream As System.IO.StreamWriter
        stream = New StreamWriter("SystemLog.txt", True)
        stream.Write(searchPNPID)
        stream.Close()

        If MsgBox("Do you want to view the log file?", vbYesNo, "Display System Log") = vbYes Then
            ' Display the collected data.
            Dim proc As Process
            Dim pHelp As New ProcessStartInfo
            pHelp.FileName = "notepad.exe"
            pHelp.Arguments = "SystemLog.txt"
            pHelp.CreateNoWindow = True
            pHelp.UseShellExecute = False
            pHelp.WindowStyle = ProcessWindowStyle.Maximized
            proc = Process.Start(pHelp)
            proc.WaitForExit()
        End If
        Dim ModemList As List(Of HardwareEnumerator.GSMModemPorts)
        ModemList = HardwareEnumerator.DeviceInfo.EnumeratModemDevices
        Dim data As String = ""
        For Each s As HardwareEnumerator.GSMModemPorts In ModemList
            data = data & (s.Name.ToString & "( " & s.Port & " )") & vbCrLf
            'nodeSubDevice.Tag = s
            data = data & s.DeviceID & vbCrLf
        Next

        MsgBox(data)
    End Sub

    Private Function findPort(searchString As String) As String
        Dim searchFriendlyName = searchString.ToLower
        Dim port As String
        Dim k = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Enum\", False)
        For Each k0Name In k.GetSubKeyNames
            Dim k0 = k.OpenSubKey(k0Name, False)
            For Each k1Name In k0.GetSubKeyNames
                Dim k1 = k0.OpenSubKey(k1Name, False)
                For Each k2name In k1.GetSubKeyNames
                    Dim k2 = k1.OpenSubKey(k2name, False)
                    If k2.GetValueNames.Contains("FriendlyName") AndAlso k2.GetValue("FriendlyName").ToString.ToLower.Contains(searchFriendlyName) Then
                        If k2.GetSubKeyNames.Contains("Device Parameters") Then
                            Dim k3 = k2.OpenSubKey("Device Parameters", False)
                            If k3.GetValueNames.Contains("PortName") Then
                                For Each s In System.IO.Ports.SerialPort.GetPortNames
                                    If k3.GetValue("PortName").ToString.ToLower = s.ToLower Then
                                        port = s
                                        For Each prop In k2.GetValueNames
                                            writeConsole(prop & ": " & k2.GetValue(prop).ToString & vbCrLf, Color.Aqua)
                                        Next
                                    End If
                                Next
                            End If
                            k3.Close()
                        End If
                    End If
                    k2.Close()
                Next
                k1.Close()
            Next
            k0.Close()
        Next
        k.Close()
        Return port
    End Function

End Class
