import asyncio
from typing import List

import feedparser

from .models import Post


async def fetch_rss(feed_url: str) -> feedparser.FeedParserDict:
    loop = asyncio.get_event_loop()
    try:
        feed = await loop.run_in_executor(None, feedparser.parse, feed_url)
        if feed.bozo:
            # Handle parse errors
            print(f"Error parsing feed {feed_url}: {feed.bozo_exception}")
            return None
        return feed
    except Exception as e:
        print(f"Error fetching feed {feed_url}: {e}")
        return None


async def fetch_posts(channel_urls: List[str]) -> List[Post]:
    tasks = [fetch_rss(url) for url in channel_urls]
    feeds = await asyncio.gather(*tasks)
    posts = []
    for feed in feeds:
        if not feed:
            continue
        for entry in feed.entries:
            post = Post(
                title=entry.title,
                link=entry.link,
                description=entry.description,
                pub_date=entry.published_parsed,
            )
            posts.append(post)
    return posts
