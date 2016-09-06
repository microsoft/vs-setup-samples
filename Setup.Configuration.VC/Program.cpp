// <copyright file="Program.cpp" company="Microsoft Corporation">
// Copyright (C) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt in the project root for license information.
// </copyright>

#include "stdafx.h"

using namespace std;

// Use smart pointers (without ATL) to release objects when they fall out of scope.
_COM_SMARTPTR_TYPEDEF(ISetupInstance, __uuidof(ISetupInstance));
_COM_SMARTPTR_TYPEDEF(ISetupInstance2, __uuidof(ISetupInstance2));
_COM_SMARTPTR_TYPEDEF(IEnumSetupInstances, __uuidof(IEnumSetupInstances));
_COM_SMARTPTR_TYPEDEF(ISetupConfiguration, __uuidof(ISetupConfiguration));
_COM_SMARTPTR_TYPEDEF(ISetupConfiguration2, __uuidof(ISetupConfiguration2));
_COM_SMARTPTR_TYPEDEF(ISetupHelper, __uuidof(ISetupHelper));
_COM_SMARTPTR_TYPEDEF(ISetupPackageReference, __uuidof(ISetupPackageReference));

module_ptr GetQuery(
    _Outptr_result_maybenull_ ISetupConfiguration** ppQuery
);

void PrintInstance(
    _In_ ISetupInstance* pInstance,
    _In_ ISetupHelper* pHelper
);

void PrintPackageReference(
    _In_ ISetupPackageReference* pPackage
);

void PrintWorkloads(
    _In_ LPSAFEARRAY psaPackages
);

DWORD wmain(
    _In_ int argc,
    _In_ LPCWSTR argv[]
)
{
    UNREFERENCED_PARAMETER(argc);
    UNREFERENCED_PARAMETER(argv);

    try
    {
        CoInitializer init;

        ISetupConfigurationPtr query;
        auto lib = GetQuery(&query);

        ISetupConfiguration2Ptr query2(query);
        IEnumSetupInstancesPtr e;

        auto hr = query2->EnumAllInstances(&e);
        if (FAILED(hr))
        {
            throw win32_exception(hr, "failed to query all instances");
        }

        ISetupHelperPtr helper(query);

        ISetupInstance* pInstances[1] = {};
        hr = e->Next(1, pInstances, NULL);
        while (S_OK == hr)
        {
            // Wrap instance without AddRef'ing.
            ISetupInstancePtr instance(pInstances[0], false);
            PrintInstance(instance, helper);

            hr = e->Next(1, pInstances, NULL);
        }

        if (FAILED(hr))
        {
            throw win32_exception(hr, "failed to enumerate all instances");
        }
    }
    catch (win32_exception& ex)
    {
        cerr << hex << "Error 0x" << ex.code() << ": " << ex.what() << endl;
        return ex.code();
    }
    catch (exception& ex)
    {
        cerr << "Error: " << ex.what() << endl;
        return E_FAIL;
    }

    return ERROR_SUCCESS;
}

module_ptr GetQuery(
    _Outptr_result_maybenull_ ISetupConfiguration** ppQuery
)
{
    typedef HRESULT(CALLBACK* LPFNGETCONFIGURATION)(_Out_ ISetupConfiguration** ppConfiguration, _Reserved_ LPVOID pReserved);

    const WCHAR wzLibrary[] = L"Microsoft.VisualStudio.Setup.Configuration.Native.dll";
    const CHAR szFunction[] = "GetSetupConfiguration";

    // As with COM, make sure we return a NULL pointer on error.
    _ASSERT(ppQuery);
    *ppQuery = NULL;

    ISetupConfigurationPtr query;

    // Try to create the CoCreate the class; if that fails, likely no instances are registered.
    auto hr = query.CreateInstance(__uuidof(SetupConfiguration));
    if (SUCCEEDED(hr))
    {
        *ppQuery = query;
        return nullptr;
    }
    else if (REGDB_E_CLASSNOTREG != hr)
    {
        throw win32_exception(hr, "failed to create query class");
    }

    // We can otherwise attempt to load the library from the PATH.
    auto hConfiguration = ::LoadLibraryW(wzLibrary);
    if (!hConfiguration)
    {
        throw win32_exception(REGDB_E_CLASSNOTREG, "failed to load configuration library");
    }

    // Make sure the module is freed when it falls out of scope.
    module_ptr lib(&hConfiguration);

    auto fnGetConfiguration = reinterpret_cast<LPFNGETCONFIGURATION>(::GetProcAddress(hConfiguration, szFunction));
    if (!fnGetConfiguration)
    {
        throw win32_exception(CLASS_E_CLASSNOTAVAILABLE, "could not find the expected entry point");
    }

    hr = fnGetConfiguration(ppQuery, NULL);
    if (FAILED(hr))
    {
        throw win32_exception(hr, "failed to get query class");
    }

    return lib;
}

