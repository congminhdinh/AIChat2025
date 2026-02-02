"""
Unit Tests for Hybrid Search Module

Tests for:
- LegalTermExtractor
- ReciprocalRankFusion
- HybridSearchStrategy
"""

import pytest
from typing import List
from qdrant_client.models import ScoredPoint
from src.hybrid_search import (
    LegalTermExtractor,
    ReciprocalRankFusion,
    HybridSearchStrategy,
    merge_and_deduplicate
)


class TestLegalTermExtractor:
    """Test suite for LegalTermExtractor."""

    def test_extract_article_numbers(self):
        """Test extraction of article numbers (Điều X)."""
        query = "Theo Điều 212 và Điều 45 khoản 2, công ty có trách nhiệm gì?"
        keywords = LegalTermExtractor.extract_keywords(query)

        assert "điều 212" in keywords
        assert "điều 45 khoản 2" in keywords

    def test_extract_abbreviations(self):
        """Test extraction of common legal abbreviations."""
        query = "Công ty có phải đóng BHXH, BHYT và BHTN không?"
        keywords = LegalTermExtractor.extract_keywords(query)

        assert "BHXH" in keywords
        assert "BHYT" in keywords
        assert "BHTN" in keywords

    def test_extract_law_codes(self):
        """Test extraction of law code names."""
        query = "Bộ luật Lao động có quy định gì về nghỉ phép?"
        keywords = LegalTermExtractor.extract_keywords(query)

        assert any("bộ luật lao động" in kw for kw in keywords)

    def test_extract_decrees(self):
        """Test extraction of decree references."""
        query = "Nghị định 145/2020/NĐ-CP về bảo hiểm xã hội"
        keywords = LegalTermExtractor.extract_keywords(query)

        assert any("nghị định 145/2020/nđ-cp" in kw for kw in keywords)

    def test_extract_circulars(self):
        """Test extraction of circular references."""
        query = "Thông tư 28/2015/TT-BLĐTBXH hướng dẫn thế nào?"
        keywords = LegalTermExtractor.extract_keywords(query)

        assert any("thông tư 28/2015/tt-blđtbxh" in kw for kw in keywords)

    def test_extract_years(self):
        """Test extraction of year references."""
        query = "Quy định năm 2019 và năm 2020 có khác gì?"
        keywords = LegalTermExtractor.extract_keywords(query)

        assert any("năm 2019" in kw for kw in keywords)
        assert any("năm 2020" in kw for kw in keywords)

    def test_extract_with_system_instruction(self):
        """Test extraction with tenant-specific terms from system_instruction."""
        query = "How is OT calculated?"
        system_instruction = [
            {"key": "OT", "value": "Overtime Payment"},
            {"key": "PTO", "value": "Paid Time Off"}
        ]

        keywords = LegalTermExtractor.extract_keywords(query, system_instruction)

        # Should extract both key and value
        assert "ot" in keywords
        assert "overtime payment" in keywords

    def test_no_duplicates(self):
        """Test that duplicate keywords are removed."""
        query = "Điều 212 BHXH, Điều 212 có quy định BHXH như thế nào?"
        keywords = LegalTermExtractor.extract_keywords(query)

        # Should not have duplicates
        assert len(keywords) == len(set(keywords))
        assert keywords.count("điều 212") == 1
        assert keywords.count("BHXH") == 1

    def test_empty_query(self):
        """Test extraction from empty query."""
        keywords = LegalTermExtractor.extract_keywords("")
        assert keywords == []

    def test_query_without_legal_terms(self):
        """Test extraction from query without legal terms."""
        query = "What is the weather today?"
        keywords = LegalTermExtractor.extract_keywords(query)
        assert len(keywords) == 0


