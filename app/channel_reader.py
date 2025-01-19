import logging
from datetime import datetime
from typing import List
from urllib.parse import urljoin
from uuid import UUID

import feedparser
import httpx

from .models import Channel, Post

logger = logging.getLogger(__name__)


class ChannelReader:
    def __init__(self, rsshub_base_url: str = "https://rsshub.app"):
        self.rsshub_base_url = rsshub_base_url
        self.logger = logging.getLogger(__name__)

    async def get_channel_posts(self, channel_id: UUID, since: datetime) -> List[Post]:
        """
        Fetch posts from a Telegram channel via RSS feed.
        Returns posts newer than the specified datetime.
        """
        try:
            channel = self._get_channel(channel_id)
            feed_url = self._build_feed_url(channel.name)

            async with httpx.AsyncClient() as client:
                response = await client.get(feed_url)
                response.raise_for_status()

                feed = feedparser.parse(response.text)

                posts = []
                for entry in feed.entries:
                    published_date = self._parse_date(entry.published)

                    if published_date >= since:
                        post = Post(
                            url=entry.link,
                            title=entry.title,
                            content=entry.description,
                            published_date=published_date,
                            channel_id=channel_id,
                        )
                        posts.append(post)

                logger.info(f"Retrieved {len(posts)} posts from channel {channel.name}")
                return posts

        except Exception as e:
            logger.error(f"Failed to get posts for channel {channel_id}: {str(e)}")
            raise

    def _build_feed_url(self, channel_name: str) -> str:
        """Build RSS feed URL for a Telegram channel."""
        return urljoin(self.rsshub_base_url, f"/telegram/channel/{channel_name}")

    def _parse_date(self, date_str: str) -> datetime:
        """Parse date string from RSS feed."""
        try:
            return datetime.strptime(date_str, "%a, %d %b %Y %H:%M:%S %z")
        except ValueError:
            # Fallback to current date if parsing fails
            logger.warning(f"Failed to parse date: {date_str}")
            return datetime.utcnow()

    def _get_channel(self, channel_id: UUID) -> Channel:
        """Get channel from database."""
        from .channels_repository import ChannelsRepository

        channel = ChannelsRepository().get_channel(channel_id)
        if not channel:
            raise ValueError(f"Channel not found: {channel_id}")
        return channel
