# Document C# Code

Add or upgrade XML documentation on the @-mentioned C# file(s), or on the file currently focused in the editor if none are mentioned.

## Goal

Produce clear, accurate `///` documentation that matches this project's existing style (see `NetworkManager.cs`, `SessionChat.cs`). Do not change behavior—warn if behavior does not match docs.

## What to document

1. **Types** — class/struct/enum purpose in a `<summary>`; use `<remarks>` for usage notes, invariants, or non-obvious side effects.
2. **Public / protected API** — every public and protected member gets a `<summary>`.
3. **Parameters & returns** — `<param>`, `<returns>` when the meaning is not obvious from the name alone.
4. **Booleans** — prefer `<see langword="true"/>` / `<see langword="false"/>`.
5. **Cross-refs** — use `<see cref="..."/>` for types, members, and related APIs in this project or Godot.
6. **Inline code** — use `<c>...</c>` for flag names, literals, and short identifiers.
7. **Private helpers** — document only when the intent is non-obvious; skip trivial getters/setters and one-line wrappers.

## Style rules

- Write in complete, concise sentences. Prefer "Returns whether…" / "Emitted when…" over vague fluff.
- Document *behavior and contracts*, not implementation narration.
- Do not restate the member name as the whole summary.
- Keep summaries to 1–3 sentences; put longer guidance in `<remarks>`.
- Match the file's existing indentation and brace style.
- Leave unrelated code alone. No drive-by refactors, renames, or formatting sweeps.
- Fix typos and outdated comments that the new docs replace.

## Workflow

1. Read the target file(s) and any direct call sites needed to describe real behavior.
2. Add or replace XML docs on the members in scope.
3. Reply with a short summary of what was documented (and anything left undocumented on purpose).
