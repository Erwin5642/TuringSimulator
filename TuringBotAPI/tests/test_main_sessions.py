from fastapi.testclient import TestClient

from main import app


def test_health_reports_fallback_and_documents():
    with TestClient(app) as client:
        response = client.get("/health")
    assert response.status_code == 200
    body = response.json()
    assert body["status"] == "ok"
    assert body["tutor_provider"] == "fallback"
    assert body["documents"] >= 20


def test_session_new_returns_unique_ids():
    with TestClient(app) as client:
        first = client.post("/session/new").json()["student_id"]
        second = client.post("/session/new").json()["student_id"]
    assert first.startswith("student_")
    assert second.startswith("student_")
    assert first != second


def test_ask_offline_returns_ptbr_reply():
    with TestClient(app) as client:
        response = client.post(
            "/ask",
            json={
                "student_id": "student_test",
                "level_id": "MoveLeftRight",
                "question": "Como eu falo com o tutor?",
            },
        )
    assert response.status_code == 200
    body = response.json()
    assert "audio_url" not in body
    assert body["reply"]
    lowered = body["reply"].lower()
    assert "shaka" in lowered or "mão direita" in lowered or "radio" in lowered or "rádio" in lowered
    assert body["tokens_in"] > 0
    assert body["tokens_out"] > 0


def test_ask_rejects_empty_question():
    with TestClient(app) as client:
        response = client.post(
            "/ask",
            json={
                "student_id": "student_test",
                "level_id": "MoveLeftRight",
                "question": "   ",
            },
        )
    assert response.status_code == 422


def test_removed_endpoints_are_gone():
    with TestClient(app) as client:
        assert client.post("/event", json={}).status_code == 404
        assert client.post("/hint", json={}).status_code == 404
        assert client.get("/state/student_x").status_code == 404


def test_web_tester_is_served():
    with TestClient(app) as client:
        page = client.get("/web-tester/index.html")
        root = client.get("/", follow_redirects=False)
    assert page.status_code == 200
    assert "ITS Web Tester" in page.text
    assert "tokens in" in page.text
    assert "tokens out" in page.text
    assert root.status_code == 307
    assert root.headers["location"] == "/web-tester/"
