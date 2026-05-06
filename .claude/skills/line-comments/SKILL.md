---
name: line-comments
description: Apply Nathan's line-comment style when writing or editing comments in C-family source files (.c, .cpp, .cs). Covers what to comment, how to phrase it, and the column-120 rewrap rule. Auto-applies to .h/.c/.hpp/.cpp/.cs/.py files. For other extensions (e.g., .ts), confirm with the user before applying. Skip .xaml entirely.
---

# Line-comment style

Use this skill whenever you write or edit `//` line comments in source files. The rules below apply project-wide unless the user overrides them in a given task.

## Scope

* **Auto-apply** to user-authored `.c`, `.cpp`, `.cs`, `.h`, `.hpp`, `.py` files.
* **Ask first** before applying to any other extension (e.g., `.ts`, `.js`, `.go`, `.rs`). Confirm with the user that the same conventions should govern that file type.
* **Excluded** entirely: `.xaml` files.
* Also skip pure boilerplate: `*.xaml.cs` files that only contain `InitializeComponent()`, `AssemblyInfo.cs`, `*.Designer.cs`, generated `Resources.cs`, empty `partial class` shells. Vendored third-party code (e.g., the Xceed forks in this repo) is out of scope.

## What to comment

* Comment every function and property unless the comment would be trivial.
  * Functions and properties: 1–3 lines (more if extra detail genuinely helps the reader).
  * Use plain `//` comments above the member — **not** XML doc (`///`) comments.
  * Write naturally — full sentences, conversational tone. Don't pack jargon to save a line; readability beats density.
* A file-top overview comment is welcomed when the file isn't self-evident. Use it to explain the file's role, the moving parts inside, and any cross-file context a future reader would need to follow the code. Skip it for trivial files (single-class data holders, plain DTOs, etc.).
* Inside long functions with distinct sub-sections, label each sub-section with a short `//` header above the block (e.g. `// Load from disc` above the loading code, then `// Parse out to csv` above the parsing code). Expand to a fuller comment when a sub-section's purpose isn't obvious from the code alone.

## What to skip

* Skip trivial comments. A comment that just restates the identifier (`// Gets or sets the name.` on `public string Name { get; set; }`) is noise — leave it off.
* The bar: if removing the comment wouldn't make a reader pause, don't write it.

## How to phrase it

* Aim for insight, not description. Explain *why* the member exists, what it's used for in context, what invariants/edge cases callers must know — not *what* the code literally does.
* Follow callers and usage when the purpose isn't obvious from the signature.
* When intent is unclear, ask before writing the comment rather than guessing.
* Replace existing comments that are weak or generic; leave good ones alone.

## Wrapping rule (column 120)

Wrap dedicated `//` comment lines at column 120 (matches the VS2022 **Rewrap** plugin with `wrappingColumn = 120` and `wrapWholeComment = true`).

* The wrap column is counted from column 1 — it includes the leading indent and the `// `  prefix. Total line width stays ≤ 120.
* **Whole-comment rewrap**: a contiguous block of `//` lines at the same indent is treated as one comment — join its text, then re-wrap with greedy word fill (fill each line as full as it goes, no balancing or backtracking).
* **Never split a word.** If the next word would push the line past 120 it moves to the next line whole. Hyphenated words (`million-point`, `auto-pan`) are atoms — do not break at the hyphen.
* **Respect intentional paragraph breaks** when re-wrapping a block:
  * *Explicit* — a bare `//` line between two text paragraphs is preserved as a separator.
  * *Implicit* — sequential `//` lines with no separator but an obvious topic shift (e.g., a short standalone summary sentence on a different subject from the paragraph above) → keep on separate lines. Do **not** insert a bare `//` to make the split explicit; preserve the structure as-is.
* **Trailing inline comments** (`code; // explanation`) are not rewrapped — only dedicated `//`-only comment lines.

## Quick checklist before finalizing edits

1. Every non-trivial function/property has a 1–3 line `//` comment above it.
2. No `///` XML doc comments were added.
3. No comment merely restates the identifier.
4. Comments explain *why* and *when*, not *what*.
5. All dedicated `//` blocks are wrapped to ≤ 120 columns using greedy word fill, no broken words, paragraph breaks preserved.
6. Trailing `code; //` comments left alone.
7. File extension is `.c`, `.cpp`, `.cs`, `.h`, `.hpp`, or `.py` — or the user has confirmed the skill applies to a different extension.
