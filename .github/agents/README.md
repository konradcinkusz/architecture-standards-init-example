# In-repo agent definitions

Agent configurations live next to the code they operate on and are reviewed like
code (REPO-BASELINE §6). Three rules hold for every file here:

- **The tool allowlist is a safety boundary**, not documentation. Both agents
  below are `['read', 'search']` by construction: an agent whose job is to report
  cannot edit, regardless of how a prompt is worded.
- **The description is the router.** Each one carries worked invocation examples,
  because a one-line description does not route.
- **Repo-relative paths only.** An absolute path (`C:\Repos\…`) breaks every other
  machine and CI. This has been a real finding in this estate.

| Agent | Does | Tools |
|---|---|---|
| [`architecture-compliance`](architecture-compliance.md) | Reviews a change against P1–P15 and the guides; reports findings ranked blocking / should fix / consider, each citing the rule | read, search |
| [`onboarding-guide`](onboarding-guide.md) | Answers run/configure/deploy questions from the files that actually exist, naming each source | read, search |

**Memory policy: none of these agents keeps persistent memory.** Both answer from
the tree on each run, which is the property that keeps them from confidently
repeating a fact that stopped being true. If one ever gains memory, it becomes
project-scoped, committed, and governed by an explicit save/don't-save policy in
this file.
