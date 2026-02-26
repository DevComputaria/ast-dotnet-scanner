# dotnet-ast

Um scanner de soluções .NET baseado em Roslyn para extração de AST e análise de sistemas.

## 🚀 Objetivo
O `dotnet-ast` é uma ferramenta de linha de comando que ajuda desenvolvedores e arquitetos a entenderem a estrutura de grandes sistemas .NET, permitindo a extração de metadados de código (métodos, classes, dependências) para formatos estruturados como JSON ou para integração com grafos (Neo4j).

## 🛠️ Funcionalidades Planejadas
- **Scan de Soluções**: Suporte a arquivos `.sln` e `.csproj`.
- **Extração de Metadados**: Lista de métodos, classes, namespaces e acessibilidade.
- **Localização Estrita**: Mapeamento de linha/coluna para cada símbolo.
- **Exportação Flexível**: Saída em texto puro para humanos ou JSON para ferramentas.
- **Filtros Avançados**: Busca por padrões de nome, namespaces específicos ou níveis de acesso.
- **Mapeamento de Grafo**: Identificação de quem chama quem (call graph).

## 🏗️ Estrutura do Projeto
- `src/AstSolutionScanner.Core`: Biblioteca central com a lógica de análise Roslyn.
- `src/AstSolutionScanner.Cli`: Interface de linha de comando baseada em `System.CommandLine`.

## 📈 Roadmap

### v0.1 - v0.2 (Fundação)
- [ ] Configuração do workspace e MSBuildLocator.
- [ ] Comando `scan` básico.
- [ ] Output para o terminal.

### v0.3 - v0.4 (Dados e DX)
- [ ] Exportação para JSON.
- [ ] Comando `info` para diagnóstico de ambiente.
- [ ] Logging refinado.

### v0.5+ (Poder e Integração)
- [ ] Filtros por Regex/Glob.
- [ ] Comando `graph` para mapeamento de chamadas.
- [ ] Exportação compatível com Neo4j.

## 📖 Como Rodar (Em breve)
```bash
dotnet run --project src/AstSolutionScanner.Cli scan my-solution.sln
```

---
*Este projeto é parte de uma iniciativa para melhorar a observabilidade em ecossistemas .NET.*
