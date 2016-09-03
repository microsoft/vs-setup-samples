Visual Studio Setup Configuration Samples
=========================================

The included samples show how to use the new setup configuration API for discovering instances of Visual Studio "15".

Visual Studio Instances
-----------------------

You can install multiple instances of Visual Studio with different workloads, or from different channels or versions. The majority of components are now lightweight installs using the same container format as VSIX packages for faster installs and cleaner uninstalls.

This change to the Visual Studio platform requires new ways of discovering Visual Studio where simple registry detection does not provide the flexibility required in many scenarios.

The new setup configuration API provides simple access for native and managed code, as well as a means to output JSON, XML, or other formats for consumption in batch or PowerShell scripts.

Available Packages
------------------

The following packages are available on [nuget.org](https://nuget.org) that provide access to the setup configuration API.

*   **[Microsoft.VisualStudio.Setup.Configuration.Native](https://www.nuget.org/packages/Microsoft.VisualStudio.Setup.Configuration.Native/)**

    Adds the header location and automatically links the library. You only need to add the `#include` as shown below.

    ```c++
    #include <Setup.Configuration.h>
    ```

*   **[Microsoft.VisualStudio.Setup.Configuration.Interop](https://www.nuget.org/packages/Microsoft.VisualStudio.Setup.Configuration.Interop/)**

    Provides embeddable interop types. If the interop types are embedded you do not need to redistribute additional assemblies. Simply instantiate the `SetupConfiguration` runtime callable wrapper (RCW) as shown below.

    ```c#
    var configuration = new SetupConfiguration();
    ```

Updates
-------

For the most recent updates to these samples, please see [https://github.com/microsoft/vs-setup-samples](https://github.com/microsoft/vs-setup-samples).
