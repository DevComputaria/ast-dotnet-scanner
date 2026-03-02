# dotnet-ast

Um scanner de soluções .NET baseado em Roslyn para extração de AST e análise de sistemas, focado na criação de **Knowledge Graphs** e **Code Property Graphs (CPG)**.

## 🚀 Objetivo
O `dotnet-ast` é uma ferramenta de linha de comando que ajuda desenvolvedores e arquitetos a entenderem a estrutura de grandes sistemas .NET, permitindo a extração de metadados ricos de código para formatos estruturados como JSON ou para ingestão direta em grafos (**Neo4j**).

## 🛠️ Funcionalidades Atuais
- **Scan de Alta Performance**: Suporte nativo a .NET 10 e arquivos `.sln` / `.csproj`.
- **Metadata V2 (CPG-Ready)**: Extração de Symbol IDs estáveis, Fully Qualified Names, Modificadores, Atributos e Herança.
- **Grafo de Chamadas (Call Graph)**: Identificação automática de quem chama quem (invocações de métodos).
- **Ingestão Direta no Neo4j**: Comandos integrados para inicializar schemas (constraints/índices) e ingerir dados via batch processing otimizado com `UNWIND`.

### 🧬 Modelo de Grafo (Neo4j)
O `Neo4jIngestor` mapeia o código-fonte para um grafo de propriedades com a seguinte estrutura:

- **Nós**: `Project`, `Namespace`, `Class`, `Interface`, `Method`, `Property`.
- **Relacionamentos**:
  - `(Project)-[:HAS_NAMESPACE]->(Namespace)`
  - `(Namespace)-[:DECLARES]->(Class|Interface)`
  - `(Class)-[:EXTENDS]->(Class)`
  - `(Class|Interface)-[:IMPLEMENTS]->(Interface)`
  - `(Parent)-[:DECLARES_METHOD]->(Method)`
  - `(Parent)-[:DECLARES_PROPERTY]->(Property)`
  - `(Method)-[:CALLS]->(Method)` (**Grafo de Chamadas**)
- **Funcionalidades Técnicas**:
  - **Schema Automático**: Criação de constraints de unicidade (`IS UNIQUE`) para garantir integridade.
  - **Sanitização de IDs**: Tratamento automático de Symbol IDs gigantes via SHA256 para compatibilidade com índices do Neo4j.
  - **Preservação de Metadados**: Ingestão de atributos, modificadores, acessibilidade, caminhos de arquivo e localização exata (linha/coluna).
- **Exportação Flexível**: 
  - Saída em Tabela formatada para humanos.
  - JSON rico com `UnsafeRelaxedJsonEscaping` para legibilidade de tipos genéricos (`Task<T>`).
- **Filtros Avançados**: Filtros por projeto, namespace, tipos de símbolos (Class, Method, Property) e acessibilidade (Public Only).

## 🏗️ Estrutura do Projeto
- `src/AstSolutionScanner.Core`: Biblioteca central com a lógica de análise Roslyn e integração Neo4j.
- `src/AstSolutionScanner.Cli`: Interface de linha de comando robusta baseada em `System.CommandLine`.

## 💻 Tech Stack
- **Runtime**: .NET 10
- **Analysis**: Roslyn (Microsoft.CodeAnalysis)
- **Database**: Neo4j (Neo4j.Driver)
- **CLI**: System.CommandLine

## 📖 Como Usar

### 1. Scan Básico (Terminal)
```bash
dotnet run --project src/AstSolutionScanner.Cli -- scan "Caminho/Para/SeuProjeto.sln"
```

### 2. Exportar JSON para Análise
```bash
dotnet run --project src/AstSolutionScanner.Cli -- scan "Caminho/Para/SeuProjeto.csproj" --format json --output result.json
```

### 3. Integração Neo4j
Primeiro, inicialize o schema (constraints de unicidade):
```bash
dotnet run --project src/AstSolutionScanner.Cli -- neo4j init-schema --uri "neo4j://localhost:7687" --user neo4j --password "sua_senha"
```

Depois, realize a ingestão direta do código para o grafo:
```bash
dotnet run --project src/AstSolutionScanner.Cli -- neo4j ingest "Caminho/Para/SeuProjeto.sln" --password "sua_senha"
```

## 📈 Roadmap

### v0.1 - v0.2 (Concluído ✅)
- [x] Configuração do workspace e MSBuildLocator (.NET 10).
- [x] Comando `scan` funcional.
- [x] Exportação para JSON V1.

### v0.3 - v0.4 (Concluído ✅)
- [x] Exportação para JSON V2 (Enhanced Metadata).
- [x] Identificação de Invocação de Métodos (Call Graph).
- [x] Ingestão em Batch para Neo4j com UNWIND.
- [x] Correção de escape HTML em JSON para tipos genéricos.

### v0.5+ (Poder e Integração - Próximos Passos)
- [ ] Mapeamento de Dependências Externas (NuGet/SaaSBOM).
- [ ] Análise de Fluxo de Dados (Data Flow).
- [ ] Exportação para Mermaid.js diretamente do CLI.

---
*Este projeto é parte de uma iniciativa para melhorar a observabilidade e segurança em ecossistemas .NET.*
