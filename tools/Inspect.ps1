param([string[]]$Names, [string]$Method, [string]$Assembly = 'Assembly-CSharp', [string]$GameRoot = (Split-Path (Split-Path $PSScriptRoot)))
$ErrorActionPreference = 'Stop'
$null = [Reflection.Assembly]::LoadFrom("$GameRoot/BepInEx/core/Mono.Cecil.dll")
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly("$GameRoot/EscapeFromTarkov_Data/Managed/$Assembly.dll")
function Types($ts) { foreach ($t in $ts) { $t; Types $t.NestedTypes } }
foreach ($t in (Types $a.MainModule.Types) | Where-Object { $_.Name -in $Names -or $_.FullName -in $Names }) {
    "TYPE $($t.FullName) BASE $($t.BaseType)"
    if ($Method) {
        foreach ($m in $t.Methods | Where-Object Name -eq $Method) {
            $m.FullName
            $m.Parameters | ForEach-Object { "PARAM $($_.Name) $($_.ParameterType)" }
            $m.Body.Instructions | ForEach-Object ToString
        }
    } else {
        $t.Fields | ForEach-Object { "FIELD $($_.Attributes) $($_.FieldType) $($_.Name)" }
        $t.Methods | ForEach-Object { "METHOD $($_.Attributes) $($_.FullName)" }
    }
}
