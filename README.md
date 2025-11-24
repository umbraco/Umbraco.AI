# Umbraco.Ai

## Getting Started

### Setting Up a Demo Site

To quickly set up a local demo site for development and testing:

```powershell
.\scripts\Install-DemoSite.ps1
```

This script will:
- Create a demo folder with a new Umbraco site (`Umbraco.Ai.DemoSite`)
- Copy `Umbraco.Ai.sln` to `Umbraco.Ai.local.sln`
- Add the demo site to the local solution in a "Demo" solution folder
- Add project references to `Umbraco.Ai.Startup` and `Umbraco.Ai.Web.StaticAssets`
- Configure the demo site to work with local package development (disables central package management)

After running the script, you can open `Umbraco.Ai.local.sln` to work with both the package source code and the demo site.