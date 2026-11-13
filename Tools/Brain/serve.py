"""Run from the project root: uv run --project Tools/Brain --locked python Tools/Brain/serve.py."""

import argparse

import uvicorn

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=8797)
    args = parser.parse_args()
    # Never expose the game's private observations on a network interface.
    uvicorn.run("nso_brain.api:app", host="127.0.0.1", port=args.port, workers=1)
