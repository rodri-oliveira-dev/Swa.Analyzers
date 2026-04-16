# Swa.Analyzers

Pacote de analyzers Roslyn para reforçar convenções de arquitetura, testes e qualidade de código em projetos .NET.

## Visão geral
Este repositório contém:
- `Swa.Analyzers.Core`: implementação dos analyzers.
- `Swa.Analyzers.Tests`: testes automatizados dos analyzers.
- `docs/rules`: documentação por regra.

## Tabela de regras
| Regra | Descrição | Status |
|---|---|---|
| ARCH001 | Permitir apenas NSubstitute como biblioteca de mock | Implementada |
| ARCH002-ARCH024 | Regras planejadas | Pendente |

## Instalação / referência
Enquanto pacote interno/NuGet não é publicado, referencie o projeto analyzer diretamente:

```xml
<ItemGroup>
  <ProjectReference Include="..\Swa.Analyzers.Core\Swa.Analyzers.Core.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Configuração via .editorconfig
Exemplo para `ARCH001`:

```ini
[*.cs]
dotnet_diagnostic.ARCH001.only_test_projects = true
dotnet_diagnostic.ARCH001.blocked_namespaces = Moq,FakeItEasy,Rhino.Mocks
dotnet_diagnostic.ARCH001.blocked_assemblies = Moq,FakeItEasy,Rhino.Mocks
dotnet_diagnostic.ARCH001.test_project_patterns = test;tests;spec
```

## Executando testes
```bash
dotnet test Swa.Analyzers.Tests/Swa.Analyzers.Tests.csproj
```
