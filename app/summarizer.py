import asyncio
import os
from typing import List

import openai

from .models import Post

openai.api_key = os.getenv("OPENAI_API_KEY")


async def summarize_text(text: str) -> str:
    response = await openai.ChatCompletion.acreate(
        model="gpt-4",
        messages=[
            {
                "role": "system",
                "content": "You are a helpful assistant that summarizes Telegram posts.",
            },
            {
                "role": "user",
                "content": f"Please summarize the following text:\n\n{text}",
            },
        ],
        max_tokens=150,
    )
    summary = response.choices[0].message["content"].strip()
    return summary


async def summarize_posts(posts: List[Post]) -> List[Post]:
    tasks = [summarize_text(post.description) for post in posts]
    summaries = await asyncio.gather(*tasks)
    for post, summary in zip(posts, summaries):
        post.summary = summary
    return posts
