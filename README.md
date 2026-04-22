# Swa.Analyzers

Analyzers Roslyn reutilizáveis para .NET, focados em convenções de arquitetura, confiabilidade e qualidade de testes.

## Projetos

- `src/Swa.Analyzers.Core`: implementação dos analyzers (`DiagnosticAnalyzer`) e metadados de release (`AnalyzerReleases.Unshipped.md`).
- `tests/Swa.Analyzers.Tests`: testes automatizados dos analyzers (Microsoft.CodeAnalysis.Testing + xUnit).
- `src/Swa.Analyzers.SampleApp`: app de exemplo para validação manual; contém casos **válidos** e **inválidos** por regra e referencia `Swa.Analyzers.Core` como analyzer.

Documentação detalhada de cada regra: `docs/rules/` (um arquivo por regra). Os diagnósticos também apontam para esses arquivos via *help link*.

## Regras existentes

| ID      | Título (resumo)                          | Categoria   | Severidade padrão | Doc                     |
| ------- | ---------------------------------------- | ----------- | ----------------- | ----------------------- |
| ARCH001 | Avoid async void outside event handlers  | Reliability | Warning           | `docs/rules/ARCH001.md` |
| ARCH002 | Avoid Task.ContinueWith                  | Reliability | Warning           | `docs/rules/ARCH002.md` |
| ARCH003 | Prohibit NotBeNull() in tests            | TestQuality | Info              | `docs/rules/ARCH003.md` |
| ARCH004 | Enforce _sut naming in unit tests        | TestQuality | Info              | `docs/rules/ARCH004.md` |
| ARCH005 | Restrict usage of NSubstitute Arg.Any()  | TestQuality | Info              | `docs/rules/ARCH005.md` |
| ARCH006 | Warn on exclusions in BeEquivalentTo()   | TestQuality | Info              | `docs/rules/ARCH006.md` |
| ARCH007 | Detect string concatenation inside loops | Performance | Info              | `docs/rules/ARCH007.md` |
| ARCH008 | Prohibit manual path composition         | Reliability | Info              | `docs/rules/ARCH008.md` |
| ARCH009 | Prohibit sync over async blocking calls  | Reliability | Warning           | `docs/rules/ARCH009.md` |

## Como configurar

Configure severidade via `.editorconfig` normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH001.severity = warning
dotnet_diagnostic.ARCH008.severity = info
```

## Como validar

- **Automatizado**: `dotnet test`
- **Manual**: veja `src/Swa.Analyzers.SampleApp/README.md` (exemplos por regra e build com diagnósticos)
