from datetime import datetime
from typing import List, Optional
from uuid import UUID, uuid4

from pydantic import BaseModel, EmailStr, Field, HttpUrl, validator


class Channel(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    name: str
    url: HttpUrl

    @validator("name")
    def name_must_be_valid(cls, v: str) -> str:
        if not v or len(v) < 3:
            raise ValueError("Channel name must be at least 3 characters")
        return v


class Post(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    url: HttpUrl
    title: str
    content: str
    published_date: datetime
    channel_id: UUID


class PostSummary(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    summary: str
    usefulness: int
    post_id: UUID

    @validator("usefulness")
    def usefulness_range(cls, v: int) -> int:
        if not 0 <= v <= 10:
            raise ValueError("Usefulness must be between 0 and 10")
        return v


class Digest(BaseModel):
    id: UUID = Field(default_factory=uuid4)
    created_date: datetime = Field(default_factory=datetime.utcnow)
    summaries: List[PostSummary]
    channel_id: UUID


class Settings(BaseModel):
    openai_api_key: str
    email_from: EmailStr
    email_to: EmailStr
    email_password: str
    email_server: str
    email_port: int = Field(ge=1, le=65535)
    digest_schedule_hour: int = Field(ge=0, le=23, default=8)
    digest_schedule_minute: int = Field(ge=0, le=59, default=0)


class ChannelMetadata(BaseModel):
    id: UUID
    img_url: Optional[HttpUrl]
    description: Optional[str]


class DigestPreview(BaseModel):
    id: UUID
    created_date: datetime
    channel_name: str
    summary_count: int


class APIResponse(BaseModel):
    success: bool
    message: str
    data: Optional[dict] = None
