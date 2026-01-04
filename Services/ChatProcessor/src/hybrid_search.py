"""
Hybrid Search Module for RAG System

This module implements hybrid search combining:
1. Vector search (semantic similarity)
2. BM25 keyword search (exact term matching)
3. RRF (Reciprocal Rank Fusion) re-ranking
4. Fallback mechanism (tenant → global legal docs)
"""

import re
from typing import List, Dict, Tuple, Optional
from qdrant_client.models import ScoredPoint
from src.logger import logger


class LegalTermExtractor:
    """Extracts legal terms from Vietnamese queries for BM25 keyword matching."""

    # Precompiled regex patterns for performance
    ARTICLE_PATTERN = re.compile(r'điều\s+\d+(?:\s+khoản\s+\d+)?', re.IGNORECASE)
    LAW_CODE_PATTERN = re.compile(
        r'bộ luật [a-záàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđ\s]+',
        re.IGNORECASE
    )
    DECREE_PATTERN = re.compile(r'nghị định\s+\d+/\d+/[a-z\-]+', re.IGNORECASE)
    CIRCULAR_PATTERN = re.compile(r'thông tư\s+\d+/\d+/[a-z\-]+', re.IGNORECASE)
    YEAR_PATTERN = re.compile(r'năm\s+\d{4}', re.IGNORECASE)

    # Common Vietnamese legal abbreviations with their full forms
    # This mapping enables expansion for better keyword matching
    ABBREVIATION_EXPANSIONS = {
        'BHXH': 'Bảo hiểm xã hội',
        'BHYT': 'Bảo hiểm y tế',
        'BHTN': 'Bảo hiểm thất nghiệp',
        'NLĐ': 'Người lao động',
        'NSDLĐ': 'Người sử dụng lao động',
        'CBNV': 'Cán bộ nhân viên',
        'CNVC': 'Công nhân viên chức',
        'PCCC': 'Phòng cháy chữa cháy',
        'ATVSLĐ': 'An toàn vệ sinh lao động',
        'HĐLĐ': 'Hợp đồng lao động',
    }

    # Keep the set for quick lookup
    COMMON_ABBREVIATIONS = set(ABBREVIATION_EXPANSIONS.keys())

    @classmethod
    def extract_keywords(
        cls,
        query: str,
        system_instruction: Optional[List[Dict[str, str]]] = None
    ) -> List[str]:
        """
        Extract legal keywords from query for BM25 matching.

        Args:
            query: User query string
            system_instruction: Tenant-specific term definitions

        Returns:
            List of unique keywords extracted from query
        """
        keywords = []

        # 1. Extract article numbers (Điều X, Điều X khoản Y)
        article_matches = cls.ARTICLE_PATTERN.findall(query)
        keywords.extend([m.lower() for m in article_matches])

        # 2. Extract law codes (Bộ luật Lao động)
        law_matches = cls.LAW_CODE_PATTERN.findall(query)
        keywords.extend([m.strip().lower() for m in law_matches])

        # 3. Extract decrees (Nghị định X/Y/NĐ-CP)
        decree_matches = cls.DECREE_PATTERN.findall(query)
        keywords.extend([m.lower() for m in decree_matches])

        # 4. Extract circulars (Thông tư X/Y/TT-...)
        circular_matches = cls.CIRCULAR_PATTERN.findall(query)
        keywords.extend([m.lower() for m in circular_matches])

        # 5. Extract years (năm 2019)
        year_matches = cls.YEAR_PATTERN.findall(query)
        keywords.extend([m.lower() for m in year_matches])

        # 6. Extract common abbreviations from query and expand them
        query_upper = query.upper()
        for abbr in cls.COMMON_ABBREVIATIONS:
            if abbr in query_upper:
                # Add the abbreviation itself
                keywords.append(abbr)
                # Also add the expanded full form for better matching
                if abbr in cls.ABBREVIATION_EXPANSIONS:
                    expanded_form = cls.ABBREVIATION_EXPANSIONS[abbr].lower()
                    keywords.append(expanded_form)
                    logger.debug(f'Expanded abbreviation "{abbr}" to "{expanded_form}"')

        # 7. Extract tenant-specific terms from system_instruction
        if system_instruction:
            for config_item in system_instruction:
                key = config_item.get('key', '')
                if key and key in query:
                    # Add both the key and value for better matching
                    keywords.append(key.lower())
                    value = config_item.get('value', '')
                    if value:
                        keywords.append(value.lower())

        # Remove duplicates while preserving order
        seen = set()
        unique_keywords = []
        for kw in keywords:
            if kw and kw not in seen:
                seen.add(kw)
                unique_keywords.append(kw)

        if unique_keywords:
            logger.info(f'Extracted {len(unique_keywords)} legal keywords: {unique_keywords}')
        else:
            logger.debug('No legal keywords extracted from query')

        return unique_keywords


