# Project Context: The Fabric Turing Machine

## 1. Vision & Pedagogy
* **Core Purpose:** A pedagogical simulator designed for freshman computer science students to visualize and understand Turing Machines (TM).
* **Metaphor:** The simulation environment is a **Fabric**, representing the "tape" and "state" through textile manipulations.
* **Interaction:** Employs block-style programming for visual logic, lowering the barrier for entry while maintaining formal rigor.

## 2. Architectural Pillars

### A. Single Entry Point & Dependency Injection
* **General Game Controller:** The application strictly relies on a single entry point that acts as the master bootstrapper for the entire game lifecycle (`TuringBootstrap`).
* **Specialized Installers:** Dependency wiring, interface binding, and event subscriptions are strictly delegated to dedicated installer classes rather than scattered `Awake()` or `Start()` methods.
* **Model Installer:** Responsible for wiring the TM mathematical brain (simulation, tape, validation, buffered steps) and producing immutable step packets for the pipeline. There is no BKT replica in Unity.
* **View Installer:** Responsible for wiring the machine/tape/halt views, level UI, and animation interpolation consumers. Full **Fabric** (textile metaphor) and **UI Toolkit** block editors are roadmap items unless already present in scene assets.
* **Controller Installer:** Responsible for XR command routing (start/run/playback/next/menu) and the playback / production pipeline orchestrator. **An MCP client is not part of the shipped MVP**—treat as future tooling integration or backlog unless explicitly added.

### B. Buffered Production Pipeline (Model-Sync-View)
* **Model (The Brain):** An isolated C# logic layer computes the Turing Machine's "Next State" without any knowledge of the Unity Engine visuals.
* **State Packets (The Data):** The Model produces immutable data packets containing the full delta of a step (e.g., Head position, Symbol change, State transition).
* **The Buffer:** A queue stores these packets, completely decoupling the logical execution speed from the visual animation speed.
* **View (The Fabric):** A consumer reads these packets and interpolates the data to provide smooth, synchronized visual transitions.

### C. Intelligent Tutoring System (ITS) & Python Integration
* **Remote Brain:** A Python FastAPI server answers `/ask` with an agentic RAG loop over markdown docs in `TuringBotAPI/knowledge/`.
* **REST:** Unity posts `/session/new`, `/ask`, and `/health`. `/ask` body is `student_id`, `level_id`, `question` (snake_case Newtonsoft / `ItsRestJson`). Reply is `reply`; Unity does Wit TTS.
* **Retrieval:** Gemini may call `search_docs` up to three times. Common greetings skip retrieval. Offline fallback uses keyword search and a short player-facing pt-BR clipboard (not the raw corpus dump).
* **Level identity:** `LevelDefinition.levelId` must align with `TuringBotAPI/knowledge/goals/` and `LevelID`.
* **Bootstrap ITS wiring:** `TuringBootstrap` ensures `ITSClient`, `SkillTracker`, `AgentTTS`, and `AgentDialogue` exist so REST tutoring can initialize. The server no longer serves `/ws/live`, `/event`, or `/hint`.

## 3. Engineering Standards (Cursor Rules)

### I. Interface-First Design (Mandatory)
* **Rule:** Prefer defining an interface before a concrete class for new **game systems** that will be mocked in tests (e.g., `ITuringModel`, simulation facades).
* **Reasoning:** Facilitates mocking for unit tests without the Python server or heavy views. **ITS networking** may use concrete singletons (`ITSClient`, `LiveTutorSocket`) in the MVP; introduce `IItsClient` when tests demand it.

### II. Data-Oriented Programming (DOP)
* **Rule:** Strictly separate **Data (What)** from **Logic (How)**.
* **Structure:** Use `readonly struct` or `record` for packets (e.g., `TuringStatePacket`). Implement stateless "Systems" that take data in and output new data.
* **Immutability:** Packets must not be modified once they enter the Buffered Production Pipeline.

### III. MVC Adaptation
* **Model:** Pure TM data structures. Tutor answers live on the **Python** RAG service.
* **View:** Unity GameObjects, shaders, and UI elements (Fabric metaphor as visual theme when implemented).
* **Controller:** Orchestrates the flow between the Python server, the pipeline, and the UI.

## 4. Technical Stack
* **Engine:** Unity (2023.x+ recommended for modern Awaitable support).
* **Language:** C# 11+ (Unity) / Python 3.11+ (Server).
* **AI/Math:** Gemini embeddings + in-memory cosine search (keyword fallback).
* **Networking:** REST ITS uses snake_case Newtonsoft settings as `ItsRestJson`. Main line: `/session/new`, `/ask`, `/health`.

## 5. Instructions for Cursor AI
* **Initialization:** When adding new systems or dependencies, ALWAYS register their interfaces in the appropriate Specialized Installer (Model, View, or Controller). Never use `Awake`/`Start` for dependency resolution.
* **Contextual Awareness:** When writing code for the "View," always check for the corresponding "Interface" and "Data Packet" in the "Model."
* **Simulation Integrity:** Never allow the View to modify the Model directly. All changes must pass as strictly defined data packets through the Production Pipeline.
* **Agent Logic:** When suggesting tutor answers, ground them in `TuringBotAPI/knowledge/` rather than inventing controls or full circuits.