# Swa.Analyzers.SampleApp

App de console usado para **validação manual** e **demonstração** dos analyzers do repositório.

Este projeto referencia `Swa.Analyzers.Core` como **analyzer** (via `ProjectReference` com `OutputItemType="Analyzer"`). Por isso, ao compilar, os diagnósticos `ARCH*` aparecem como warnings/erros no output do build.

## Como usar

- Compile o projeto e observe os diagnósticos:

```bash
dotnet build src/Swa.Analyzers.SampleApp/Swa.Analyzers.SampleApp.csproj
```

- Ajustes de severidade específicos do SampleApp estão em `src/Swa.Analyzers.SampleApp/.editorconfig` (o objetivo é **não quebrar a compilação** mesmo com exemplos inválidos).

## Organização dos exemplos

Os exemplos ficam separados por regra, em pastas `Arch###/`:

- `*_Invalid.cs`: código intencionalmente não conforme (deve disparar o analyzer)
- `*_Valid.cs`: código conforme (não deve disparar o analyzer)

Algumas regras precisam de “contexto” de bibliotecas de teste (ex.: FluentAssertions, NSubstitute, xUnit). Para não depender de packages reais, o projeto inclui stubs mínimos em `Stubs/` apenas para permitir que o analyzer reconheça os símbolos.

## Documentação das regras

A explicação detalhada de cada regra (motivação, exemplos completos, limitações) está em `docs/rules/`.
