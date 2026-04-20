# ARCH008: Proibir composição manual de caminhos de arquivo

## Objective
Detectar **composição manual de caminhos** (path) via **concatenação** (`+`) ou **string interpolada** quando o valor é passado diretamente para **APIs de filesystem**, e sugerir o uso de **`System.IO.Path.Combine`** ou **`System.IO.Path.Join`**.

## Motivation
Montar caminhos “na mão” com `"/"`, `"\\"` ou interpolação costuma ser frágil:

- pode quebrar em diferentes sistemas operacionais (separadores diferentes)
- pode gerar caminhos inválidos com barras duplicadas, ausência de separador, etc.
- torna o código menos explícito sobre a intenção (compor um path vs. apenas formatar texto)

`Path.Combine`/`Path.Join` existem justamente para tratar os detalhes de composição de forma consistente.

## Non-compliant code examples

### Concatenação manual em `File.*`

```csharp
using System.IO;

var content = File.ReadAllText(dir + "/" + fileName);
```

### Interpolação manual em `Directory.*`

```csharp
using System.IO;

Directory.CreateDirectory($"{root}/{folder}");
```

### Concatenação manual em `new FileInfo(...)`

```csharp
using System.IO;

var info = new FileInfo(dir + "\\" + fileName);
```

## Compliant code examples

### Usando `Path.Combine`

```csharp
using System.IO;

var content = File.ReadAllText(Path.Combine(dir, fileName));
```

### Usando `Path.Join`

```csharp
using System.IO;

Directory.CreateDirectory(Path.Join(root, folder));
```

## Configuration
Esta regra não expõe opções customizadas via `.editorconfig` na primeira versão.

A severidade pode ser configurada normalmente:

```ini
[*.cs]
dotnet_diagnostic.ARCH008.severity = info
```

## Known limitations
- A regra reporta apenas quando a composição manual ocorre **diretamente no argumento** passado para um sink conhecido.
  - Ex.: `File.ReadAllText(dir + "/" + name)` é reportado.
  - Ex.: `var p = dir + "/" + name; File.ReadAllText(p)` **não** é reportado.
- A regra identifica “argumentos de path” por **heurística baseada no nome do parâmetro** (`path`, `fileName`, `sourceFileName`, etc.). Se um sink tiver nomes diferentes, o analyzer pode ficar silencioso.
- A regra não tenta provar que a string contém separadores; ela sinaliza o padrão de composição manual (concatenação/interpolação) apenas em sinks de filesystem.

## When not to use
- Se seu código precisa compor paths para **formatar ou exibir** (por exemplo, logs) e não para uma API de filesystem. Note que esta regra já evita esse caso por construção (ela só roda em sinks).
- Quando o path é obtido de uma fonte externa (config, environment variables) e o código apenas o repassa.

## Expected impact
- Melhora portabilidade (Windows/Linux/macOS) ao reduzir dependência de separadores hard-coded.
- Melhora legibilidade e intenção do código.
- Reduz bugs de paths malformados.

## Notes about false positives, heuristics, or exceptions

### Sinks cobertos
Na versão atual, a regra verifica composição manual de path em argumentos `string` (de parâmetros com nomes típicos de path) passados para:

- `System.IO.File` (métodos estáticos)
- `System.IO.Directory` (métodos estáticos)
- `System.IO.FileInfo` (construtor e métodos)
- `System.IO.DirectoryInfo` (construtor e métodos)
- `System.IO.FileStream` (construtor)
- `System.IO.StreamReader` (construtor)
- `System.IO.StreamWriter` (construtor)

### Heurística para reduzir ruído
- A regra **não** reporta interpolação “identidade” como `File.ReadAllText($"{path}")`.
- A regra **não** reporta concatenação/interpolação fora dos sinks listados.

### No code fix
Esta regra **não fornece code fix automático** porque alterar composição de string para `Path.Combine`/`Path.Join` pode mudar comportamento de forma não determinística:

- `Path.Combine` trata caminhos absolutos em segmentos posteriores de forma diferente (ele descarta segmentos anteriores)
- existem diferenças sutis entre `Combine` e `Join` dependendo de `null`/`""`
- pode existir intenção de preservar exatamente a string resultante (por exemplo, quando o código depende de um formato específico)