class ReciprocalRankFusion:
    """Implements Reciprocal Rank Fusion (RRF) for combining search results."""

    DEFAULT_K = 60  # Standard RRF constant

    @classmethod
    def fuse(
        cls,
        vector_results: List[ScoredPoint],
        keyword_results: List[ScoredPoint],
        k: int = DEFAULT_K
    ) -> List[ScoredPoint]:
        """
        Combine results from vector and keyword search using RRF.

        RRF formula: score(d) = Σ 1 / (k + rank_i(d))

        Args:
            vector_results: Results from vector search (ranked by cosine similarity)
            keyword_results: Results from keyword/BM25 search
            k: Constant to reduce impact of high ranks (default: 60)

        Returns:
            Re-ranked list of ScoredPoint objects sorted by RRF score
        """
        rrf_scores = {}
        result_map = {}  # Store actual ScoredPoint objects

        # Process vector search results
        for rank, result in enumerate(vector_results, start=1):
            doc_id = result.id
            rrf_scores[doc_id] = rrf_scores.get(doc_id, 0.0) + 1.0 / (k + rank)
            result_map[doc_id] = result

        # Process keyword search results
        for rank, result in enumerate(keyword_results, start=1):
            doc_id = result.id
            rrf_scores[doc_id] = rrf_scores.get(doc_id, 0.0) + 1.0 / (k + rank)
            # Prefer vector result if exists, otherwise use keyword result
            if doc_id not in result_map:
                result_map[doc_id] = result

        # Sort by RRF score (descending)
        sorted_doc_ids = sorted(rrf_scores.items(), key=lambda x: x[1], reverse=True)

        # Build final result list with updated scores
        final_results = []
        for doc_id, rrf_score in sorted_doc_ids:
            result = result_map[doc_id]
            # Create new ScoredPoint with RRF score
            # Note: Qdrant's ScoredPoint is immutable, so we create a new instance
            fused_result = ScoredPoint(
                id=result.id,
                version=result.version,
                score=rrf_score,
                payload=result.payload,
                vector=result.vector
            )
            final_results.append(fused_result)

        logger.info(
            f'RRF fusion: {len(vector_results)} vector + {len(keyword_results)} keyword '
            f'→ {len(final_results)} unique results'
        )

        return final_results

    @classmethod
    def fuse_multi_source(
        cls,
        result_lists: List[List[ScoredPoint]],
        k: int = DEFAULT_K
    ) -> List[ScoredPoint]:
        """
        Fuse multiple result lists using RRF.

        Useful for combining results from multiple tenants or sources.

        Args:
            result_lists: List of result lists to fuse
            k: RRF constant

        Returns:
            Fused and re-ranked results
        """
        rrf_scores = {}
        result_map = {}

        for result_list in result_lists:
            for rank, result in enumerate(result_list, start=1):
                doc_id = result.id
                rrf_scores[doc_id] = rrf_scores.get(doc_id, 0.0) + 1.0 / (k + rank)
                if doc_id not in result_map:
                    result_map[doc_id] = result

        # Sort and build final results
        sorted_doc_ids = sorted(rrf_scores.items(), key=lambda x: x[1], reverse=True)

        final_results = []
        for doc_id, rrf_score in sorted_doc_ids:
            result = result_map[doc_id]
            fused_result = ScoredPoint(
                id=result.id,
                version=result.version,
                score=rrf_score,
                payload=result.payload,
                vector=result.vector
            )
            final_results.append(fused_result)

        logger.info(f'Multi-source RRF fusion: {len(result_lists)} sources → {len(final_results)} results')

        return final_results