class TestReciprocalRankFusion:
    """Test suite for ReciprocalRankFusion."""

    def _create_scored_point(self, doc_id: str, score: float) -> ScoredPoint:
        """Helper to create a ScoredPoint for testing."""
        return ScoredPoint(
            id=doc_id,
            version=1,
            score=score,
            payload={"text": f"Document {doc_id}"},
            vector=None
        )

    def test_rrf_basic_fusion(self):
        """Test basic RRF fusion of two result lists."""
        vector_results = [
            self._create_scored_point("doc1", 0.95),
            self._create_scored_point("doc2", 0.85),
            self._create_scored_point("doc3", 0.75)
        ]

        keyword_results = [
            self._create_scored_point("doc2", 0.90),
            self._create_scored_point("doc4", 0.80),
            self._create_scored_point("doc1", 0.70)
        ]

        fused = ReciprocalRankFusion.fuse(vector_results, keyword_results, k=60)

        # doc2 appears in both lists at high ranks → should rank highest
        assert fused[0].id == "doc2"

        # All unique documents should be in result
        fused_ids = {r.id for r in fused}
        assert fused_ids == {"doc1", "doc2", "doc3", "doc4"}

    def test_rrf_with_k_parameter(self):
        """Test RRF with different k values."""
        vector_results = [self._create_scored_point("doc1", 0.9)]
        keyword_results = [self._create_scored_point("doc2", 0.8)]

        # With k=60
        fused_k60 = ReciprocalRankFusion.fuse(vector_results, keyword_results, k=60)
        score_k60 = fused_k60[0].score

        # With k=1 (more emphasis on rank difference)
        fused_k1 = ReciprocalRankFusion.fuse(vector_results, keyword_results, k=1)
        score_k1 = fused_k1[0].score

        # Scores should be different
        assert score_k60 != score_k1

    def test_rrf_empty_lists(self):
        """Test RRF with empty result lists."""
        vector_results = []
        keyword_results = []

        fused = ReciprocalRankFusion.fuse(vector_results, keyword_results)
        assert len(fused) == 0

    def test_rrf_single_list(self):
        """Test RRF when only one list has results."""
        vector_results = [
            self._create_scored_point("doc1", 0.9),
            self._create_scored_point("doc2", 0.8)
        ]
        keyword_results = []

        fused = ReciprocalRankFusion.fuse(vector_results, keyword_results)

        # Should return vector results with updated RRF scores
        assert len(fused) == 2
        assert fused[0].id == "doc1"
        assert fused[1].id == "doc2"

    def test_rrf_multi_source_fusion(self):
        """Test multi-source RRF fusion."""
        list1 = [self._create_scored_point("doc1", 0.9)]
        list2 = [self._create_scored_point("doc2", 0.8)]
        list3 = [self._create_scored_point("doc1", 0.85)]

        fused = ReciprocalRankFusion.fuse_multi_source([list1, list2, list3], k=60)

        # doc1 appears in list1 and list3 → should rank highest
        assert fused[0].id == "doc1"
        assert len(fused) == 2  # Only 2 unique documents


class TestHybridSearchStrategy:
    """Test suite for HybridSearchStrategy."""

    def _create_scored_point(self, doc_id: str, score: float) -> ScoredPoint:
        """Helper to create a ScoredPoint for testing."""
        return ScoredPoint(
            id=doc_id,
            version=1,
            score=score,
            payload={"text": f"Document {doc_id}"},
            vector=None
        )

    def test_fallback_triggered_insufficient_tenant_results(self):
        """Test fallback when tenant results are insufficient."""
        # Only 1 tenant result (below threshold of 2)
        tenant_results = [self._create_scored_point("tenant1", 0.75)]

        # Strong global results
        global_results = [
            self._create_scored_point("global1", 0.85),
            self._create_scored_point("global2", 0.80),
            self._create_scored_point("global3", 0.75),
            self._create_scored_point("global4", 0.70)
        ]

        tenant_filtered, global_filtered, fallback = HybridSearchStrategy.apply_fallback_logic(
            tenant_results, global_results, limit=5
        )

        assert fallback == True
        assert len(tenant_filtered) <= 2
        assert len(global_filtered) >= 3

    def test_fallback_not_triggered_sufficient_tenant_results(self):
        """Test normal operation when tenant results are sufficient."""
        # 3 high-quality tenant results
        tenant_results = [
            self._create_scored_point("tenant1", 0.85),
            self._create_scored_point("tenant2", 0.80),
            self._create_scored_point("tenant3", 0.75)
        ]

        global_results = [
            self._create_scored_point("global1", 0.85),
            self._create_scored_point("global2", 0.80)
        ]

        tenant_filtered, global_filtered, fallback = HybridSearchStrategy.apply_fallback_logic(
            tenant_results, global_results, limit=5
        )

        assert fallback == False
        # Balanced split: ~60% tenant, ~40% global
        assert len(tenant_filtered) >= 2
        assert len(global_filtered) >= 1

    def test_fallback_low_quality_tenant_results(self):
        """Test fallback when tenant results have low quality scores."""
        # Multiple tenant results but all low quality (< 0.5 QUALITY_COSINE_THRESHOLD)
        tenant_results = [
            self._create_scored_point("tenant1", 0.45),
            self._create_scored_point("tenant2", 0.40),
            self._create_scored_point("tenant3", 0.35)
        ]

        global_results = [
            self._create_scored_point("global1", 0.85),
            self._create_scored_point("global2", 0.80)
        ]

        tenant_filtered, global_filtered, fallback = HybridSearchStrategy.apply_fallback_logic(
            tenant_results, global_results, limit=5
        )

        # Should trigger fallback because no high-quality tenant results (all < 0.5)
        assert fallback == True

    def test_fallback_respects_limit(self):
        """Test that result count respects the limit parameter."""
        tenant_results = [self._create_scored_point(f"tenant{i}", 0.8) for i in range(10)]
        global_results = [self._create_scored_point(f"global{i}", 0.8) for i in range(10)]

        tenant_filtered, global_filtered, fallback = HybridSearchStrategy.apply_fallback_logic(
            tenant_results, global_results, limit=5
        )

        # Total should not exceed limit
        assert len(tenant_filtered) + len(global_filtered) <= 5


