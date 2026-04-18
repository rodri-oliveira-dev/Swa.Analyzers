# ARCH006: Alertar uso de Excluding em BeEquivalentTo

## Objective
Alertar quando um teste usa **exclusões** (`Excluding*`) em comparações de equivalência do FluentAssertions, especialmente via `BeEquivalentTo(...)`, porque isso pode reduzir a precisão do teste e esconder regressões.

## Motivation
`BeEquivalentTo` é uma asserção poderosa para comparar objetos complexos, mas quando o teste começa a excluir partes do grafo (`Excluding(...)`, `ExcludingMissingMembers()`, etc.) ele tende a se tornar mais permissivo do que o necessário.

Esse tipo de exclusão pode:

- mascarar mudanças acidentais no comportamento (o teste continua passando mesmo com divergências relevantes)
- reduzir a clareza do que realmente importa na comparação
- incentivar o uso de “atalhos” em vez de modelar o esperado de forma explícita

## Non-compliant code examples

### Excluding em opções do BeEquivalentTo

```csharp
actual.Should().BeEquivalentTo(expected, options =>
    options.Excluding(x => true));
```

### ExcludingMissingMembers em opções do BeEquivalentTo

```csharp
actual.Should().BeEquivalentTo(expected, options =>
    options.ExcludingMissingMembers());
```

## Compliant code examples

### Comparação direta sem exclusões

```csharp
actual.Should().BeEquivalentTo(expected);
```

### Ajustar o esperado em vez de excluir

```csharp
var expected = new Dto
{
    Id = actual.Id, // quando o Id é gerado pelo sistema e faz parte do contrato do teste
    Name = "Foo",
};

actual.Should().BeEquivalentTo(expected);
```

## Configuration
Esta regra não expõe opções customizadas via `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH006.severity = info
```

## Known limitations
- A regra é intencionalmente limitada a **contextos de teste**, usando a mesma heurística de outras regras do pacote:
  - só roda quando a compilação referencia atributos conhecidos de frameworks de teste (xUnit/NUnit/MSTest)
  - só reporta quando a invocação está em um método de teste ou dentro de um tipo que contenha pelo menos um método de teste
- A regra é intencionalmente limitada a `BeEquivalentTo(...)` do **FluentAssertions** (validação semântica por namespace), para evitar falsos positivos em APIs com o mesmo nome.
- A regra procura chamadas `Excluding*` **apenas dentro do delegate de opções** passado para `BeEquivalentTo`. Se um código montar opções fora do lambda e aplicá-las indiretamente, a regra pode não reportar.

## When not to use
Apesar do alerta, existem casos em que excluir pode ser aceitável. Exemplos típicos:

- Propriedades **não determinísticas** ou geradas pelo sistema (timestamps, GUIDs, campos de auditoria) quando:
  - elas não fazem parte do comportamento sob teste, e
  - existe outro teste ou asserção dedicada cobrindo o contrato dessas propriedades
- Comparações em cenários de migração/refactor onde a equivalência completa não é viável temporariamente (use com parcimônia e com intenção explícita)

Nesses casos, ainda é recomendado:

- documentar a razão da exclusão (comentário no teste)
- preferir exclusões **precisas e mínimas**
- adicionar asserções específicas para os pontos mais críticos do comportamento

## Expected impact
- Incentiva testes mais estritos e com maior poder de detecção de regressões.
- Reduz a tendência de “alargar” asserções de equivalência como forma de resolver falhas de teste.

## Notes about false positives, heuristics, or exceptions
### Identificação semântica
A regra identifica `BeEquivalentTo` e `Excluding*` usando informações semânticas (namespace do FluentAssertions e sub-namespace `FluentAssertions.Equivalency`).
Isso evita alertar sobre métodos com o mesmo nome em bibliotecas não relacionadas.

### No code fix
Esta regra não fornece code fix automático, porque remover/extrair exclusões exige entendimento do domínio e do objetivo do teste (não é uma transformação segura e determinística).
