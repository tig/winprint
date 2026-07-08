# Mermaid, the grand tour

`demo.md` shows one diagram and calls it a day. This file is the stress test: every diagram
type WinPrint's built-in renderer handles, drawn with enough going on to catch regressions,
plus the types it does not handle yet so the code-block fallback gets exercised too. If a
section below prints as source instead of a picture, that is either the fallback doing its
job or a bug; know which before filing it.

## Flowchart, with subgraphs

The classic. Direction, shapes, edge labels, and two subgraphs, because nobody's build
pipeline fits in one box.

```mermaid
flowchart TB
    subgraph authoring[Authoring]
        MD[write markdown] --> FENCE[add a mermaid fence]
    end
    subgraph printing[Printing]
        PARSE{fence tagged mermaid?}
        PARSE -->|yes| RENDER[render in-process]
        PARSE -->|no| CODE[print as code]
        RENDER -->|unsupported type| CODE
    end
    FENCE --> PARSE
    RENDER --> PAGE[ink on paper]
    CODE --> PAGE
```

## Sequence, with alt and a note

Lifelines, solid and dashed arrows, an alt block, and a note. The full soap opera.

```mermaid
sequenceDiagram
    participant U as User
    participant W as WinPrint
    participant R as Renderer
    U->>W: print mermaid.md
    W->>R: render fence
    alt diagram type supported
        R-->>W: PNG
    else not yet
        R-->>W: null (falls back to code)
    end
    Note over W,R: no network involved
    W-->>U: pages
```

## State

Every printer I have ever owned:

```mermaid
stateDiagram-v2
    [*] --> Ready
    Ready --> Printing: job arrives
    Printing --> Ready: pages out
    Printing --> Jammed: always eventually
    Jammed --> Ready: percussive maintenance
    Jammed --> [*]: replaced in anger
```

## Class

The inheritance hierarchy nobody asked to see printed, printed:

```mermaid
classDiagram
    ContentTypeEngineBase <|-- TextCte
    TextCte <|-- MarkdownCte
    ContentTypeEngineBase <|-- HtmlCte
    ContentTypeEngineBase : +RenderAsync()
    ContentTypeEngineBase : +PaintPage()
    MarkdownCte : +RenderMermaidDiagrams bool
    MarkdownCte : +MermaidBackend string
```

## Entity-relationship

A database for a printing app, which is to say, over-engineering:

```mermaid
erDiagram
    USER ||--o{ PRINTJOB : submits
    PRINTJOB ||--|{ SHEET : produces
    SHEET ||--o{ PAGE : "lays out"
    USER {
        string name
        int patience
    }
```

## Git graph

Every release, ever:

```mermaid
gitGraph
    commit
    commit
    branch fix-the-fix
    commit
    checkout main
    merge fix-the-fix
    commit tag: "v3.x.x"
```

## Mindmap

```mermaid
mindmap
  root((WinPrint))
    Source code
      Syntax highlighting
      Line numbers
    Markdown
      Images
      Mermaid
    Output
      Paper
      PDF
```

## Timeline

```mermaid
timeline
    title The long road to printing a diagram
    1998 : WinSpit ships
    2020 : WinPrint 2.0
    2026 : Markdown renders
         : Mermaid renders too
```

## Quadrant

```mermaid
quadrantChart
    title Features by effort and glory
    x-axis Low effort --> High effort
    y-axis Low glory --> High glory
    quadrant-1 Do these
    quadrant-2 Marketing
    quadrant-3 Quietly skip
    quadrant-4 Labors of love
    Syntax highlighting: [0.7, 0.5]
    Mermaid diagrams: [0.8, 0.9]
    Footnotes: [0.2, 0.1]
```

## Pie

Put `pie` on its own line; the renderer is particular about its headers.

```mermaid
pie
    title Where the ink goes
    "Diagrams" : 30
    "Code blocks" : 45
    "Regret" : 25
```

## Not yet: gantt

The built-in renderer does not do this one yet, so it prints as a code block below, exactly
as documented. Point `mermaidBackend` at `service` if you need it as a picture today.

```mermaid
gantt
    title Shipping this file
    section Render
    Spike the renderer :done, a1, 2026-07-07, 1d
    Print this page    :active, a2, after a1, 1d
```
