import asyncio

import student_model
from main import (
    EventRequest,
    get_state,
    handle_event,
    health,
    new_session,
)


def test_health_reports_tutor_provider():
    response = asyncio.run(health())

    assert response["status"] == "ok"
    assert response["tutor_provider"] in {"fallback", "gemini"}


def test_sequential_sessions_isolate_event_evidence(tmp_path, monkeypatch):
    monkeypatch.setattr(
        student_model,
        "_STATE_PATH",
        tmp_path / "student_state.json",
    )
    student_model.STUDENT_MODEL._store.clear()

    player_a = asyncio.run(new_session()).student_id
    player_b = asyncio.run(new_session()).student_id

    asyncio.run(
        handle_event(
            EventRequest(
                student_id=player_a,
                level_id="MoveLeftRight",
                event_type="program_run",
                correct=True,
                skill_ids=["S3.1"],
            )
        )
    )

    state_a = asyncio.run(get_state(player_a))
    state_b = asyncio.run(get_state(player_b))

    assert player_a != player_b
    assert state_a.knowledge_state["S3.1"] > 0.30
    assert state_b.knowledge_state == {}
