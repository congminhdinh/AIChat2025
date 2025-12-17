import asyncio
import json
from datetime import datetime
from app.models import UserPromptReceivedMessage, BotResponseCreatedMessage
from app.services import OllamaService

async def test_ollama_service():
    print('\n' + '=' * 60)
    print('Testing Ollama Service')
    print('=' * 60)
    ollama = OllamaService()
    print('\n1. Running health check...')
    healthy = await ollama.health_check()
    print(f"   Ollama health: {('✓ OK' if healthy else '✗ FAILED')}")
    if healthy:
        print('\n2. Testing response generation...')
        try:
            response = await ollama.generate_response('Say hello in one sentence.')
            print(f'   Response: {response[:100]}...')
            print('   ✓ Response generation successful')
        except Exception as e:
            print(f'   ✗ Failed: {e}')
    else:
        print('\n   Skipping response generation test (Ollama not available)')

async def test_message_models():
    print('\n' + '=' * 60)
    print('Testing Message Models')
    print('=' * 60)
    print('\n1. Testing UserPromptReceivedMessage...')
    prompt_msg = UserPromptReceivedMessage(conversation_id=123, message='What is Python?', user_id=456, timestamp=datetime.utcnow())
    print(f'   Created: {prompt_msg.model_dump_json(indent=2)}')
    print('   ✓ UserPromptReceivedMessage works')
    print('\n2. Testing BotResponseCreatedMessage...')
    response_msg = BotResponseCreatedMessage(conversation_id=123, message='Python is a programming language.', user_id=0, timestamp=datetime.utcnow(), model_used='llama2')
    print(f'   Created: {response_msg.model_dump_json(indent=2)}')
    print('   ✓ BotResponseCreatedMessage works')
    print('\n3. Testing JSON round-trip...')
    json_str = prompt_msg.model_dump_json()
    reconstructed = UserPromptReceivedMessage(**json.loads(json_str))
    assert reconstructed.conversation_id == prompt_msg.conversation_id
    print('   ✓ JSON serialization/deserialization works')

async def main():
    print('\n╔════════════════════════════════════════════════════════════╗')
    print('║        ChatProcessor Service Component Tests               ║')
    print('╚════════════════════════════════════════════════════════════╝')
    await test_message_models()
    await test_ollama_service()
    print('\n' + '=' * 60)
    print('Tests completed!')
    print('=' * 60)
    print('\nNote: RabbitMQ tests require running RabbitMQ server.')
    print('      Use the full service to test RabbitMQ integration.')
    print()
if __name__ == '__main__':
    asyncio.run(main())