# Swa.Analyzers

Pacote de analyzers Roslyn focado em regras arquiteturais e convenções de testes para reuso interno.

## Visão geral

- Projeto de analyzer: `Swa.Analyzers.Core`
- Projeto de testes: `Swa.Analyzers.Tests`
- Documentação de regras: `docs/rules`

## Regras

| ID | Título | Status |
|---|---|---|
| ARCH001 | Permitir apenas NSubstitute como biblioteca de mock | ✅ Implementada |
| ARCH002..ARCH024 | Regras arquiteturais adicionais do roadmap | ⏳ Planejada |

## Instalação / referência

```xml
<ItemGroup>
  <PackageReference Include="Swa.Analyzers" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

Para referência local em solução:

```xml
<ItemGroup>
  <ProjectReference Include="../Swa.Analyzers.Core/Swa.Analyzers.Core.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Configuração via `.editorconfig`

Exemplo para ARCH001:

```ini
[*.cs]
dotnet_diagnostic.ARCH001.only_test_projects = true
dotnet_diagnostic.ARCH001.blocked_namespaces = Moq,FakeItEasy,Rhino.Mocks
dotnet_diagnostic.ARCH001.test_project_patterns = test;tests;spec
```

## Executando testes

```bash
dotnet test Swa.Analyzers.slnx
```
