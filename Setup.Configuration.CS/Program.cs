// <copyright file="Program.cs" company="Microsoft Corporation">
// Copyright (C) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the project root for license information.
// </copyright>

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;

/// <summary>
/// The main program class.
/// </summary>
internal class Program
{
    private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

    /// <summary>
    /// Program entry point.
    /// </summary>
    /// <param name="args">Command line arguments passed to the program.</param>
    /// <returns>The process exit code.</returns>
    internal static int Main(string[] args)
    {
        try
        {
            var query = new SetupConfiguration();
            var query2 = (ISetupConfiguration2)query;
            var e = query2.EnumAllInstances();

            var helper = (ISetupHelper)query;

            int fetched;
            var instances = new ISetupInstance[1];
            do
            {
                e.Next(1, instances, out fetched);
                if (fetched > 0)
                {
                    PrintInstance(instances[0], helper);
                }
            }
            while (fetched > 0);

            return 0;
        }
        catch (COMException ex) when (ex.HResult == REGDB_E_CLASSNOTREG)
        {
            Console.WriteLine("The query API is not registered. Assuming no instances are installed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error 0x{ex.HResult:x8}: {ex.Message}");
            return ex.HResult;
        }
    }

    private static void PrintInstance(ISetupInstance instance, ISetupHelper helper)
    {
        var instance2 = (ISetupInstance2)instance;
        var state = instance2.GetState();
        Console.WriteLine($"InstanceId: {instance2.GetInstanceId()} ({(state == InstanceState.Complete ? "Complete" : "Incomplete")})");

        var installationVersion = instance.GetInstallationVersion();
        var version = helper.ParseVersion(installationVersion);

        Console.WriteLine($"InstallationVersion: {installationVersion} ({version})");

        if ((state & InstanceState.Local) == InstanceState.Local)
        {
            Console.WriteLine($"InstallationPath: {instance2.GetInstallationPath()}");
        }

        if ((state & InstanceState.Registered) == InstanceState.Registered)
        {
            Console.WriteLine($"Product: {instance2.GetProduct().GetId()}");
            Console.WriteLine("Workloads:");

            PrintWorkloads(instance2.GetPackages());
        }

        Console.WriteLine();
    }

    private static void PrintWorkloads(ISetupPackageReference[] packages)
    {
        var workloads = from package in packages
                        where string.Equals(package.GetType(), "Workload", StringComparison.OrdinalIgnoreCase)
                        orderby package.GetId()
                        select package;

        foreach (var workload in workloads)
        {
            Console.WriteLine($"    {workload.GetId()}");
        }
    }

    [DllImport("Microsoft.VisualStudio.Setup.Configuration.Native.dll", ExactSpelling = true, PreserveSig = true)]
    private static extern int GetSetupConfiguration(
        [MarshalAs(UnmanagedType.Interface), Out] out ISetupConfiguration configuration,
        IntPtr reserved);
}
