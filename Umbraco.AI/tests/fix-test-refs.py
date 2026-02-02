import os
from pathlib import Path

# List of src projects that need path fix
src_projects = [
    'Umbraco.AI.Core',
    'Umbraco.AI.Persistence',
    'Umbraco.AI.Persistence.SqlServer',
    'Umbraco.AI.Persistence.Sqlite',
    'Umbraco.AI.Web',
    'Umbraco.AI.Web.StaticAssets',
    'Umbraco.AI.Startup',
    'Umbraco.AI'
]

test_files = [
    'Umbraco.AI.Tests.Common/Umbraco.AI.Tests.Common.csproj',
    'Umbraco.AI.Tests.Integration/Umbraco.AI.Tests.Integration.csproj',
    'Umbraco.AI.Tests.Unit/Umbraco.AI.Tests.Unit.csproj'
]

for test_file in test_files:
    if not os.path.exists(test_file):
        print(f"Warning: {test_file} not found")
        continue
    
    with open(test_file, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Fix each src project reference
    for project in src_projects:
        # Replace ..\ProjectName\ with ..\..\src\ProjectName\
        old_pattern = f'..\{project}\'
        new_pattern = f'..\..\src\{project}\'
        if old_pattern in content:
            content = content.replace(old_pattern, new_pattern)
            print(f"Fixed {project} reference in {test_file}")
    
    with open(test_file, 'w', encoding='utf-8') as f:
        f.write(content)

print("Done fixing test project references")
