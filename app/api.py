from fastapi import APIRouter, HTTPException, Depends
from fastapi.responses import JSONResponse
from typing import List
from datetime import datetime, timedelta
from uuid import UUID

from .models import (
    Channel, Settings, Digest, DigestPreview,
    APIResponse, Post, PostSummary
)
from .channels_repository import ChannelsRepository
from .digests_repository import DigestsRepository
from .settings import SettingsManager
from .summary_generator import SummaryGenerator
from .email_service import EmailSender
from .channel_reader import ChannelReader

router = APIRouter()

# Dependencies
def get_channels_repo():
    return ChannelsRepository()

def get_digests_repo():
    return DigestsRepository()

def get_settings():
    return SettingsManager().load_settings()

@router.get("/channels", response_model=List[Channel])
async def get_channels(repo: ChannelsRepository = Depends(get_channels_repo)):
    return repo.get_channels()

@router.post("/channels", response_model=APIResponse)
async def add_channel(
    channel: Channel,
    repo: ChannelsRepository = Depends(get_channels_repo)
):
    try:
        repo.add_channel(channel)
        return APIResponse(
            success=True,
            message="Channel added successfully",
            data={"channel_id": str(channel.id)}
        )
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@router.delete("/channels/{channel_id}", response_model=APIResponse)
async def remove_channel(
    channel_id: UUID,
    repo: ChannelsRepository = Depends(get_channels_repo)
):
    try:
        repo.remove_channel(channel_id)
        return APIResponse(
            success=True,
            message="Channel removed successfully"
        )
    except Exception as e:
        raise HTTPException(status_code=404, detail=str(e))

@router.get("/settings", response_model=Settings)
async def get_settings_endpoint(settings: Settings = Depends(get_settings)):
    return settings

@router.post("/settings", response_model=APIResponse)
async def update_settings(settings: Settings):
    try:
        SettingsManager().save_settings(settings)
        return APIResponse(
            success=True,
            message="Settings updated successfully"
        )
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))

@router.get("/digests", response_model=List[DigestPreview])
async def get_digest_history(
    days: int = 7,
    repo: DigestsRepository = Depends(get_digests_repo)
):
    from_date = datetime.utcnow() - timedelta(days=days)
    return repo.get_digests(from_date, datetime.utcnow())

@router.get("/digests/{digest_id}", response_model=Digest)
async def get_digest(
    digest_id: UUID,
    repo: DigestsRepository = Depends(get_digests_repo)
):
    digest = repo.get_digest(digest_id)
    if not digest:
        raise HTTPException(status_code=404, detail="Digest not found")
    return digest

@router.post("/digests/generate", response_model=Digest)
async def generate_digest(
    channel_id: UUID,
    settings: Settings = Depends(get_settings)
):
    try:
        channel_reader = ChannelReader()
        summary_generator = SummaryGenerator(settings.openai_api_key)

        # Get posts
        posts = channel_reader.get_channel_posts(channel_id, datetime.utcnow())

        # Generate summaries
        summaries = []
        for post in posts:
            summary = summary_generator.generate_summary(post)
            summaries.append(summary)

        # Create digest
        digest = Digest(
            channel_id=channel_id,
            summaries=summaries
        )

        # Save digest
        DigestsRepository().add_digest(digest)

        return digest
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@router.post("/digests/{digest_id}/send", response_model=APIResponse)
async def send_digest(
    digest_id: UUID,
    settings: Settings = Depends(get_settings)
):
    try:
        digest = DigestsRepository().get_digest(digest_id)
        if not digest:
            raise HTTPException(status_code=404, detail="Digest not found")

        email_sender = EmailSender()
        email_sender.send_digest(digest, settings)

        return APIResponse(
            success=True,
            message="Digest sent successfully"
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
