from datetime import datetime, timedelta
from typing import List
from uuid import UUID

from fastapi import APIRouter, Depends, HTTPException
from pydantic import ValidationError

from .channels_repository import ChannelsRepository
from .digest_service import DigestService
from .digests_repository import DigestsRepository
from .models import APIResponse, Channel, Digest, DigestPreview, Settings
from .settings import SettingsManager

router = APIRouter()


# Dependencies
def get_channels_repo() -> ChannelsRepository:
    return ChannelsRepository()


def get_digests_repo() -> DigestsRepository:
    return DigestsRepository()


def get_settings_manager() -> SettingsManager:
    return SettingsManager()


def get_digest_service() -> DigestService:
    return DigestService()


@router.get("/channels", response_model=List[Channel])
async def get_channels(
    repo: ChannelsRepository = Depends(get_channels_repo),
) -> List[Channel]:
    return repo.get_channels()


@router.post("/channels", response_model=APIResponse)
async def add_channel(
    channel: Channel, repo: ChannelsRepository = Depends(get_channels_repo)
) -> APIResponse:
    try:
        repo.add_channel(channel)
        return APIResponse(
            success=True,
            message="Channel added successfully",
            data={"channel_id": str(channel.id)},
        )
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))


@router.delete("/channels/{channel_id}", response_model=APIResponse)
async def remove_channel(
    channel_id: UUID, repo: ChannelsRepository = Depends(get_channels_repo)
) -> APIResponse:
    try:
        repo.remove_channel(channel_id)
        return APIResponse(success=True, message="Channel removed successfully")
    except Exception as e:
        raise HTTPException(status_code=404, detail=str(e))


@router.get("/settings", response_model=Settings)
async def get_settings_endpoint(
    settings_manager: SettingsManager = Depends(get_settings_manager),
) -> Settings:
    try:
        return settings_manager.load_settings()
    except FileNotFoundError:
        raise HTTPException(
            status_code=404,
            detail="Settings not found. Please initialize settings first.",
        )
    except ValidationError as e:
        raise HTTPException(
            status_code=400, detail=f"Invalid settings format: {str(e)}"
        )
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to load settings: {str(e)}"
        )


@router.post("/settings", response_model=APIResponse)
async def update_settings(
    settings: Settings,
    settings_manager: SettingsManager = Depends(get_settings_manager),
) -> APIResponse:
    try:
        settings_manager.save_settings(settings)
        return APIResponse(success=True, message="Settings updated successfully")
    except ValidationError as e:
        raise HTTPException(
            status_code=400, detail=f"Invalid settings format: {str(e)}"
        )
    except PermissionError as e:
        raise HTTPException(
            status_code=403, detail=f"Permission denied when saving settings: {str(e)}"
        )
    except Exception as e:
        raise HTTPException(
            status_code=500, detail=f"Failed to save settings: {str(e)}"
        )


@router.get("/digests", response_model=List[DigestPreview])
async def get_digest_history(
    days: int = 7, repo: DigestsRepository = Depends(get_digests_repo)
) -> List[Digest]:
    from_date = datetime.utcnow() - timedelta(days=days)
    return repo.get_digests(from_date, datetime.utcnow())


@router.get("/digests/{digest_id}", response_model=Digest)
async def get_digest(
    digest_id: UUID, repo: DigestsRepository = Depends(get_digests_repo)
) -> Digest:
    digest = repo.get_digest(digest_id)
    if not digest:
        raise HTTPException(status_code=404, detail="Digest not found")
    return digest


@router.post("/digests/generate", response_model=Digest)
async def generate_digest(
    channel_id: UUID, settings: Settings = Depends(get_settings_manager)
) -> Digest:
    try:
        digest_generator = DigestService()
        digest = await digest_generator.generate_digest(channel_id, settings)
        return digest
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@router.post("/digests/{digest_id}/send", response_model=APIResponse)
async def send_digest(
    digest_id: UUID,
    settings_manager: SettingsManager = Depends(get_settings_manager),
    digest_service: DigestService = Depends(get_digest_service),
) -> APIResponse:
    try:
        digest_service.send_digest(digest_id, settings_manager.load_settings())
        return APIResponse(success=True, message="Digest sent successfully")
    except ValueError as e:
        raise HTTPException(status_code=404, detail=str(e))
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
