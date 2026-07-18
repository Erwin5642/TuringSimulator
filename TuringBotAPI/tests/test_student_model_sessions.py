from student_model import StudentModel


def test_create_new_student_returns_unique_ids():
    model = StudentModel()

    first = model.create_new_student()
    second = model.create_new_student()

    assert first != second


def test_new_student_starts_with_empty_knowledge_state():
    model = StudentModel()
    student_id = model.create_new_student()

    assert model.knowledge_state(student_id) == {}
