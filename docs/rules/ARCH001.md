# ARCH001 - Permitir apenas NSubstitute como biblioteca de mock

## Objetivo
Garantir padronização do framework de mock em testes, bloqueando bibliotecas não aprovadas (ex.: Moq, FakeItEasy, Rhino Mocks) e incentivando o uso exclusivo de NSubstitute.

## Motivação
Padronização reduz custo de manutenção, facilita leitura de testes e evita dependências redundantes de bibliotecas com APIs divergentes.

## Código inválido
```csharp
using Moq;

var customerRepo = new Mock<ICustomerRepository>();
```

## Código válido
```csharp
using NSubstitute;

var customerRepo = Substitute.For<ICustomerRepository>();
```

## Como configurar
```ini
# Analisa apenas projetos de teste (padrão: true)
dotnet_diagnostic.ARCH001.only_test_projects = true

# Sobrescreve bibliotecas/namespace bloqueados (separador: , ; ou |)
dotnet_diagnostic.ARCH001.blocked_namespaces = Moq,FakeItEasy,Rhino.Mocks

# Sobrescreve assemblies/pacotes bloqueados (fallback: mesmo valor de blocked_namespaces)
dotnet_diagnostic.ARCH001.blocked_assemblies = Moq,FakeItEasy,Rhino.Mocks

# Define padrões para detectar assembly de teste
# (padrão: test;tests;spec)
dotnet_diagnostic.ARCH001.test_project_patterns = test;tests;spec
```

## Limitações conhecidas
- A detecção de "projeto de teste" usa heurística por nome de assembly e referências conhecidas de frameworks de teste; em cenários incomuns, configure `only_test_projects = false`.
- Wrappers internos que encapsulem bibliotecas bloqueadas podem exigir ajuste de configuração.

## Quando não usar
- Equipes que explicitamente aceitam múltiplos frameworks de mock.

## Impacto esperado
- Maior consistência entre testes.
- Menor acoplamento com múltiplas bibliotecas de mocking.

## Observações sobre falsos positivos/heurísticas
- O analyzer não analisa strings, comentários ou documentação.
- O diagnóstico é emitido para uso semântico/sintático real (using, criação de objeto, invocações e referências de membros).
