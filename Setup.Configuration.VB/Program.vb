' <copyright file="Program.vb" company="Microsoft Corporation">
' Copyright (C) Microsoft Corporation. All rights reserved.
' Licensed under the MIT license. See LICENSE.txt in the project root for license information.
' </copyright>

Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.Setup.Configuration

''' <summary>
''' The main program class.
''' </summary>
Friend Module Program

    Private Const REGDB_E_CLASSNOTREG As Integer = &H80040154

    ''' <summary>
    ''' Command line arguments passed to the program.
    ''' </summary>
    ''' <param name="args">Command line arguments passed to the program.</param>
    Friend Function Main(args As String()) As Integer
        Try
            Dim query = New SetupConfiguration()
            Dim query2 = CType(query, ISetupConfiguration2)
            Dim e = query2.EnumAllInstances()

            Dim helper = CType(query, ISetupHelper)

            Dim fetched = 0
            Dim instances(1) As ISetupInstance
            Do
                e.Next(1, instances, fetched)
                If fetched > 0 Then
                    PrintInstance(instances(0), helper)
                End If
            Loop Until fetched = 0

            Return 0
        Catch ex As COMException When ex.HResult = REGDB_E_CLASSNOTREG
            Console.WriteLine("The query API is not registered. Assuming no instances are installed.")
            Return 0
        Catch ex As Exception
            Console.Error.WriteLine($"Error 0x{ex.HResult:x8}: {ex.Message}")
            Return ex.HResult
        End Try
    End Function

    Private Sub PrintInstance(instance As ISetupInstance, helper As ISetupHelper)
        Dim instance2 = CType(instance, ISetupInstance2)
        Dim state = instance2.GetState()
        Console.WriteLine($"InstanceId: {instance2.GetInstanceId()} ({If(state = InstanceState.Complete, "Complete", "Incomplete")})")

        Dim installationVersion = instance2.GetInstallationVersion()
        Dim version = helper.ParseVersion(installationVersion)

        Console.WriteLine($"InstallationVersion: {installationVersion} ({version})")

        If (state And InstanceState.Local) = InstanceState.Local Then
            Console.WriteLine($"InstallationPath: {instance2.GetInstallationPath()}")
        End If

        Dim catalog = TryCast(instance, ISetupInstanceCatalog)
        If Not catalog Is Nothing Then
            Console.WriteLine($"IsPrerelease: {catalog.IsPrerelease()}")
        End If

        If (state And InstanceState.Registered) = InstanceState.Registered Then
            Console.WriteLine($"Product: {instance2.GetProduct().GetId()}")
            Console.WriteLine("Workloads:")

            PrintWorkloads(instance2.GetPackages())
        End If

        Dim properties = instance2.GetProperties()
        If Not properties Is Nothing Then
            Console.WriteLine("Custom properties:")
            PrintProperties(properties)
        End If

        properties = catalog?.GetCatalogInfo()
        If Not properties Is Nothing Then
            Console.WriteLine("Catalog properties:")
            PrintProperties(properties)
        End If

        Console.WriteLine()
    End Sub

    Private Sub PrintProperties(store As ISetupPropertyStore)
        Dim properties = From name In store.GetNames()
                         Order By name
                         Select New With {.Name = name, .Value = store.GetValue(name)}

        For Each prop In properties
            Console.WriteLine($"    {prop.Name}: {prop.Value}")
        Next
    End Sub

    Private Sub PrintWorkloads(packages As ISetupPackageReference())
        Dim workloads = From package In packages
                        Where String.Equals(package.GetType(), "Workload", StringComparison.OrdinalIgnoreCase)
                        Order By package.GetId()

        For Each workload In workloads
            Console.WriteLine($"    {workload.GetId()}")
        Next
    End Sub

End Module
