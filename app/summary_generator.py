from typing import List
import openai
from .models import Post, PostSummary

class SummaryGenerator:
    def __init__(self, api_key: str):
        self.api_key = api_key
        openai.api_key = api_key

    def generate_summary(self, post: Post) -> PostSummary:
        """Generate a summary for a given post using OpenAI API."""
        try:
            # Prepare prompt
            prompt = f"""
            Please analyze and summarize the following Telegram post maintaining the key information:

            Title: {post.title}
            Content: {post.content}

            Provide a concise summary and rate its usefulness on a scale of 0-10.
            Format: Summary text followed by usefulness rating on new line.
            """

            # Get response from OpenAI
            response = openai.ChatCompletion.create(
                model="gpt-4",
                messages=[
                    {"role": "system", "content": "You are a helpful assistant that summarizes Telegram posts and rates their usefulness."},
                    {"role": "user", "content": prompt}
                ],
                max_tokens=500,
                temperature=0.7
            )

            # Parse response
            response_text = response.choices[0].message.content
            summary_parts = response_text.strip().split('\n')

            summary = summary_parts[0].strip()
            usefulness = 5  # Default value

            # Try to extract usefulness rating from the response
            for part in summary_parts:
                if part.strip().isdigit():
                    usefulness = int(part.strip())
                    break

            # Create and return PostSummary
            return PostSummary(
                summary=summary,
                usefulness=min(max(usefulness, 0), 10),  # Ensure within 0-10 range
                post_id=post.id
            )

        except Exception as e:
            raise Exception(f"Failed to generate summary: {str(e)}")

    def _evaluate_importance(self, content: str) -> int:
        """Evaluate the importance of content on a scale of 0-10."""
        try:
            prompt = f"""
            On a scale of 0-10, rate the importance and relevance of this content:
            {content}

            Provide only the numeric rating.
            """

            response = openai.ChatCompletion.create(
                model="gpt-4",
                messages=[
                    {"role": "system", "content": "You are a content evaluator that rates content importance on a scale of 0-10."},
                    {"role": "user", "content": prompt}
                ],
                max_tokens=10,
                temperature=0.3
            )

            rating = int(response.choices[0].message.content.strip())
            return min(max(rating, 0), 10)  # Ensure within 0-10 range

        except Exception as e:
            raise Exception(f"Failed to evaluate importance: {str(e)}")
