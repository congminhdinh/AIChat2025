# Hybrid Search Quick Start Guide

## 🚀 Quick Overview

The Hybrid Search feature combines **Vector Search** (semantic similarity) with **BM25 Keyword Search** (exact term matching) to improve retrieval accuracy for Vietnamese legal documents.

---

## ✅ What's New?

### 1. **Automatic Legal Term Extraction**
Queries are automatically analyzed to extract:
- Article numbers: `Điều 212`, `Điều 45 khoản 2`
- Abbreviations: `BHXH`, `BHTN`, `BHYT`, `NLĐ`
- Law codes: `Bộ luật Lao động`
- Decrees & Circulars: `Nghị định 145/2020/NĐ-CP`

### 2. **BM25 Keyword Matching**
Extracted keywords are matched against document metadata:
- `text` field (document content)
- `document_name` (e.g., "Bộ luật Lao động 2019")
- `heading1`, `heading2` (article/section titles)

### 3. **RRF Re-ranking**
Results from vector search and keyword search are combined using **Reciprocal Rank Fusion**:
- Documents appearing in both result sets rank higher
- Balances semantic similarity with keyword relevance

### 4. **Intelligent Fallback**
When tenant-specific documents are insufficient:
- System automatically searches global legal knowledge base
- Adds fallback notice to the LLM prompt
- Ensures users always get relevant responses

---

## 📥 Input: system_instruction Field

The `UserPromptReceivedMessage` already includes `system_instruction`:

```python
class UserPromptReceivedMessage(BaseModel):
    conversation_id: int
    message: str
    token: str
    timestamp: Optional[datetime]
    system_instruction: Optional[List[PromptConfigDto]]  # ← Used for hybrid search
```

**Example system_instruction:**
```json
{
  "systemInstruction": [
    {"key": "BHXH", "value": "Bảo hiểm xã hội"},
    {"key": "BHTN", "value": "Bảo hiểm thất nghiệp"},
    {"key": "OT", "value": "Overtime Payment"}
  ]
}
```

**How it's used:**
1. **Query Expansion**: Abbreviations are replaced with full terms before embedding
2. **Keyword Extraction**: Both keys and values are used as BM25 keywords

---

## 📤 Output: Updated Response

The `ChatResponse` now includes a `fallback_triggered` field:

```python
{
  "conversation_id": 123,
  "message": "Theo [Bộ luật Lao động - Điều 212]...",
  "timestamp": "2025-12-28T10:30:00Z",
  "model_used": "vistral",
  "rag_documents_used": 3,
  "source_ids": [101, 102, 103],
  "scenario": "BOTH",
  "fallback_triggered": false  # ← NEW: Indicates if fallback was used
}
```

---

## 🔄 How It Works

### Before Hybrid Search:
```
User Query → Embedding → Vector Search → Filter → Response
```

### After Hybrid Search:
```
User Query
    ↓
Query Expansion (system_instruction)
    ↓
Legal Term Extraction (Điều X, BHXH, etc.)
    ↓
Parallel Hybrid Search:
    ├─ Vector Search (semantic)
    └─ Keyword Search (exact terms)
    ↓
RRF Re-ranking (combine results)
    ↓
Fallback Logic:
    - IF tenant results < 2: Use global legal docs
    - ELSE: Balanced split (60% tenant, 40% global)
    ↓
Response Generation
```

---

## 🧪 Testing the Feature

### Test Case 1: Exact Legal Term Match

**Request:**
```json
{
  "conversation_id": 1,
  "message": "Điều 212 BHXH quy định gì?",
  "tenant_id": 5,
  "user_id": 10,
  "system_instruction": [
    {"key": "BHXH", "value": "Bảo hiểm xã hội"}
  ]
}
```

**Expected Behavior:**
1. Extracts keywords: `["điều 212", "BHXH", "bảo hiểm xã hội"]`
2. Searches for documents containing these exact terms
3. Returns response with exact article reference

### Test Case 2: Fallback Activation

**Setup:**
- Tenant 99 has no documents uploaded
- Query: "Quy định về nghỉ phép năm là gì?"

**Expected Behavior:**
1. Tenant search returns 0 results
2. Fallback triggered: searches global legal docs (tenant_id=1)
3. Response includes: `"fallback_triggered": true`
4. System prompt includes fallback notice

### Test Case 3: Hybrid Improves Ranking

**Query:** "Công ty có phải đóng BHTN cho nhân viên thử việc?"

