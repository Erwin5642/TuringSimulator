# Project Context: The Fabric Turing Machine

## 1. Vision & Pedagogy
* **Core Purpose:** A pedagogical simulator designed for freshman computer science students to visualize and understand Turing Machines (TM).
* **Metaphor:** The simulation environment is a **Fabric**, representing the "tape" and "state" through textile manipulations.
* **Interaction:** Employs block-style programming for visual logic, lowering the barrier for entry while maintaining formal rigor.

## 2. Architectural Pillars

### A. Single Entry Point & Dependency Injection
* **General Game Controller:** The application strictly relies on a single entry point that acts as the master bootstrapper for the entire game lifecycle (`TuringBootstrap`).
* **Specialized Installers:** Dependency wiring, interface binding, and event subscriptions are strictly delegated to dedicated installer classes rather than scattered `Awake()` or `Start()` methods.
* **Model Installer:** Responsible for wiring the TM mathematical brain (simulation, tape, validation, buffered steps) and producing immutable step packets for the pipeline. **Bayesian student modeling (BKT) is authoritative on the Python server**; there is no separate BKT replica in the Unity model for the current MVP.
* **View Installer:** Responsible for wiring the machine/tape/halt views, level UI, and animation interpolation consumers. Full **Fabric** (textile metaphor) and **UI Toolkit** block editors are roadmap items unless already present in scene assets.
* **Controller Installer:** Responsible for input routing (XR / `PlayerInputCatcher`) and the playback / production pipeline orchestrator. **An MCP client is not part of the shipped MVP**—treat as future tooling integration or backlog unless explicitly added.

### B. Buffered Production Pipeline (Model-Sync-View)
* **Model (The Brain):** An isolated C# logic layer computes the Turing Machine's "Next State" without any knowledge of the Unity Engine visuals.
* **State Packets (The Data):** The Model produces immutable data packets containing the full delta of a step (e.g., Head position, Symbol change, State transition).
* **The Buffer:** A queue stores these packets, completely decoupling the logical execution speed from the visual animation speed.
* **View (The Fabric):** A consumer reads these packets and interpolates the data to provide smooth, synchronized visual transitions.

### C. Intelligent Tutoring System (ITS) & Python Integration
* **Remote Brain:** A Python-based server handles heavy computation, student modeling, and the embodied agent's intelligence.
* **Bayesian Knowledge Tracking (BKT):** The server maintains a probabilistic model of the student's mastery of specific CS concepts based on incoming "Observation Packets."
* **REST events:** Unity posts `/event`, `/ask`, `/hint` with `student_id`, `level_id`, and skill/program payloads. Use structured logging on both sides (`setup_logging` + optional `AGENT_LOG_PATH` on the server).
* **Live WebSocket (protocol v1):** `LiveTutorSocket` connects to `/ws/live` using **snake_case JSON** with **Newtonsoft.Json** (`LiveV1Json` / `LiveV1Wire` in `Assets/TuringSimulator/ITS/Protocol/`). Payload shapes mirror Pydantic models in `TuringBotAPI/protocol/live_v1.py`; the server validates with `validate_envelope` and `validate_inbound_payload`. Unity sends `live.handshake`, `live.run_lifecycle`, `live.level_snapshot`, `live.sim_step`, `live.sim_halt`. The server may emit downstream **`live.advisory_nudge`** (and other `live.advisory_*`) on a throttled basis—for example periodic nudges during stepped playback. Full Gemini-driven live advisory is optional. Hints and dialogue still route through REST `/hint` / `/ask` and `ILiveTutorInboundHandler` / `AgentDialogue` by default.
* **Level identity:** `LevelDefinition.levelId` must align with ITS/Python skill keys (fallback: `LevelID.MoveLeftRight`).
* **Bootstrap ITS wiring:** `TuringBootstrap` ensures `ITSClient`, `SkillTracker`, `AgentTTS`, `AgentDialogue`, and `LiveTutorSocket` exist on the persistent bootstrap root so REST and WebSocket tutoring can initialize without manual scene setup.

## 3. Engineering Standards (Cursor Rules)

### I. Interface-First Design (Mandatory)
* **Rule:** Prefer defining an interface before a concrete class for new **game systems** that will be mocked in tests (e.g., `ITuringModel`, simulation facades).
* **Reasoning:** Facilitates mocking for unit tests without the Python server or heavy views. **ITS networking** may use concrete singletons (`ITSClient`, `LiveTutorSocket`) in the MVP; introduce `IItsClient` when tests demand it.

### II. Data-Oriented Programming (DOP)
* **Rule:** Strictly separate **Data (What)** from **Logic (How)**.
* **Structure:** Use `readonly struct` or `record` for packets (e.g., `TuringStatePacket`, `BKTUpdatePacket`). Implement stateless "Systems" that take data in and output new data.
* **Immutability:** Packets must not be modified once they enter the Buffered Production Pipeline.

### III. MVC Adaptation
* **Model:** Pure TM data structures; student mastery (BKT) lives on the **Python** service.
* **View:** Unity GameObjects, shaders, and UI elements (Fabric metaphor as visual theme when implemented).
* **Controller:** Orchestrates the flow between the Python server, the pipeline, and the UI.

## 4. Technical Stack
* **Engine:** Unity (2023.x+ recommended for modern Awaitable support).
* **Language:** C# 11+ (Unity) / Python 3.11+ (Server).
* **AI/Math:** Bayesian Knowledge Tracking (BKT).
* **Networking:** REST ITS uses the same snake_case Newtonsoft settings as `ItsRestJson` (`EventResponseDto` includes `updated_skills`). WebSocket `/ws/live` streams steps (schema `1.0.0` in `protocol/live_v1.py` / `LiveV1Constants.ProtocolVersion`).

## 5. Instructions for Cursor AI
* **Initialization:** When adding new systems or dependencies, ALWAYS register their interfaces in the appropriate Specialized Installer (Model, View, or Controller). Never use `Awake`/`Start` for dependency resolution.
* **Contextual Awareness:** When writing code for the "View," always check for the corresponding "Interface" and "Data Packet" in the "Model."
* **Simulation Integrity:** Never allow the View to modify the Model directly. All changes must pass as strictly defined data packets through the Production Pipeline.
* **Agent Logic:** When suggesting agent behaviors, ensure there is a corresponding data hook for the Python server to monitor that specific student interaction.