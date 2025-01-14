import aiocron

from .database import SessionLocal, Summary
from .emailer import send_email
from .rss_fetcher import fetch_posts
from .summarizer import summarize_posts

CHANNEL_URLS = [
    "https://rsshub.app/telegram/channel/lovedeathtransformers",
    # Add more channel RSS URLs here
]


@aiocron.crontab("0 8 * * *")  # Every day at 08:00 AM server time
async def daily_digest():
    db = SessionLocal()
    try:
        # Fetch posts
        posts = await fetch_posts(CHANNEL_URLS)

        # Filter new posts
        existing_links = set(link for link, in db.query(Summary.link).all())
        new_posts = [post for post in posts if post.link not in existing_links]

        if not new_posts:
            print("No new posts to summarize.")
            return

        # Summarize posts
        summarized_posts = await summarize_posts(new_posts)

        # Save summaries to DB
        summaries = []
        for post in summarized_posts:
            summary = Summary(
                title=post.title,
                link=post.link,
                summary=post.summary,
                pub_date=post.pub_date,
            )
            db.add(summary)
            summaries.append(summary)
        db.commit()

        # Send email
        await send_email(summaries)

        print(f"Sent digest with {len(summaries)} summaries.")

    except Exception as e:
        print(f"Error in daily_digest: {e}")
    finally:
        db.close()
