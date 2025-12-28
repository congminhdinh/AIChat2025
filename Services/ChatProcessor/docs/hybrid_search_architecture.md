# Hybrid Search Architecture for RAG System

## Overview
This document describes the Hybrid Search implementation that combines:
1. **Vector Search** (semantic similarity via Qdrant)
2. **BM25 Keyword Search** (exact term matching via Qdrant payload filtering)
3. **RRF Re-ranking** (Reciprocal Rank Fusion)
4. **Fallback Mechanism** (tenant → global legal docs)

---

## Architecture Diagram

```
User Query + system_instruction
    ↓
┌─────────────────────────────────────────┐
│  Step 1: Query Enhancement              │
├─────────────────────────────────────────┤
│ 1.1 Keyword Expansion                   │
│     (system_instruction mapping)        │
│ 1.2 Legal Term Extraction               │
│     (Điều X, BHXH, BHTN, document names)│
└─────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────┐
│  Step 2: Parallel Hybrid Search         │
├─────────────────────────────────────────┤
│  For Each Scope (tenant + global):      │
│  ┌────────────────┬─────────────────┐   │
│  │ Vector Search  │  BM25 Keyword   │   │
│  │ (embeddings)   │  (payload match)│   │
│  └────────────────┴─────────────────┘   │
│           ↓              ↓               │
│  ┌────────────────────────────────────┐ │
│  │ RRF Re-ranking (merge results)     │ │
│  └────────────────────────────────────┘ │
└─────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────┐
│  Step 3: Fallback Mechanism             │
├─────────────────────────────────────────┤
│ IF tenant_results.empty:                │
│    Use only global_legal_results        │
│ ELSE:                                   │
│    Combine tenant + global results      │
└─────────────────────────────────────────┘
    ↓
┌─────────────────────────────────────────┐
│  Step 4: Final Re-ranking               │
├─────────────────────────────────────────┤
│ RRF fusion across tenant + global       │
│ Top-K selection (limit=5)               │
└─────────────────────────────────────────┘
    ↓
Scenario Detection & Context Building
```

---

## Component Details

### 1. Legal Term Extractor

Extracts structured legal references from Vietnamese queries:

**Patterns Recognized:**
- Article numbers: `Điều 212`, `điều 45`, `Điều 15 khoản 2`
- Abbreviations: `BHXH`, `BHTN`, `BHYT`, `NLĐ`, `NSDLĐ`
- Document names: `Bộ luật Lao động`, `Nghị định 145/2020/NĐ-CP`
- Dates: `năm 2019`, `tháng 12/2020`

**Implementation:**
```python
def extract_legal_terms(query: str, system_instruction: List[Dict]) -> List[str]:
    """
    Extract legal terms for BM25 keyword matching.

    Returns:
        List of keywords to use in payload filtering
    """
    terms = []

    # Article numbers (Điều X)
    article_pattern = r'điều\s+\d+(?:\s+khoản\s+\d+)?'
    terms.extend(re.findall(article_pattern, query.lower()))

    # Abbreviations from system_instruction
    for config in system_instruction:
        if config['key'] in query:
            terms.append(config['key'])

    # Document names (match against known legal docs)
    doc_patterns = [
        r'bộ luật [a-záàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵ\s]+',
        r'nghị định\s+\d+/\d+/[a-z\-]+',
        r'thông tư\s+\d+/\d+/[a-z\-]+'
    ]
    for pattern in doc_patterns:
        terms.extend(re.findall(pattern, query.lower()))

    return list(set(terms))  # Remove duplicates
```

### 2. BM25 Keyword Search (via Qdrant)

Qdrant supports keyword matching through payload filtering:

```python
async def search_with_keywords(
    query_vector: List[float],
    keywords: List[str],
    tenant_id: int,
    limit: int = 10
) -> List[ScoredPoint]:
    """
    Search using both vector similarity AND keyword matching.

    Uses Qdrant's must + should filters:
    - must: tenant_id filter
    - should: keyword matches in text/document_name/headings
    """
    # Build keyword filters
    keyword_filters = []
    for keyword in keywords:
        keyword_filters.append(
            FieldCondition(
                key='text',
                match=MatchText(text=keyword)  # Full-text search
            )
        )

    search_filter = Filter(
        must=[
            FieldCondition(key='tenant_id', match=MatchValue(value=tenant_id))
        ],
        should=keyword_filters  # Boost results containing keywords
    )

    results = await client.search(
        collection_name=collection_name,
        query_vector=query_vector,
        query_filter=search_filter,
        limit=limit,
        score_threshold=0.6  # Lower threshold for keyword matches
    )

    return results
```

### 3. Reciprocal Rank Fusion (RRF)

Combines results from vector search and keyword search:

