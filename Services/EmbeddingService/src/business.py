import torch
import uuid
from typing import List
from optimum.onnxruntime import ORTModelForFeatureExtraction
from transformers import AutoTokenizer
from qdrant_client import QdrantClient
from qdrant_client.models import Distance, VectorParams, PointStruct, Filter, FieldCondition, MatchValue

from src.config import settings

class EmbeddingService:
    def __init__(self):
        print(f"Loading model: {settings.model_name}...")
        try:
            self.tokenizer = AutoTokenizer.from_pretrained(settings.model_name)
            self.model = ORTModelForFeatureExtraction.from_pretrained(settings.model_name, export=True)
            print("Model loaded successfully with ONNX Runtime!")
        except Exception as e:
            print(f"Error loading model: {e}")
            raise

        print(f"Connecting to Qdrant at {settings.qdrant_host}:{settings.qdrant_port}...")
        try:
            self.qdrant_client = QdrantClient(host=settings.qdrant_host, port=settings.qdrant_port)
            print("Qdrant client initialized successfully!")
        except Exception as e:
            print(f"Error connecting to Qdrant: {e}")
            raise

    def mean_pooling(self, model_output, attention_mask):
        token_embeddings = model_output[0]
        input_mask_expanded = attention_mask.unsqueeze(-1).expand(token_embeddings.size()).float()
        return torch.sum(token_embeddings * input_mask_expanded, 1) / torch.clamp(input_mask_expanded.sum(1), min=1e-9)

    def encode_text(self, text: str) -> List[float]:
        encoded_input = self.tokenizer(text, padding=True, truncation=True, return_tensors='pt', max_length=512)
        model_output = self.model(**encoded_input)
        sentence_embeddings = self.mean_pooling(model_output, encoded_input['attention_mask'])
        sentence_embeddings = torch.nn.functional.normalize(sentence_embeddings, p=2, dim=1)
        return sentence_embeddings[0].tolist()

    def ensure_collection(self, collection_name: str, vector_size: int):
        collections = self.qdrant_client.get_collections().collections
        collection_names = [c.name for c in collections]

        if collection_name not in collection_names:
            self.qdrant_client.create_collection(
                collection_name=collection_name,
                vectors_config=VectorParams(size=vector_size, distance=Distance.COSINE)
            )

    def create_embedding(self, text: str) -> List[float]:
        if not text:
            raise ValueError("Text cannot be empty")
        return self.encode_text(text)

    def vectorize_and_store(self, text: str, metadata: dict, collection_name: str = None):
        if not text:
            raise ValueError("Text cannot be empty")

        collection_name = collection_name or settings.qdrant_collection
        embedding = self.encode_text(text)
        self.ensure_collection(collection_name, len(embedding))
        point_id = str(uuid.uuid4())

        self.qdrant_client.upsert(
            collection_name=collection_name,
            points=[
                PointStruct(
                    id=point_id,
                    vector=embedding,
                    payload={"text": text, **metadata}
                )
            ]
        )

        return point_id, len(embedding), collection_name

    def vectorize_batch(self, items: list, collection_name: str = None):
        if not items:
            raise ValueError("Items list cannot be empty")

        collection_name = collection_name or settings.qdrant_collection
        points = []

        for item in items:
            if not item.text:
                continue
            embedding = self.encode_text(item.text)
            if not points:
                self.ensure_collection(collection_name, len(embedding))
            point_id = str(uuid.uuid4())

            points.append(
                PointStruct(
                    id=point_id,
                    vector=embedding,
                    payload={"text": item.text, **item.metadata}
                )
            )

        if points:
            self.qdrant_client.upsert(collection_name=collection_name, points=points)

        return len(points), collection_name

    def delete_by_filter(self, source_id: str, tenant_id: int, type: int, collection_name: str = None):
        collection_name = collection_name or settings.qdrant_collection

        delete_filter = Filter(
            must=[
                FieldCondition(key="source_id", match=MatchValue(value=source_id)),
                FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id)),
                FieldCondition(key="type", match=MatchValue(value=type))
            ]
        )

        self.qdrant_client.delete(collection_name=collection_name, points_selector=delete_filter)
        return collection_name