**Comparison:**
- **Vector-only**: May rank general labor law docs highest
- **Hybrid**: Boosts docs containing "BHTN" (exact match) to top

---

## 📊 Monitoring & Metrics

### Key Metrics to Track:

1. **Fallback Rate**
   ```python
   fallback_rate = (requests_with_fallback / total_requests) * 100
   ```
   - **Target**: < 20% (means most tenants have sufficient docs)

2. **Keyword Extraction Rate**
   ```python
   keyword_extraction_rate = (requests_with_keywords / total_requests) * 100
   ```
   - **Target**: > 60% (means most queries contain legal terms)

3. **Average Latency**
   - **Baseline (vector-only)**: ~500ms
   - **Target (hybrid)**: < 550ms (+10% acceptable)

4. **User Satisfaction** (if available)
   - Track thumbs up/down on responses
   - Compare hybrid vs. vector-only

### Logging Examples:

**Successful Hybrid Search:**
```
[INFO] Extracted 3 legal keywords: ['điều 212', 'BHXH', 'bảo hiểm xã hội']
[INFO] Hybrid search for tenant 5: 2 vector + 3 keyword → 4 fused
[INFO] Normal hybrid search for tenant 5: 3 tenant + 2 global results
[INFO] Applied COMPARISON system prompt (fallback: False)
```

**Fallback Triggered:**
```
[INFO] Extracted 2 legal keywords: ['nghỉ phép', 'năm 2020']
[INFO] Hybrid search for tenant 99: 0 vector + 0 keyword → 0 fused
[WARNING] Fallback triggered: Only 0 quality tenant results (threshold: 2)
[WARNING] Fallback activated for tenant 99: 0 tenant + 5 global results
[INFO] Applied SINGLE SOURCE system prompt for LEGAL_ONLY (fallback: True)
```

---

## 🔧 Configuration (Optional)

### Environment Variables:

Add to `.env` file:

```bash
# Hybrid Search Settings
HYBRID_SEARCH_ENABLED=true
RRF_K=60
MIN_TENANT_RESULTS_THRESHOLD=2
KEYWORD_SCORE_THRESHOLD=0.6
FALLBACK_SIMILARITY_THRESHOLD=0.65
```

### Feature Flag:

To gradually roll out or A/B test:

```python
# In config.py
HYBRID_SEARCH_ENABLED = os.getenv('HYBRID_SEARCH_ENABLED', 'true').lower() == 'true'

# In business.py
if settings.HYBRID_SEARCH_ENABLED:
    # Use hybrid search
else:
    # Use legacy vector-only search
```

---

## 🐛 Troubleshooting

### Issue: No keywords extracted

**Symptoms:**
```
[DEBUG] No legal keywords extracted from query
```

**Possible Causes:**
1. Query doesn't contain legal terms
2. Query is in English (patterns are Vietnamese-focused)

**Solution:**
- Check query content
- Verify `system_instruction` contains relevant abbreviations

### Issue: Fallback always triggered

**Symptoms:**
```
[WARNING] Fallback triggered: Only 0 quality tenant results
```

**Possible Causes:**
1. Tenant has no documents uploaded
2. Documents don't match the query
3. Similarity threshold too high (0.7)

**Solution:**
1. Upload tenant documents to Qdrant
2. Verify documents are indexed correctly
3. Lower threshold to 0.6 for testing

### Issue: High latency

**Symptoms:**
- Response time > 1 second

**Possible Causes:**
1. Qdrant overloaded
2. Too many keyword filters (> 20)
3. Large result sets

**Solution:**
1. Scale Qdrant horizontally
2. Limit keywords to top 10
3. Reduce search limit (e.g., 5 → 3)

---

## 📚 Additional Resources

- **Full Documentation**: [hybrid_search_architecture.md](./hybrid_search_architecture.md)
- **Implementation Summary**: [hybrid_search_implementation_summary.md](./hybrid_search_implementation_summary.md)
- **Unit Tests**: [../tests/test_hybrid_search.py](../tests/test_hybrid_search.py)

---

## ✅ Checklist for Production

- [ ] Qdrant payload fields indexed: `text`, `document_name`, `heading1`, `heading2`
- [ ] All unit tests passing
- [ ] Integration tests validated
- [ ] Logging enabled for keyword extraction
- [ ] Fallback rate monitored (< 20%)
- [ ] Latency benchmarked (< 550ms)
- [ ] User feedback mechanism in place
- [ ] A/B test plan prepared (optional)

---

**Last Updated**: 2025-12-28
**Status**: Production-Ready ✅