```python
def reciprocal_rank_fusion(
    vector_results: List[ScoredPoint],
    keyword_results: List[ScoredPoint],
    k: int = 60
) -> List[ScoredPoint]:
    """
    RRF formula: score(d) = Σ 1 / (k + rank(d))

    Args:
        vector_results: Results from vector search (ranked by similarity)
        keyword_results: Results from keyword search (ranked by BM25)
        k: Constant (typically 60) to reduce impact of high ranks

    Returns:
        Re-ranked results sorted by RRF score
    """
    rrf_scores = {}

    # Add scores from vector search
    for rank, result in enumerate(vector_results, start=1):
        doc_id = result.id
        rrf_scores[doc_id] = rrf_scores.get(doc_id, 0) + 1 / (k + rank)

    # Add scores from keyword search
    for rank, result in enumerate(keyword_results, start=1):
        doc_id = result.id
        rrf_scores[doc_id] = rrf_scores.get(doc_id, 0) + 1 / (k + rank)

    # Sort by RRF score
    sorted_docs = sorted(rrf_scores.items(), key=lambda x: x[1], reverse=True)

    # Build final result list
    final_results = []
    for doc_id, score in sorted_docs:
        # Get the actual ScoredPoint (prefer from vector_results)
        result = next((r for r in vector_results if r.id == doc_id), None)
        if not result:
            result = next((r for r in keyword_results if r.id == doc_id), None)

        if result:
            # Override score with RRF score
            result.score = score
            final_results.append(result)

    return final_results
```

### 4. Fallback Mechanism

Intelligently falls back to global legal docs when tenant docs are insufficient:

```python
async def hybrid_search_with_fallback(
    query: str,
    query_vector: List[float],
    keywords: List[str],
    tenant_id: int,
    limit: int = 5
) -> Tuple[List[ScoredPoint], List[ScoredPoint]]:
    """
    Performs hybrid search with fallback logic.

    Flow:
    1. Search tenant docs (hybrid: vector + keyword)
    2. Search global legal docs (hybrid: vector + keyword)
    3. If tenant results < threshold, boost global results

    Returns:
        (tenant_results, global_results)
    """
    # Parallel hybrid search in both scopes
    tenant_task = hybrid_search_single_tenant(
        query_vector, keywords, tenant_id, limit
    )
    global_task = hybrid_search_single_tenant(
        query_vector, keywords, tenant_id=1, limit
    )

    tenant_results, global_results = await asyncio.gather(
        tenant_task, global_task
    )

    # Fallback logic
    MIN_TENANT_RESULTS = 2
    if len(tenant_results) < MIN_TENANT_RESULTS:
        logger.warning(
            f'Insufficient tenant results ({len(tenant_results)}), '
            f'prioritizing global legal docs'
        )
        # Boost global results when tenant is insufficient
        return tenant_results, global_results[:limit]
    else:
        # Normal case: balance tenant and global
        return tenant_results[:3], global_results[:2]
```

---

## Benefits of Hybrid Search

### 1. **Better Recall for Legal Terms**
- **Before**: "Điều 212 BHXH" might miss exact matches if embedding doesn't capture them
- **After**: Keyword filter ensures articles/abbreviations are matched exactly

### 2. **Improved Ranking**
- **Before**: Single similarity score
- **After**: RRF combines semantic similarity + keyword relevance

### 3. **Fallback Resilience**
- **Before**: If tenant docs are empty → no results
- **After**: Automatically falls back to global legal knowledge base

### 4. **Tenant-Specific Term Matching**
- **Before**: system_instruction only used for query expansion
- **After**: Also used to extract tenant-specific keywords for BM25

---

## Performance Considerations

1. **Latency Impact**: Minimal (~20-50ms increase)
   - Keyword extraction: ~5ms
   - Parallel searches: No additional latency (already parallel)
   - RRF computation: ~10ms for 20 documents

2. **Qdrant Optimization**:
   - Use `should` filters instead of separate queries
   - Payload indexing on `text`, `document_name`, `heading1`, `heading2`

3. **Caching**:
   - Cache legal term patterns (compiled regex)
   - Cache system_instruction → keyword mappings per tenant

---

## Integration Points

### QdrantService (Services/ChatProcessor/src/business.py:90)
- Add `hybrid_search_with_tenant_filter()` method
- Add `extract_legal_keywords()` helper

### ChatBusiness (Services/ChatProcessor/src/business.py:220)
- Update `process_chat_message()` to use hybrid search
- Add fallback logic based on result count

### System Instruction Usage
- Already integrated in query expansion
- **New**: Extract keywords for BM25 matching

---

## Testing Strategy

1. **Unit Tests**:
   - Legal term extraction (various patterns)
   - RRF calculation (known rankings)
   - Fallback logic (empty tenant results)

2. **Integration Tests**:
   - Hybrid search returns correct documents
   - RRF improves ranking over vector-only
   - Fallback activates when needed

3. **A/B Testing**:
   - Compare hybrid vs. vector-only on real queries
   - Measure recall@5, MRR, NDCG

---

## Configuration

New settings in `config.py`:

```python
# Hybrid Search Configuration
HYBRID_SEARCH_ENABLED = True
BM25_WEIGHT = 0.5  # Weight for keyword search (0-1)
VECTOR_WEIGHT = 0.5  # Weight for vector search (0-1)
RRF_K = 60  # RRF constant
MIN_TENANT_RESULTS_THRESHOLD = 2  # Trigger fallback if below this
KEYWORD_SCORE_THRESHOLD = 0.6  # Lower threshold for keyword matches
```

---

## Migration Path

**Phase 1**: Add hybrid search alongside existing vector search (feature flag)
**Phase 2**: A/B test hybrid vs. vector-only
**Phase 3**: Switch to hybrid as default
**Phase 4**: Deprecate vector-only path

This allows gradual rollout and easy rollback if needed.