class HybridSearchStrategy:
    """Defines strategies for combining tenant and global search results."""

    # Thresholds for fallback logic
    MIN_TENANT_RESULTS = 2  # Minimum tenant results before triggering fallback
    FALLBACK_SIMILARITY_THRESHOLD = 0.65  # Lower threshold when falling back

    @classmethod
    def apply_fallback_logic(
        cls,
        tenant_results: List[ScoredPoint],
        global_results: List[ScoredPoint],
        limit: int = 5
    ) -> Tuple[List[ScoredPoint], List[ScoredPoint], bool]:
        """
        Apply fallback logic based on tenant result quality.

        Strategy:
        1. If tenant has >= MIN_TENANT_RESULTS good results: balanced split
        2. If tenant has < MIN_TENANT_RESULTS: prioritize global legal docs

        Args:
            tenant_results: Results from tenant-specific docs
            global_results: Results from global legal knowledge base
            limit: Total result limit

        Returns:
            (tenant_results_filtered, global_results_filtered, fallback_triggered)
        """
        fallback_triggered = False

        # Count high-quality tenant results
        quality_tenant_results = [
            r for r in tenant_results
            if r.score >= 0.7
        ]

        if len(quality_tenant_results) < cls.MIN_TENANT_RESULTS:
            # FALLBACK: Insufficient tenant results
            logger.warning(
                f'Fallback triggered: Only {len(quality_tenant_results)} quality tenant results '
                f'(threshold: {cls.MIN_TENANT_RESULTS})'
            )
            fallback_triggered = True

            # Prioritize global results, keep all tenant results
            # Lower the threshold for global results in fallback mode
            quality_global_results = [
                r for r in global_results
                if r.score >= cls.FALLBACK_SIMILARITY_THRESHOLD
            ]

            # Balance: Keep up to 1-2 tenant results, rest from global
            tenant_limit = min(len(tenant_results), 2)
            global_limit = limit - tenant_limit

            return (
                tenant_results[:tenant_limit],
                quality_global_results[:global_limit],
                fallback_triggered
            )
        else:
            # NORMAL: Balanced split between tenant and global
            # Typical split: 60% tenant, 40% global (3:2 for limit=5)
            tenant_limit = max(1, int(limit * 0.6))
            global_limit = limit - tenant_limit

            logger.info(
                f'Balanced split: {tenant_limit} tenant + {global_limit} global '
                f'(total: {limit})'
            )

            return (
                tenant_results[:tenant_limit],
                global_results[:global_limit],
                fallback_triggered
            )


def merge_and_deduplicate(
    tenant_results: List[ScoredPoint],
    global_results: List[ScoredPoint],
    limit: int = 5
) -> List[ScoredPoint]:
    """
    Merge tenant and global results, remove duplicates, sort by score.

    Args:
        tenant_results: Results from tenant docs
        global_results: Results from global docs
        limit: Maximum number of results to return

    Returns:
        Merged, deduplicated, and sorted results
    """
    # Use dict to deduplicate by ID (keeps first occurrence)
    seen_ids = {}

    # Add tenant results first (priority)
    for result in tenant_results:
        if result.id not in seen_ids:
            seen_ids[result.id] = result

    # Add global results
    for result in global_results:
        if result.id not in seen_ids:
            seen_ids[result.id] = result

    # Sort by score and limit
    merged = sorted(seen_ids.values(), key=lambda x: x.score, reverse=True)

    logger.info(
        f'Merged results: {len(tenant_results)} tenant + {len(global_results)} global '
        f'→ {len(seen_ids)} unique → top {min(limit, len(merged))}'
    )

    return merged[:limit]
