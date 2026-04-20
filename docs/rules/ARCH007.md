# ARCH007: Detectar concatenação de string em loop

## Objective
Detectar **concatenação repetida de `string` dentro de laços** (`for`, `foreach`, `while`, `do/while`) e sugerir o uso de **`StringBuilder`** (ou estratégia similar de buffer) como alternativa mais adequada.

## Motivation
Concatenação incremental de `string` em loops tende a causar muitas alocações porque `string` é imutável. Em cenários com muitas iterações, isso pode degradar performance (inclusive com crescimento quadrático em alguns casos), além de aumentar pressão no GC.

## Non-compliant code examples

### Usando `+=` dentro do loop

```csharp
var result = "";
for (var i = 0; i < items.Count; i++)
{
    result += items[i];
}
```

### Auto-referência: `s = s + ...`

```csharp
foreach (var item in items)
{
    result = result + item;
}
```

### Interpolação com auto-referência

```csharp
while (condition)
{
    result = $"{result}{value}";
}
```

## Compliant code examples

### Usando `StringBuilder`

```csharp
var sb = new System.Text.StringBuilder();
foreach (var item in items)
{
    sb.Append(item);
}

var result = sb.ToString();
```

## Configuration
Esta regra não expõe opções customizadas via `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH007.severity = info
```

## Known limitations
- A regra é baseada em **heurística** (ver seção abaixo) e reporta apenas padrões que indicam *fortemente* concatenação incremental.
- A regra não tenta estimar o número de iterações em tempo de compilação. Se um loop executa poucas vezes em prática, ainda pode ser reportado (exceto em casos com condição constante `false`).
- A regra não reporta concatenação em **linhas isoladas fora de loops**.
- A regra não reporta concatenação que acontece **dentro de lambdas/local functions declaradas no corpo do loop**, porque isso pode representar execução tardia (deferred execution) e aumentar falsos positivos. Isso pode gerar falsos negativos em cenários onde a lambda é executada imediatamente.

## When not to use
- Em loops com pouquíssimas iterações onde a micro-otimização não é relevante e você prefere simplicidade.
- Quando o objetivo é apenas montar uma string *por item* (não cumulativa), por exemplo para log/format de saída por iteração.

## Expected impact
- Reduz alocações e melhora performance em cenários comuns de construção incremental de string.
- Incentiva padrão consistente (StringBuilder) para concatenação em loops.

## Notes about false positives, heuristics, or exceptions

### Heurística implementada
A regra reporta quando encontra, **dentro do corpo de um loop**, uma atribuição a um alvo do tipo `string` (local ou campo) que representa concatenação incremental, nos seguintes padrões:

1. **Compound assignment**: `s += expr` (onde `s` é `string`)
2. **Atribuição com auto-referência**: `s = s + expr` (inclui variações como `s = s + a + b`)
3. **Interpolated string com auto-referência**: `s = $"{s}{expr}"`
4. **`string.Concat` com auto-referência**: `s = string.Concat(s, expr)`

### Redução de falso positivo
- A regra **não reporta** concatenação em variáveis locais do tipo `string` **declaradas dentro do corpo do loop**. Exemplo típico: construir uma string temporária por iteração.
- A regra **não reporta** quando o valor atribuído não faz referência ao próprio alvo (ex.: `s = x + y`).
- A regra **não reporta** loops com condição constante `false` (ex.: `while(false)`), porque o corpo nunca executa.
- A regra **não reporta** `do { ... } while(false)` porque, apesar de executar uma vez, é um caso tipicamente trivial.

### No code fix
Esta regra **não fornece code fix automático**, porque a transformação para `StringBuilder` não é segura e determinística em geral:
- exige decidir onde declarar o `StringBuilder`
- precisa considerar escopo/visibilidade (local vs campo)
- pode exigir alterações de tipo e return/uso posterior
- pode ter impacto semântico quando o código depende de `null` vs `""`, cultura/formatação, etc.
