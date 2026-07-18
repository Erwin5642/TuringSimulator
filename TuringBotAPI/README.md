# Turing Machine ITS — FastAPI Server

## Setup

Use Python **3.11+** and a local virtual environment (no Nix / direnv required).

```bash
# From the repository root
cd TuringBotAPI

python3 -m venv .venv
source .venv/bin/activate        # Windows: .venv\Scripts\activate

pip install -U pip
pip install -r requirements-dev.txt

pytest tests/ -q

cp .env.example .env
# Edit .env: set GEMINI_API_KEY (https://aistudio.google.com/app/apikey)

uvicorn main:app --reload --port 8000
```

The interactive API docs are at **http://localhost:8000/docs**

The server can start without `GEMINI_API_KEY`. In that case `/health` reports
`"tutor_provider": "fallback"` and `/ask`, `/hint`, and reactive event comments
use deterministic factory-themed responses so the Unity scene remains testable.

---

## Endpoints

### `POST /event`
Unity sends this every time the student does something meaningful.

```json
{
  "student_id": "student_42",
  "level_id": "AppendScrew",
  "event_type": "program_run",
  "correct": false,
  "skill_ids": ["S4.1", "S4.5"],
  "details": { "error": "false_port_unconnected" }
}
```

**Response:**
```json
{
  "updated_skills": { "S4.1": 0.1823, "S4.5": 0.1102 },
  "comment": "Hmm, looks like one of those ports is still dangling, trainee!"
}
```

---

### `POST /ask`
Student asks the agent a free-form question.

```json
{
  "student_id": "student_42",
  "level_id": "AppendScrew",
  "question": "Why do I need to wire both ports?"
}
```

**Response:**
```json
{
  "reply": "Great question! Think of it like a railroad switch..."
}
```

---

### `POST /hint`
Student clicks the hint button. Escalates automatically.

```json
{
  "student_id": "student_42",
  "level_id": "AppendScrew",
  "skill_id": "S4.5"
}
```
Leave `skill_id` as `null` and the server will pick the weakest skill automatically.

**Response:**
```json
{
  "reply": "Have you thought about what happens after the robot checks a cell and doesn't find blank?",
  "skill_id": "S4.5",
  "hint_level": 1
}
```

---

### `GET /state/{student_id}`
Debug endpoint — see the student's full BKT knowledge state.

```
GET /state/student_42
```

---

## Unity Integration (C# snippet)

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ITSClient : MonoBehaviour
{
    private const string BASE_URL = "http://localhost:8000";
    private string studentId = "student_01"; // set from login/session

    // Call after every meaningful player action
    public IEnumerator SendEvent(
        string levelId, string eventType, bool correct, string[] skillIds)
    {
        var body = new EventRequest {
            student_id = studentId,
            level_id   = levelId,
            event_type = eventType,
            correct    = correct,
            skill_ids  = skillIds,
        };
        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest(BASE_URL + "/event", "POST");
        req.uploadHandler   = new UploadHandlerRaw(
            System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<EventResponse>(
                req.downloadHandler.text);
            if (!string.IsNullOrEmpty(resp.comment))
                AgentDialogue.Say(resp.comment);   // play in-game
        }
    }

    // Call when player clicks the hint button
    public IEnumerator RequestHint(string levelId, string skillId = null)
    {
        var body = new HintRequest {
            student_id = studentId,
            level_id   = levelId,
            skill_id   = skillId,
        };
        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest(BASE_URL + "/hint", "POST");
        req.uploadHandler   = new UploadHandlerRaw(
            System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<HintResponse>(
                req.downloadHandler.text);
            AgentDialogue.Say(resp.reply);
        }
    }
}
```

## Sequential players

Use `POST /session/new` at the beginning of every player run. The returned
`student_id` is the BKT isolation key. Do not clear `student_state.json` between
players; a new player gets a fresh empty state while previous state remains
available for a future resume flow.

The Unity live channel must handshake after session allocation. The server
rejects telemetry whose `student_id` or `session_id` does not match the active
WebSocket handshake.

---

## File structure

```
TuringBotAPI/
├── main.py              ← FastAPI app + REST/WebSocket contracts
├── orchestrator.py      ← provider-backed prompt assembly
├── tutor_provider.py    ← Gemini/fallback provider boundary
├── student_model.py     ← BKT state + persistence
├── requirements.txt
├── .env.example         ← copy to .env and add your key
├── student_state.json   ← auto-created on first run
└── domain/
    ├── concepts.py      ← concept map + BKT parameters
    └── hints.py         ← hint trees (27 trees, 9 levels)
```

## Skill IDs Unity should send

| Skill | When to send it |
|-------|----------------|
| S1.1  | Player places a wire |
| S1.2  | Player connects a port |
| S1.3  | Player positions head correctly |
| S1.4  | Player uses blank to detect tape end |
| S2.1  | Player identifies a symbol via condition |
| S2.2  | Player places and configures write block |
| S2.3  | Player uses write as a marker/memory |
| S3.1  | Player places a move block |
| S3.2  | Player chains move before write/condition |
| S4.1  | Player places and configures condition block |
| S4.2  | Player wires both condition output ports |
| S4.3  | Player builds circuit with all five blocks |
| S4.4  | Player designs a multi-phase program |
| S4.5  | Player creates a feedback loop |
| S5.1  | Player reaches any terminal block |
| S5.2a | Player reaches accept block |
| S5.2b | Player correctly chooses accept vs reject |
| S5.3  | Player accepts/rejects based on language membership |

Set `correct: true` when the action moves toward the solution,
`correct: false` when it is a mistake or the program run fails.