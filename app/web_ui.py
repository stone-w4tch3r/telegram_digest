import traceback

import httpx
from fastapi import APIRouter, Form, Request, Response
from fastapi.responses import HTMLResponse, RedirectResponse
from fastapi.templating import Jinja2Templates

# Create router instead of app
router = APIRouter()

# Setup templates
templates = Jinja2Templates(directory="templates")


def _pretty_exception(e: Exception) -> str:
    """Pretty print an exception with pretty traceback."""
    return f"{e.__class__.__name__}: {e}\n{traceback.format_exc()}"


def _render_error(request: Request, errors: list[str]) -> Response:
    """Render an error page with the given error messages."""
    return templates.TemplateResponse(
        "base.html",
        {
            "request": request,
            "messages": [{"type": "error", "text": error} for error in errors],
        },
    )


def _render_error_from_exception(request: Request, e: Exception) -> Response:
    """Render an error page from an exception."""
    return _render_error(request, [f"An error occurred: {_pretty_exception(e)}"])


@router.get("/", response_class=HTMLResponse)
async def home(request: Request) -> Response:
    """Render the home page."""

    try:
        async with httpx.AsyncClient() as client:
            response = await client.get(f"{request.base_url}api/channels")
            channels = response.json()
    except Exception as e:
        return _render_error_from_exception(request, e)˝

    return templates.TemplateResponse(
        "channels.html",
        {"request": request, "channels": channels, "active_page": "channels"},
    )


@router.get("/settings", response_class=HTMLResponse)
async def settings_page(request: Request) -> Response:
    """Render the settings page."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{request.base_url}api/settings")

        if response.status_code != 200:
            error_data = response.json()
            return templates.TemplateResponse(
                "settings.html",
                {
                    "request": request,
                    "settings": None,
                    "messages": error_data.get("detail", "Unknown error occurred"),
                    "active_page": "settings",
                },
            )

        settings = response.json()

    return templates.TemplateResponse(
        "settings.html",
        {
            "request": request,
            "settings": settings,
            "error": None,
            "active_page": "settings",
        },
    )


@router.get("/history", response_class=HTMLResponse)
async def digest_history(request: Request) -> Response:
    """Render the digest history page."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{request.base_url}api/digests")
        digests = response.json()

    return templates.TemplateResponse(
        "digest_history.html",
        {"request": request, "digests": digests, "active_page": "history"},
    )


@router.get("/digest/{digest_id}", response_class=HTMLResponse)
async def digest_page(request: Request, digest_id: str) -> Response:
    """Render a specific digest page."""
    async with httpx.AsyncClient() as client:
        response = await client.get(f"{request.base_url}api/digests/{digest_id}")
        if response.status_code == 404:
            return templates.TemplateResponse(
                "error.html", {"request": request, "error_message": "Digest not found"}
            )
        digest = response.json()

    return templates.TemplateResponse(
        "digest_page.html",
        {"request": request, "digest": digest, "active_page": "digest"},
    )


@router.post("/channels/add")
async def add_channel_form(
    request: Request, channel_name: str = Form(...), channel_url: str = Form(...)
) -> Response:
    """Handle channel addition form submission."""
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{request.base_url}api/channels",
            json={"name": channel_name, "url": channel_url},
        )

        if response.status_code != 200:
            return templates.TemplateResponse(
                "error.html",
                {"request": request, "error_message": "Failed to add channel"},
            )

    return RedirectResponse(url="/", status_code=303)


@router.post("/settings/update")
async def update_settings_form(
    request: Request,
    openai_api_key: str = Form(...),
    email_from: str = Form(...),
    email_to: str = Form(...),
    email_password: str = Form(...),
    email_server: str = Form(...),
    email_port: int = Form(...),
    digest_schedule_hour: int = Form(...),
    digest_schedule_minute: int = Form(...),
) -> Response:
    """Handle settings update form submission."""
    async with httpx.AsyncClient() as client:
        response = await client.post(
            f"{request.base_url}api/settings",
            json={
                "openai_api_key": openai_api_key,
                "email_from": email_from,
                "email_to": email_to,
                "email_password": email_password,
                "email_server": email_server,
                "email_port": email_port,
                "digest_schedule_hour": digest_schedule_hour,
                "digest_schedule_minute": digest_schedule_minute,
            },
        )

        if response.status_code != 200:
            return templates.TemplateResponse(
                "error.html",
                {"request": request, "error_message": "Failed to update settings"},
            )

    return RedirectResponse(url="/settings", status_code=303)
