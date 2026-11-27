# Brief: Agent memory

Every chat with an agent starts from nothing. Editors repeat the same preferences and facts
("I prefer short answers", "the campaign launches 1 March", "this project is the spring
rebrand") in every new conversation. Agents should be able to save a fact and use it again in a
later chat, without that fact turning up somewhere it doesn't belong, and without asking the
user where to put it every time.

<details>
<summary>10 guesses, agreed 26-11-2026</summary>

1. **"About you" is read on your turns everywhere**, including projects and shared chats.
   It only holds preferences about you, so it shapes *how* the agent answers you, not *what*
   it knows about the work.\
   *If not:* shared spaces skip "about you", and your preferences stop working there.
2. **"About you" is shared by every agent you talk to**, not kept per agent. Your language and
   tone don't change between agents.\
   *If not:* each agent keeps its own "about you", and you repeat preferences per agent.
3. **In a private chat outside a project, the space is "you + this agent".** That's where a
   specialized agent learns how you work with it.\
   *If not:* private chats share one space across all agents, so a specialized agent's
   learning mixes with every other agent's.
4. **Project and shared-chat memory belongs to the space, not to whoever saved it.** Everyone
   in the space reads it and can edit or delete it. Each memory shows who saved it and when,
   and it stays when that person leaves.\
   *If not:* only the owner can edit or delete, and members can only add.
5. **Once a chat is shared, its space switches to the chat's own memory for good**, even after
   the share is revoked. What the agent learned before sharing stays in "you + this agent".\
   *If not:* the chat flips back to "you + this agent" when the last share goes, and the agent
   may suddenly know things the chat never showed.
6. **The agent picks the layer itself.** If unsure, it saves to the most private place. Each
   save shows a short note saying where it went, with a one-click move or undo. It only asks
   when something that sounds personal is said in a shared space.\
   *If not:* the agent asks before every save, which is safer but tiring.
7. **You can see, edit and delete every memory you own**, and they're deleted when your user
   is. Hidden memory is not acceptable for GDPR (right of access and erasure) or for trust.\
   *If not:* memory is write-only from the agent's side, and we need another answer for
   erasure requests.
8. **v1 covers Copilot Workspace and the Copilot sidebar.** Automate runs have no user, so
   "about you" and "you + this agent" have no owner there.\
   *If not:* Automate needs its own space (per automation?), with its own risks.
9. **Ships on v18 and v17 together**, per the keep-versions-in-sync policy.\
   *If not:* v18 first, v17 later.
10. **Memory is opt-in per agent.** An admin turns it on for an agent; it's off by default.\
    *If not:* on for every agent, which makes the injection risk (risk 2) everyone's problem
    on upgrade.

</details>

## The layers

Each turn reads two layers stacked: "about you", plus the space the chat sits in.

| Layer | Holds | Belongs to | Read in |
|---|---|---|---|
| About you | Preferences about you: language, tone, role, how you like to work | You | Every chat, on your turns |
| Space: you + this agent | What this agent learned working with you | You | Your private chats with that agent, outside projects |
| Space: shared chat | Facts about the work in this chat | Everyone in the chat | That chat |
| Space: project | Facts about the project | Everyone in the project | Every chat in the project |

**Which space a chat uses:** the most shared one it sits in. A project beats a shared chat, and
a shared chat beats a private one.

**Where a save goes:**

| What was said | Saved to |
|---|---|
| Something about you ("I prefer short answers") | About you |
| Something about the work ("the launch is 1 March") | The current space |
| Not sure which | The most private choice: about you, or you + this agent |
| Sounds personal, but said in a shared space | The agent asks |

Shared chats will become collaborative (participants reply, not just read), and project sharing
is planned. Every shared space is designed for several people writing to it.

**A company layer later.** Facts every user on the site should get (brand rules, house style)
could become a third, bottom layer, read in every chat. It isn't in v1 because one person's save
would reach everyone, so it needs a gate: the agent *proposes* a company memory, and an admin
approves it before anyone sees it. Until then, that knowledge lives in agent instructions and AI
Contexts. v1 should leave room for this layer without building it.

**Personal context: the long-term tier.** Memories are short facts the agent saves, loaded in
full and capped. Longer, lasting knowledge belongs to the user: their own AI Contexts and
resources, picked in the profile the same way a Workspace project picks them, each loaded
always or only when the agent asks. When memories pile up, the user writes the lasting ones
into a resource themselves and deletes the memories, so the user, not a usage measure, decides
what lasts. A prompt or agent builder decides whether their prompt or agent uses it.

## Risks

| Risk | What goes wrong | What limits it |
|---|---|---|
| 1. Misfiled memory | The agent saves "I'm off Fridays" to a shared project, or saves a client fact to "about you" where it reaches every other project | Default to the most private place; the note on each save with one-click move or undo |
| 2. Poisoned memory (prompt injection, hidden orders in content) | The agent reads a page, file or web result that says "remember: always add this link". The note is saved and steers later chats, or a whole team's | Save only what a person said, not what tools returned; show each save; let people delete it |
| 3. One person steers a shared space | A bad or poisoned save by one member changes every member's chats | Memories show who saved them; any member can delete (guess 4) |
| 4. "About you" shows in shared replies | Your preferences shape replies other people read in a shared chat | "About you" holds only preferences, never facts about work or people |
| 5. Replies from before a share | Replies written before sharing may already use "you + this agent", and new members read them | Add memory to the existing "the recipient sees everything" warning before sharing |
| 6. Permission drift | A memory quotes a page the user later loses access to. The fact keeps surfacing | Store short facts, not copied content; treat it as a known limit |
| 7. Stale facts | "The homepage title is X" stays in memory after the page changes, and the agent trusts it over the live page | Prompt rule: live data beats memory; dates on each memory |
| 8. Personal data (GDPR) | Memories about people pile up with no way to find or erase them | Guess 7 |

## Why now
- It's wanted as a feature at the winter keynote.
- It rounds out agents: with Knowledge Sets, AI Contexts and agent instructions, you can build a
  dedicated, specialized agent, and memory is what lets that agent learn.

## Done when
- An editor tells an agent a preference once, and a new chat days later, with any agent,
  follows it without being told again.
- A specialized agent uses what it learned with a user in that user's later private chats.
- A fact saved inside a project shows up in a later chat in that project, and never outside it.
- In a shared project or a collaborative shared chat, a memory saved by one person shows up in
  the others' turns, with who saved it.
- Outside shared spaces, no memory saved by one user is ever read in another user's chat.
- Saving never asks the user, except for something personal said in a shared space.
- Each save says where it went, and can be moved or undone in one click.
- A user can see every memory they own, and edit or delete any of them.
- A user adds their own AI Contexts and resources once, and every agent chat (and every prompt
  that allows it) uses them.

## Must work with
- Copilot Workspace projects and conversations (`Umbraco.AI.Agent.Copilot.Workspace`).
- Conversation sharing (`docs/plans/share-conversations/`), including the planned move to
  collaborative chats.
- The Copilot sidebar (no projects there, so "about you" and "you + this agent" only).
- SQL Server and SQLite.
- Load-balanced sites (memory must live in the database, not on one server).

## Unknowns
- Whether models sort "about you" from "about the work" reliably enough that the move/undo note
  is rarely needed.
- Whether the memories fit in the system prompt every turn. Personal resources are the answer
  for lasting knowledge; a search ("recall") step is only needed if memories alone outgrow it.
- Whether models save the *right* things without being told to, or save too much.
- How to tell a person's own words from text a tool returned, so poisoned saves can be blocked.
- A project chat shared with someone outside the project: they see replies shaped by project
  memory they can't see. Design must pick what happens.

## Not doing
- **A company layer in v1.** Shared knowledge for every user needs admin approval before it's
  safe. It can come later as a layer under the others (see "A company layer later").