void PrintInstance(
    _In_ ISetupInstance* pInstance,
    _In_ ISetupHelper* pHelper
)
{
    HRESULT hr = S_OK;
    ISetupInstance2Ptr instance(pInstance);

    bstr_t bstrId;
    if (FAILED(hr = instance->GetInstanceId(bstrId.GetAddress())))
    {
        throw win32_exception(hr, "failed to get InstanceId");
    }

    InstanceState state;
    if (FAILED(hr = instance->GetState(&state)))
    {
        throw win32_exception(hr, "failed to get State");
    }

    wcout << L"InstanceId: " << bstrId << L" (" << (eComplete == state ? L"Complete" : L"Incomplete") << L")" << endl;

    bstr_t bstrVersion;
    if (FAILED(hr = instance->GetInstallationVersion(bstrVersion.GetAddress())))
    {
        throw win32_exception(hr, "failed to get InstallationVersion");
    }

    ULONGLONG ullVersion;
    if (FAILED(hr = pHelper->ParseVersion(bstrVersion, &ullVersion)))
    {
        throw win32_exception(hr, "failed to parse InstallationVersion");
    }

    wcout << L"InstallationVersion: " << bstrVersion << L" (" << ullVersion << L")" << endl;

    // Reboot may have been required before the installation path was created.
    if ((eLocal & state) == eLocal)
    {
        bstr_t bstrInstallationPath;
        if (FAILED(hr = instance->GetInstallationPath(bstrInstallationPath.GetAddress())))
        {
            throw win32_exception(hr, "failed to get InstallationPath");
        }

        wcout << L"InstallationPath: " << bstrInstallationPath << endl;
    }

    // Reboot may have been required before the product package was registered (last).
    if ((eRegistered & state) == eRegistered)
    {
        ISetupPackageReferencePtr product;
        if (FAILED(hr = instance->GetProduct(&product)))
        {
            throw win32_exception(hr, "failed to get Product");
        }

        wcout << L"Product: ";
        PrintPackageReference(product);

        wcout << endl;

        LPSAFEARRAY psa = NULL;
        if (FAILED(hr = instance->GetPackages(&psa)))
        {
            throw win32_exception(hr, "failed to get Packages");
        }

        // Make sure the SAFEARRAY is freed when it falls out of scope.
        safearray_ptr psa_ptr(&psa);

        wcout << L"Workloads:" << endl;
        PrintWorkloads(psa);
    }

    wcout << endl;
}

void PrintPackageReference(
    _In_ ISetupPackageReference* pPackage
)
{
    HRESULT hr = S_OK;
    ISetupPackageReferencePtr ref(pPackage);
    
    bstr_t bstrId;
    if (FAILED(hr = ref->GetId(bstrId.GetAddress())))
    {
        throw win32_exception(hr, "failed to get reference Id");
    }

    // Check that an ID is registered; unexpected otherwise, but would throw in RCW.
    if (!!bstrId)
    {
        wcout << bstrId;
    }
}

void PrintWorkloads(
    _In_ LPSAFEARRAY psaPackages
)
{
    // Lock the SAFEARRAY to get the raw pointer array.
    auto hr = ::SafeArrayLock(psaPackages);
    if (FAILED(hr))
    {
        throw win32_exception(hr, "failed to lock package arrays");
    }

    auto rgpPackages = reinterpret_cast<ISetupPackageReference**>(psaPackages->pvData);
    auto cPackages = psaPackages->rgsabound[0].cElements;

    if (0 == cPackages)
    {
        return;
    }

    vector<ISetupPackageReference*> packages(rgpPackages, rgpPackages + cPackages);

    const WCHAR wzType[] = L"Workload";
    const size_t cchType = sizeof(wzType) / sizeof(WCHAR) - 1;

    // Find all the workload package types.
    vector<ISetupPackageReference*> workloads;
    for (auto pPackage : packages)
    {
        bstr_t bstrType;

        if (SUCCEEDED(hr = pPackage->GetType(bstrType.GetAddress())))
        {
            if (cchType == bstrType.length() && 0 == _wcsnicmp(wzType, bstrType, cchType))
            {
                workloads.push_back(pPackage);
            }
        }
    }

    sort(workloads.begin(), workloads.end(), [&](ISetupPackageReference* pA, ISetupPackageReference* pB) -> bool
    {
        bstr_t bstrA;
        bstr_t bstrB;

        if (SUCCEEDED(hr = pA->GetId(bstrA.GetAddress())))
        {
            if (SUCCEEDED(hr = pB->GetId(bstrB.GetAddress())))
            {
                return 0 > _wcsicmp(bstrA, bstrB);
            }
        }

        return 0 > _wcsicmp(__nameof(bstrA), __nameof(bstrB));
    });

    for_each(workloads.begin(), workloads.end(), [&](ISetupPackageReference* pWorkload)
    {
        wcout << L"    ";
        PrintPackageReference(pWorkload);

        wcout << endl;
    });

    // SafeArrayDeleter will unlock if exception thrown.
    ::SafeArrayUnlock(psaPackages);
}
