from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import HTMLResponse
from fastapi.templating import Jinja2Templates
from sqlalchemy.orm import Session

from .database import SessionLocal, Summary, init_db
from .scheduler import daily_digest  # noqa: F401

app = FastAPI()
templates = Jinja2Templates(directory="app/templates")


@app.on_event("startup")
async def startup_event():
    init_db()
    # The scheduler starts automatically due to aiocron decorator


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


@app.get("/summary/{summary_id}", response_class=HTMLResponse)
async def get_summary(summary_id: int, request: Request, db: Session = Depends(get_db)):
    summary = db.query(Summary).filter(Summary.id == summary_id).first()
    if not summary:
        raise HTTPException(status_code=404, detail="Summary not found")
    return templates.TemplateResponse(
        "summary.html", {"request": request, "summary": summary}
    )


@app.get("/", response_class=HTMLResponse)
async def home(request: Request, db: Session = Depends(get_db)):
    summaries = db.query(Summary).order_by(Summary.pub_date.desc()).limit(10).all()
    return templates.TemplateResponse(
        "index.html", {"request": request, "summaries": summaries}
    )
