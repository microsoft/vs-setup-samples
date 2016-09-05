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
            Dim query = GetQuery()
            Dim query2 = CType(query, ISetupConfiguration2)
            Dim e = query2.EnumAllInstances()

            Dim fetched = 0
            Dim instances(1) As ISetupInstance
            Do
                e.Next(1, instances, fetched)
                If fetched > 0 Then
                    PrintInstance(instances(0))
                End If
            Loop Until fetched = 0

            Return 0
        Catch ex As Exception
            Console.Error.WriteLine($"Error 0x{ex.HResult:x8}: {ex.Message}")
            Return ex.HResult
        End Try
    End Function

    Private Function GetQuery() As ISetupConfiguration
        Try
            ' Try to CoCreate the class object.
            Return New SetupConfiguration()
        Catch ex As COMException When ex.HResult = REGDB_E_CLASSNOTREG
            ' Try to get the class object using app-local call.
            Dim query As ISetupConfiguration = Nothing
            Dim result = GetSetupConfiguration(query, IntPtr.Zero)

            If result < 0 Then Throw New COMException("Failed to get query class", result)

            Return query
        End Try
    End Function

    Private Sub PrintInstance(instance As ISetupInstance)
        Dim instance2 = CType(instance, ISetupInstance2)
        Dim state = instance2.GetState()
        Console.WriteLine($"InstanceId: {instance2.GetInstanceId()} ({If(state = InstanceState.Complete, "Complete", "Incomplete")})")

        If (state And InstanceState.Local) = InstanceState.Local Then
            Console.WriteLine($"InstallationPath: {instance2.GetInstallationPath()}")
        End If

        If (state And InstanceState.Registered) = InstanceState.Registered Then
            Console.WriteLine($"Product: {instance2.GetProduct().GetId()}")
            Console.WriteLine("Workloads:")

            PrintWorkloads(instance2.GetPackages())
        End If

        Console.WriteLine()
    End Sub

    Private Sub PrintWorkloads(packages As ISetupPackageReference())
        Dim workloads = From package In packages
                        Where String.Equals(package.GetType(), "Workload", StringComparison.OrdinalIgnoreCase)
                        Order By package.GetId()

        For Each workload In workloads
            Console.WriteLine($"    {workload.GetId()}")
        Next
    End Sub

    Private Declare Unicode Function GetSetupConfiguration Lib "Microsoft.VisualStudio.Setup.Configuration.Native.dll" (
        <MarshalAs(UnmanagedType.Interface), Out> ByRef configuration As ISetupConfiguration,
        reserved As IntPtr) As Integer

End Module
