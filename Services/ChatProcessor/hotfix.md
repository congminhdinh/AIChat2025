# Hotfix: Multi-Source Search with RRF

## Problem Statement

The previous implementation had several critical issues:

1. **Fake Hybrid Search**: The "keyword search" was not actual BM25 - it was vector search with keyword filters, returning the same cosine similarity scores
2. **Meaningless RRF**: RRF was used to combine vector + keyword results within each source, but both had identical ranking signals (cosine similarity)
3. **Broken Quality Threshold**: The fallback logic checked for `score >= 0.7`, but after RRF fusion, scores max out at ~0.033, making this check always fail

## Solution: Option 1 - RRF for Cross-Source Fusion

### New Logic Flow

```
User Query
    │
    ▼
Query Expansion (abbreviations → full terms)
    │
    ▼
Vector Embedding
    │
    ├─────────────────────┬─────────────────────┐
    ▼                     ▼                     │
Tenant Search         Global Search             │
(tenant_id = X)       (tenant_id = 1)           │
Pure Vector Search    Pure Vector Search        │
Cosine Similarity     Cosine Similarity         │
    │                     │                     │
    ▼                     ▼                     │
Top K Results         Top K Results             │
(with cosine scores)  (with cosine scores)      │
    │                     │                     │
    └──────────┬──────────┘                     │
               ▼                                │
    Fallback Logic (using cosine scores)        │
    - Check if tenant has ≥2 results with       │
      cosine score ≥ 0.5                        │
    - If yes: 60% tenant + 40% global           │
    - If no: prioritize global (fallback)       │
               │                                │
               ▼                                │
    RRF Fusion (Tenant + Global)                │
    - Score = 1/(60 + rank_tenant)              │
            + 1/(60 + rank_global)              │
    - Documents in both lists get boosted       │
               │                                │
               ▼                                │
         Final Top 5                            │
               │
               ▼
    Scenario Detection & Response
```

### Key Changes

1. **Removed fake hybrid search within each source**
   - No more vector + keyword + RRF per tenant
   - Pure vector search only

2. **RRF now combines Tenant + Global results**
   - Meaningful fusion of two different document sources
   - Guarantees representation from both sources
   - Documents appearing in both get ranking boost

3. **Fixed quality threshold**
   - Changed from `score >= 0.7` (impossible with RRF) to `score >= 0.5` (cosine similarity)
   - Fallback logic now applied BEFORE RRF, using original cosine scores

### Why This Works

| Aspect | Before (Broken) | After (Fixed) |
|--------|-----------------|---------------|
| RRF combines | Vector + Keyword (same scores) | Tenant + Global (different sources) |
| Quality check | RRF scores (max 0.033) | Cosine scores (0-1) |
| Threshold | 0.7 (never met) | 0.5 (realistic) |
| Benefit of RRF | None | Balances two sources |

### Files Modified

1. **hybrid_search.py**
   - Updated module docstring
   - Renamed RRF parameters: `vector_results/keyword_results` → `tenant_results/global_results`
   - Fixed `HybridSearchStrategy.apply_fallback_logic`:
     - `QUALITY_COSINE_THRESHOLD = 0.5`
     - `FALLBACK_COSINE_THRESHOLD = 0.4`
     - Added note that method expects cosine scores, not RRF scores

2. **business.py**
   - Updated `hybrid_search_with_fallback`:
     - Now calls `search_exact_tenant` directly (pure vector search)
     - Applies fallback logic BEFORE RRF (with cosine scores)
     - Applies RRF fusion AFTER fallback to combine filtered results
     - `hybrid_search_single_tenant` is no longer used (kept for backward compatibility)

### Implementation Steps

1. Created new branch `hotfix` from `zip`
2. Updated module docstring in `hybrid_search.py` to reflect new architecture
3. Renamed RRF `fuse()` parameters for clarity
4. Fixed `apply_fallback_logic` thresholds to use cosine similarity values
5. Simplified `hybrid_search_with_fallback` to:
   - Use pure vector search for both sources
   - Apply fallback with cosine scores
   - Apply RRF for cross-source fusion
6. Verified syntax with `python -m py_compile`

### Testing Checklist

- [ ] Verify tenant search returns results with cosine scores
- [ ] Verify global search returns results with cosine scores
- [ ] Verify fallback triggers when tenant has < 2 results with score >= 0.5
- [ ] Verify RRF fusion produces combined ranking
- [ ] Verify final results contain documents from both sources (when available)
