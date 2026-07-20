"""
tests/test_concepts.py

Validates structural integrity of the concept map:
  - All prerequisite references resolve to real skill IDs
  - No cyclic dependencies
  - Every level ID in exercised_in / introduced_in is a known level
  - BKT parameters are in valid probability ranges
  - Every skill introduced in a level is also listed in exercised_in
"""

import pytest
from domain.concepts import (
    CONCEPT_MAP,
    CLUSTER_BKT_DEFAULTS,
    SkillCluster,
)

KNOWN_LEVELS = {
    "MoveLeftRight",
    "PlaceGear",
    "ReplaceAllWithNuts",
    "RejectIfGearExists",
    "SwapNutsAndScrews",
    "PatternRepeated",
    "BalancedPairs",
    "PatternSomewhere",
}

ALL_SKILL_IDS = {s.id for s in CONCEPT_MAP.skills}


class TestSkillIntegrity:

    def test_all_prerequisite_ids_exist(self):
        for skill in CONCEPT_MAP.skills:
            for pid in skill.prerequisites:
                assert pid in ALL_SKILL_IDS, (
                    f"Skill {skill.id} has unknown prerequisite '{pid}'."
                )

    def test_no_cyclic_dependencies(self):
        order = CONCEPT_MAP.topological_order()
        assert len(order) == len(CONCEPT_MAP.skills)

    def test_introduced_in_is_known_level(self):
        for skill in CONCEPT_MAP.skills:
            assert skill.introduced_in in KNOWN_LEVELS, (
                f"Skill {skill.id} introduced_in unknown level "
                f"'{skill.introduced_in}'."
            )

    def test_exercised_in_are_known_levels(self):
        for skill in CONCEPT_MAP.skills:
            for lid in skill.exercised_in:
                assert lid in KNOWN_LEVELS, (
                    f"Skill {skill.id} exercised_in unknown level '{lid}'."
                )

    def test_introduced_in_is_subset_of_exercised_in(self):
        for skill in CONCEPT_MAP.skills:
            assert skill.introduced_in in skill.exercised_in, (
                f"Skill {skill.id}: introduced_in '{skill.introduced_in}' "
                f"not listed in exercised_in."
            )

    def test_bkt_params_in_valid_range(self):
        for skill in CONCEPT_MAP.skills:
            params = CONCEPT_MAP.get_bkt_params(skill.id)
            for field, value in params.model_dump().items():
                assert 0.0 <= value <= 1.0, (
                    f"Skill {skill.id}: BKT param {field} = {value} "
                    f"out of [0, 1]."
                )

    def test_cluster_defaults_cover_all_clusters(self):
        for cluster in SkillCluster:
            assert cluster in CLUSTER_BKT_DEFAULTS, (
                f"No BKT default defined for cluster '{cluster}'."
            )

    def test_all_skill_ids_unique(self):
        ids = [s.id for s in CONCEPT_MAP.skills]
        assert len(ids) == len(set(ids)), "Duplicate skill IDs detected."


class TestConceptMapQueries:

    def test_get_skill_returns_correct(self):
        skill = CONCEPT_MAP.get_skill("S4.1")
        assert skill.name == "Condition block"

    def test_get_skill_raises_on_unknown(self):
        with pytest.raises(KeyError):
            CONCEPT_MAP.get_skill("S99.99")

    def test_get_prerequisites_resolves(self):
        prereqs = CONCEPT_MAP.get_prerequisites("S4.1")
        ids = [p.id for p in prereqs]
        assert "S1.2" in ids
        assert "S2.1" in ids

    def test_get_skills_for_level(self):
        skills = CONCEPT_MAP.get_skills_for_level("MoveLeftRight")
        ids = [s.id for s in skills]
        assert "S1.1" in ids
        assert "S3.1" in ids

    def test_get_introduced_skills(self):
        introduced = CONCEPT_MAP.get_introduced_skills("ReplaceAllWithNuts")
        ids = [s.id for s in introduced]
        assert "S4.1" in ids
        assert "S4.5" in ids
        assert "S1.4" in ids

    def test_get_skills_by_cluster(self):
        control_skills = CONCEPT_MAP.get_skills_by_cluster(SkillCluster.CONTROL)
        ids = [s.id for s in control_skills]
        assert "S4.1" in ids
        assert "S4.5" in ids

    def test_topological_order_respects_prerequisites(self):
        order = CONCEPT_MAP.topological_order()
        pos = {sid: i for i, sid in enumerate(order)}
        for skill in CONCEPT_MAP.skills:
            for pid in skill.prerequisites:
                assert pos[pid] < pos[skill.id], (
                    f"Prerequisite {pid} appears after {skill.id} "
                    f"in topological order."
                )

    def test_prerequisite_graph_keys_match_skills(self):
        graph = CONCEPT_MAP.prerequisite_graph()
        assert set(graph.keys()) == ALL_SKILL_IDS