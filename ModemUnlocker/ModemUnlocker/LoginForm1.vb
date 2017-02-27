Imports System.Management
Imports System.Threading
Public Class LoginForm1
    Dim Scope As New ManagementScope("\\.\ROOT\cimv2")
    Dim query As New ObjectQuery
    Dim SystemLog As String = ""
    Dim percent As Integer = 0
    Dim driverFound As Boolean = False
    Dim stream As System.IO.StreamWriter
    Dim fileName As String
    Dim pHelp As New ProcessStartInfo
    Private Sub OK_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles nextBtn.Click
        Dim form As New Form1
        form.ShowDialog()
        Me.Dispose()
        Me.Close()
    End Sub

    Private Sub Install_click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Install.Click
        Timer1.Interval = 500
        percent = 0
        Label1.Text = "Installing Drivers"
        Timer1.Enabled = True
        Dim t1 As Threading.Thread
        With pHelp
            .FileName = "Huawei_Drivers\DriverSetup.exe"
            .CreateNoWindow = True
            .UseShellExecute = False
            .WindowStyle = ProcessWindowStyle.Normal
            .Verb = "runas"

        End With
        t1 = New Threading.Thread(AddressOf execFile)
        t1.Start()
        t1.Join()
        With pHelp
            .FileName = "FcSerialDrv\DriverSetup.exe"
            .CreateNoWindow = True
            .UseShellExecute = False
            .WindowStyle = ProcessWindowStyle.Normal
            .Verb = "runas"
        End With
        t1 = New Threading.Thread(AddressOf execFile)
        t1.Start()
        t1.Join()
        Label1.Text = "Scanning drivers"

        'Application.Restart()
        LoginForm1_Load(sender, e)

    End Sub
    Private Function execFile()
        Dim proc As Process
        proc = Process.Start(pHelp)
        proc.WaitForExit()
        Dim verb As String
        For Each verb In pHelp.Verbs
            Console.WriteLine(". {0}", verb)
        Next


        Return True
    End Function
    Private Sub LoginForm1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Control.CheckForIllegalCrossThreadCalls = False
        AddHandler Me.Paint, AddressOf paintMe
        'MsgBox(Process.GetProcessesByName("ModemUnlocker.exe")(0).StartInfo.UserName)
        Label1.Text = "Searching for drivers"
      
        'write data to file
        stream = New System.IO.StreamWriter("systemlog.txt", False)
        stream.WriteLine("system log:")
        stream.WriteLine("------------------------------------------------------------------")
        stream.Close()
        Timer1.Enabled = True
        Dim t1 As Thread
        t1 = New Threading.Thread(AddressOf listDrivers)
        t1.Start()
        t1.Join()

        'listDrivers()
        'listModems()
        'listSerialPorts()
        t1 = New Thread(AddressOf listSerialPorts)
        t1.Start()
        t1.Join()
        t1 = New Thread(AddressOf listModems)
        t1.Start()
        t1.Join()
        t1 = New Thread(AddressOf listModemToSerial)
        t1.Start()
        t1.Join()


    End Sub


    Private Function listModems() As Boolean

        'Collecting Data modems information
        query.QueryString = "SELECT * FROM Win32_potsModem"
        Dim mgmtSearcher As ManagementObjectSearcher = New ManagementObjectSearcher(Scope, query)
        SystemLog = "Connected Modems:" & vbCrLf
        For Each mgmtObject As ManagementObject In mgmtSearcher.Get
            For Each propItem As PropertyData In mgmtObject.Properties
                Try
                    SystemLog = SystemLog & propItem.Name & ": "
                    SystemLog = SystemLog & propItem.Value & vbCrLf

                Catch ex As Exception
                    SystemLog = SystemLog & vbCrLf
                End Try
            Next
            SystemLog = SystemLog & vbCrLf & ("------------------------------------------------------------------") & vbcrlf
        Next
        Dim modemStream As System.IO.StreamWriter
        modemStream = New System.IO.StreamWriter("SystemLog.txt", True)
        modemStream.Write(SystemLog)
        modemStream.Write("------------------------------------------------------------------")
        modemStream.Close()

        Return True
    End Function
    Private Function listModemToSerial() As Boolean

        'collecting modem to serial port data
        SystemLog = "Modem to serial port data" & vbCrLf & vbCrLf
        query.QueryString = "SELECT * FROM Win32_POTSModemToSerialPort"
        Dim modemToSerialPort As ManagementObjectSearcher = New ManagementObjectSearcher(Scope, query)
        For Each obj As ManagementObject In modemToSerialPort.Get
            For Each prop As PropertyData In obj.Properties
                Try
                    SystemLog = SystemLog & prop.Name & ": " & prop.Value & vbCrLf
                Catch ex As Exception
                    SystemLog = SystemLog & vbCrLf
                End Try
            Next
            SystemLog = SystemLog & vbCrLf & ("------------------------------------------------------------------") & vbcrlf
        Next
        stream = New System.IO.StreamWriter("SystemLog.txt", True)
        stream.WriteLine(SystemLog)
        stream.WriteLine("------------------------------------------------------------------")
        stream.Close()

        Return modemToSerialPort.Get.Count <> 0
    End Function


    Private Function listSerialPorts()

        'collecting all ports details
        SystemLog = "All detected ports." & vbCrLf & ("------------------------------------------------------------------") & vbcrlf

        For Each port As String In My.Computer.Ports.SerialPortNames
            SystemLog = SystemLog & port & vbCrLf
        Next

        'collecting serial port data  
        SystemLog = SystemLog & vbCrLf & vbCrLf & "Serial port data" & vbCrLf & vbCrLf
        query.QueryString = "SELECT * FROM Win32_SerialPort"
        Dim serialPortSearcher As ManagementObjectSearcher = New ManagementObjectSearcher(Scope, query)
        For Each queryObj As ManagementObject In serialPortSearcher.Get()
            For Each prop As PropertyData In queryObj.Properties
                Try
                    SystemLog = SystemLog & prop.Name & ": " & prop.Value & vbCrLf
                Catch ex As Exception
                    SystemLog = SystemLog & vbCrLf
                End Try
            Next
            SystemLog = SystemLog & vbCrLf & ("------------------------------------------------------------------") & vbcrlf
        Next
        stream = New System.IO.StreamWriter("SystemLog.txt", True)
        stream.WriteLine(SystemLog)
        stream.WriteLine("------------------------------------------------------------------")
        stream.Close()

        If serialPortSearcher.Get.Count > 0 Then
            Return True
        End If
        Return False
    End Function
    Private Function listDrivers()

        'collecting driver data
        SystemLog = "Drivers database..query results:" & vbCrLf & ("------------------------------------------------------------------") & vbcrlf
        query.QueryString = "SELECT * FROM Win32_PnPSignedDriver"
        Dim searcher As ManagementObjectSearcher = New ManagementObjectSearcher(Scope, query)
        For Each driver As ManagementObject In searcher.Get
            For Each prop As PropertyData In driver.Properties
                If prop.Name = "DeviceName" Then
                    Try
                        SystemLog = SystemLog & prop.Value & vbCrLf
                        If prop.Value.ToString.Contains("HUAWEI") Then
                            driverFound = True
                        End If
                    Catch ex As Exception
                        SystemLog = SystemLog & vbCrLf
                    End Try

                End If
            Next
        Next
        stream = New System.IO.StreamWriter("SystemLog.txt", True)
        stream.Write(SystemLog)
        stream.WriteLine("------------------------------------------------------------------")
        stream.Close()


        Return driverFound
    End Function


    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If percent <= 98 Then
            percent = percent + 2
            Me.Refresh()
        Else
            If driverFound Then
                Label1.Text = "Drivers Found. Please wait."

            Else
                Label1.Text = "Drivers Not Found. Install now?"
                Install.Visible = True
                nextBtn.Visible = True
                Install.Text = "Install and Continue"
            End If
            percent = 0
            Me.Refresh()
            Timer1.Enabled = False
            Timer1.Dispose()
            Form1.ShowDialog()
            Me.Hide()
            Me.Close()
        End If


    End Sub

    Private Sub paintMe(sender As Object, e As PaintEventArgs)
        'Throw New NotImplementedException

        Dim g As Graphics = e.Graphics
        Dim rect As New Rectangle(Me.Width / 2 - 50, Me.Height / 2 - 50, 90, 90)


        Dim curve_progress = CSng(360 / 100 * percent)
        Dim curvatura_rimanente = 360 - curve_progress


        Using tratto_progresso As New Pen(Color.LimeGreen, 8), tratto_rimanente As New Pen(Color.White, 8)
            If percent <> 0 Then
                PictureBox2.Visible = False
                g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                g.DrawArc(tratto_progresso, rect, -90, curve_progress)
                g.DrawArc(tratto_rimanente, rect, curve_progress - 90, curvatura_rimanente)
            Else
                PictureBox2.Visible = True
            End If
        End Using

        Using fnt As New Font(Me.Font.FontFamily, 14)

            Dim text As String = percent.ToString + "%"

            Dim textSize = g.MeasureString(text, fnt)
            Dim textPoint As New Point(CInt(rect.Left + (rect.Width / 2) - (textSize.Width / 2)), CInt(rect.Top + (rect.Height / 2) - (textSize.Height / 2)))
            If percent <> 0 Then
                PictureBox2.Visible = False
                g.DrawString(text, fnt, Brushes.Black, textPoint)
            Else
                PictureBox2.Visible = True
            End If
        End Using
    End Sub

End Class

