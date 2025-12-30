from fastapi import FastAPI
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer

app = FastAPI()
model = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")  # free, small, good

class EmbedReq(BaseModel):
    texts: list[str]

@app.post("/embed")
def embed(req: EmbedReq):
    vecs = model.encode(req.texts, normalize_embeddings=True).tolist()
    return {"vectors": vecs}
