from fastapi import FastAPI, HTTPException
import uvicorn
from pydantic import BaseModel
from pydantic_ai import Agent
from pydantic_ai.models.openai import OpenAIModel
from pydantic_ai.providers.openai import OpenAIProvider
import os
from openai import AsyncAzureOpenAI
from azure.identity import (
    DefaultAzureCredential,
    get_bearer_token_provider
)

# Define Pydantic models
class Attraction(BaseModel):
    name: str
    description: str
    address: str
    rating: float
    operating_hours: str

class CityAttractions(BaseModel):
    city: str
    attractions: list[Attraction]

# Initialize FastAPI app
app = FastAPI()

# Root endpoint
@app.get("/")
async def root():
    return {"message": "FastAPI is running"}

# Initialize the Agent
azure_credential = DefaultAzureCredential()
token_provider = get_bearer_token_provider(azure_credential, "https://cognitiveservices.azure.com/.default")

client = AsyncAzureOpenAI(
    azure_endpoint=os.environ.get("AZURE_OPENAI_ENDPOINT"),
    azure_ad_token_provider=token_provider,
    api_version="2024-06-01"
)

model_name = os.environ.get("MODEL_NAME", "gpt-4o-mini")
model = OpenAIModel(model_name, provider=OpenAIProvider(openai_client=client))
agent = Agent(model, 
              output_type=CityAttractions,
              system_prompt="You are an expert local guide. Provide detailed information about attractions in the specified city.")

# Endpoint to get attractions for a city
@app.post("/attractions")
async def get_attractions(query: str):
    try:
        # Use the agent asynchronously to fetch attractions
        structured_result = await agent.run(f"{query}")

        # Use the model to provide a more user-friendly response
        result = await agent.run(f"""Please provide a detailed list of attractions in {structured_result.output.city} with the 
                                 following details: 
                                 {structured_result.output.attractions}""", 
                                 output_type=str)
        
        return result.output
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
    
# start app
if __name__ == "__main__":
    port = int(os.environ.get('PORT', 8000))
    uvicorn.run(app, host="0.0.0.0", port=port)