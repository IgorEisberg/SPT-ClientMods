param([string]$GameRoot = (Split-Path (Split-Path $PSScriptRoot)), [string[]]$Projects = @('CustomInteractions.Prepatch','CustomInteractions','ItemAttributeFix','MagazineInspector','MunitionsExpert','ItemSellPrice','ItemContextMenuExt'))
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$gameRoot = [IO.Path]::GetFullPath($GameRoot)
$out = Join-Path $root 'artifacts'
$null = New-Item -ItemType Directory -Force $out
$null = [Reflection.Assembly]::LoadFrom("$PSHOME/Microsoft.CodeAnalysis.dll")
$null = [Reflection.Assembly]::LoadFrom("$PSHOME/Microsoft.CodeAnalysis.CSharp.dll")
$parse = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithLanguageVersion([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::Latest)
$options = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary).WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release).WithDeterministic($true)
$references = [Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new()
$paths = @(Get-ChildItem "$gameRoot/EscapeFromTarkov_Data/Managed" -Filter *.dll | ForEach-Object FullName) + @("$gameRoot/BepInEx/core/BepInEx.dll", "$gameRoot/BepInEx/core/Mono.Cecil.dll", "$gameRoot/BepInEx/plugins/spt/spt-reflection.dll")
foreach ($path in $paths) { $references.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($path)) }
$failed = $false
foreach ($project in $Projects) {
    $trees = [Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]]::new()
    foreach ($file in Get-ChildItem "$root/$project" -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' -and $_.Name -ne 'Resources.Designer.cs' }) {
        $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($file.FullName), $parse, $file.FullName))
    }
    [xml]$projectXml = Get-Content "$root/$project/$project.csproj"
    $version = $projectXml.Project.PropertyGroup.Version
    $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText('[assembly: System.Reflection.AssemblyVersion("' + $version + '")]', $parse))
    $refs = [Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]]::new($references)
    if ($project -eq 'ItemContextMenuExt') { $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile("$out/IcyClawz.CustomInteractions.dll")) }
    $resources = [Collections.Generic.List[Microsoft.CodeAnalysis.ResourceDescription]]::new()
    if ($project -eq 'MunitionsExpert') {
        foreach ($png in Get-ChildItem "$root/$project/Resources" -Filter *.png) {
            $bytes = [IO.File]::ReadAllBytes($png.FullName)
            $provider = { [IO.MemoryStream]::new($bytes, $false) }.GetNewClosure()
            $resources.Add([Microsoft.CodeAnalysis.ResourceDescription]::new("IcyClawz.MunitionsExpert.Resources.$($png.Name)", [Func[IO.Stream]]$provider, $true))
        }
    }
    $compilation = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create("IcyClawz.$project", $trees, $refs, $options)
    $stream = [IO.File]::Create("$out/IcyClawz.$project.dll")
    try { $result = $compilation.Emit($stream, $null, $null, $null, $resources) } finally { $stream.Dispose() }
    "$project : $($result.Success)"
    $result.Diagnostics | Where-Object Severity -in 'Error','Warning' | ForEach-Object ToString | Select-Object -Unique
    if (-not $result.Success) { $failed = $true }
}
if ($failed) { exit 1 }
