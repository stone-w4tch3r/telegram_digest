from datetime import datetime

from pydantic import BaseModel
from sqlalchemy import Column, DateTime, Integer, String, Text

from .database import Base


class Post(BaseModel):
    title: str
    link: str
    description: str
    pub_date: datetime
    summary: str = None  # Will be populated later


class Summary(Base):
    __tablename__ = "summaries"

    id = Column(Integer, primary_key=True, index=True)
    title = Column(String, index=True)
    link = Column(String, unique=True, index=True)
    summary = Column(Text)
    pub_date = Column(DateTime, default=datetime.utcnow)
