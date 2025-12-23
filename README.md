# ARCtrl.Querymodel
 
| Version | Downloads |
| :--------|-----------:|
|<a href="https://www.nuget.org/packages/ARCtrl.QueryModel/"><img alt="Nuget" src="https://img.shields.io/nuget/v/ARCtrl.QueryModel?logo=nuget&color=%234fb3d9"></a>|<a href="https://www.nuget.org/packages/ARCtrl.QueryModel/"><img alt="Nuget" src="https://img.shields.io/nuget/dt/ARCtrl.QueryModel?color=%234FB3D9"></a>|
|<a href="https://www.npmjs.com/package/@nfdi4plants/arcquerymodel"><img alt="NPM" src="https://img.shields.io/npm/v/%40nfdi4plants/arcquerymodel?logo=npm&color=%234fb3d9"></a>|<a href="https://www.npmjs.com/package/@nfdi4plants/arcquerymodel"><img alt="NPM" src="https://img.shields.io/npm/dt/%40nfdi4plants%2Farcquerymodel?color=%234fb3d9"></a>|
|<a href="https://pypi.org/project/arcquerymodel/"><img alt="PyPI" src="https://img.shields.io/pypi/v/arcquerymodel?logo=pypi&color=%234fb3d9"></a>|<a href="https://pypi.org/project/arcquerymodel/"><img alt="PyPI" src="https://img.shields.io/pepy/dt/arcquerymodel?color=%234fb3d9"></a>|

Adds querying functionality to the core [ARCtrl](https://github.com/nfdi4plants/ARCtrl) package in .NET.

The documentation for the actual functions for manipulating the ARC datamodel can be found [here](https://github.com/nfdi4plants/ARCtrl/tree/main/docs/scripts_fsharp).

## Usage

```fsharp
open ARCtrl
open ARCtrl.QueryModel
open ARCtrl.ISA

let i = ArcInvestigation("Dummy Investigation")

i.ArcTables.Values().WithName("Dummy Header").First.ValueText

i.GetAssay("Dummy Assay").LastSamples
```

## Development

#### Requirements

- [.NET SDK](https://dotnet.microsoft.com/en-us/download)
    - verify with `dotnet --version` (Tested with 7.0.306)

#### Local Setup

- Setup dotnet tools `dotnet tool restore`

- Verify correct setup with  `./build.cmd runtests` ✨
