from datetime import datetime

from pydantic import BaseModel


class Post(BaseModel):
    title: str
    link: str
    description: str
    pub_date: datetime
    summary: str = None  # Will be populated later