class TestMergeAndDeduplicate:
    """Test suite for merge_and_deduplicate function."""

    def _create_scored_point(self, doc_id: str, score: float) -> ScoredPoint:
        """Helper to create a ScoredPoint for testing."""
        return ScoredPoint(
            id=doc_id,
            version=1,
            score=score,
            payload={"text": f"Document {doc_id}"},
            vector=None
        )

    def test_merge_basic(self):
        """Test basic merge of tenant and global results."""
        tenant_results = [
            self._create_scored_point("doc1", 0.9),
            self._create_scored_point("doc2", 0.8)
        ]

        global_results = [
            self._create_scored_point("doc3", 0.85),
            self._create_scored_point("doc4", 0.75)
        ]

        merged = merge_and_deduplicate(tenant_results, global_results, limit=5)

        assert len(merged) == 4
        # Should be sorted by score
        assert merged[0].id == "doc1"  # 0.9
        assert merged[1].id == "doc3"  # 0.85

    def test_merge_with_duplicates(self):
        """Test that duplicates are removed (tenant takes priority)."""
        tenant_results = [
            self._create_scored_point("doc1", 0.9),
            self._create_scored_point("doc2", 0.8)
        ]

        global_results = [
            self._create_scored_point("doc1", 0.85),  # Duplicate
            self._create_scored_point("doc3", 0.75)
        ]

        merged = merge_and_deduplicate(tenant_results, global_results, limit=5)

        # Should have 3 unique documents
        assert len(merged) == 3

        # doc1 should appear only once (from tenant)
        doc1_count = sum(1 for r in merged if r.id == "doc1")
        assert doc1_count == 1

    def test_merge_respects_limit(self):
        """Test that merge respects the limit parameter."""
        tenant_results = [self._create_scored_point(f"tenant{i}", 0.8) for i in range(5)]
        global_results = [self._create_scored_point(f"global{i}", 0.7) for i in range(5)]

        merged = merge_and_deduplicate(tenant_results, global_results, limit=3)

        # Should limit to 3 results
        assert len(merged) == 3

    def test_merge_empty_lists(self):
        """Test merge with empty lists."""
        merged = merge_and_deduplicate([], [], limit=5)
        assert len(merged) == 0

    def test_merge_sorted_by_score(self):
        """Test that merged results are sorted by score."""
        tenant_results = [
            self._create_scored_point("doc1", 0.7),
            self._create_scored_point("doc2", 0.9)
        ]

        global_results = [
            self._create_scored_point("doc3", 0.8),
            self._create_scored_point("doc4", 0.6)
        ]

        merged = merge_and_deduplicate(tenant_results, global_results, limit=5)

        # Check sorting (descending by score)
        scores = [r.score for r in merged]
        assert scores == sorted(scores, reverse=True)
        assert merged[0].id == "doc2"  # Highest score (0.9)


# Integration Test Examples (require Qdrant connection)
class TestHybridSearchIntegration:
    """Integration tests for hybrid search (requires running Qdrant instance)."""

    @pytest.mark.integration
    @pytest.mark.asyncio
    async def test_hybrid_search_end_to_end(self):
        """Test complete hybrid search flow."""
        # This would require:
        # 1. Running Qdrant instance
        # 2. Sample data loaded
        # 3. QdrantService instance
        pytest.skip("Integration test - requires Qdrant")

    @pytest.mark.integration
    @pytest.mark.asyncio
    async def test_fallback_mechanism_end_to_end(self):
        """Test fallback mechanism with real Qdrant queries."""
        pytest.skip("Integration test - requires Qdrant")


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
