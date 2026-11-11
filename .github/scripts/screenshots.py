"""Fills a fresh Ludex (SQL Server, built-in catalog) with three friends and their games, then screenshots every
screen on a computer and on a phone.

Usage: python screenshots.py <base-url> <output-folder> <setup-key>

It creates the admin Rodas (with the setup key) and two friends, Tiago and Ines, who send Rodas friend requests that
Rodas accepts on the Friends page. Everyone adds games through the same actions the pages use. Fails (exit 1) when a
page throws a JavaScript error, logs one to the console, answers 5xx, shows "engine isn't answering", or names two
covers the same for the view transitions.
"""
import json
import pathlib
import sys
import time

from playwright.sync_api import sync_playwright

base = sys.argv[1].rstrip("/")
out = pathlib.Path(sys.argv[2])
setup_key = sys.argv[3]
out.mkdir(parents=True, exist_ok=True)
errors = []

FIRST = 1_000_000_000          # the built-in catalog's ids: FIRST + position in the list (1-based)
G = {name: FIRST + i for i, name in enumerate([
    "Elden Ring", "Silksong", "Hollow Knight", "Hades II", "Hades", "Balatro", "Baldur's Gate 3", "Expedition 33",
    "Pokemon Z-A", "Tears of the Kingdom", "Breath of the Wild", "Echoes of Wisdom", "Ocarina of Time", "Cyberpunk 2077",
    "Red Dead Redemption 2", "The Witcher 3", "Celeste", "Stardew Valley", "Persona 5 Royal", "Outer Wilds",
    "Disco Elysium", "Portal 2", "Sekiro", "Hi-Fi Rush", "Monster Hunter Wilds", "Split Fiction", "Minecraft",
    "Dune Imperium", "Catan", "Mario Kart World", "Super Mario Odyssey", "God of War Ragnarok", "Final Fantasy VII Rebirth",
    "Astro Bot", "Pokemon Scarlet"], start=1)}

PASSWORD = "ludex-ci-2026"


def watch(page, label):
    page.on("pageerror", lambda e: errors.append(f"{label}: page error: {e}"))
    page.on("console", lambda m: errors.append(f"{label}: console {m.type}: {m.text}") if m.type == "error" and "favicon" not in m.text else None)
    page.on("response", lambda r: errors.append(f"{label}: {r.status} {r.url}") if r.status >= 500 else None)


def check_page(page, label):
    text = page.content()
    if "engine isn't answering" in text or "isn&#x27;t answering" in text:
        errors.append(f"{label}: the page says the engine isn't answering")
    duplicates = page.evaluate("""() => {
        const seen = {}; const dup = [];
        document.querySelectorAll('*').forEach(el => {
            const name = getComputedStyle(el).viewTransitionName;
            if (name && name !== 'none') { if (seen[name]) dup.push(name); seen[name] = true; }
        });
        return dup;
    }""")
    if duplicates:
        errors.append(f"{label}: duplicate view-transition names {duplicates}")


def create_account(page, user, first=False):
    page.goto(base + "/create-account")
    if first:
        page.fill("input[name=setupKey]", setup_key)
    page.fill("input[name=userName]", user)
    page.fill("input[name=password]", PASSWORD)
    page.fill("input[name=confirmPassword]", PASSWORD)
    page.click("button[type=submit]")
    page.wait_for_load_state("networkidle")
    assert "/discover" in page.url, f"{user}: after create-account the page is {page.url}\n{page.content()[:1500]}"
    print(f"{user}: account created")


def save(page, game, status, rating=None, hours=None, platform=None, notes=None):
    """Saves a game the way the game page does (the Website's JSON action, with the page's anti-forgery token)."""
    result = page.evaluate("""async ([id, body]) => {
        const token = document.querySelector('meta[name="request-verification-token"]').content;
        const r = await fetch(`/games/${id}/save`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token }, body: JSON.stringify(body) });
        return { status: r.status, body: await r.json() };
    }""", [G[game], {"status": status, "rating": rating, "hours": hours, "platform": platform, "notes": notes}])
    if result["status"] != 200 or not result["body"].get("isValid"):
        errors.append(f"save {game}: {json.dumps(result)[:400]}")


def request_friend(page, name):
    page.goto(base + "/friends")
    page.fill("input[name=userName]", name)
    page.click("form.add-friend button[type=submit]")
    page.wait_for_load_state("networkidle")
    assert "Request sent" in page.content(), page.content()[:1500]


def shoot(page, name, path, wait_for=None):
    page.goto(base + path)
    page.wait_for_load_state("networkidle")
    if wait_for:
        page.wait_for_selector(wait_for)
    page.wait_for_timeout(1300)          # let the arrival animations and the count-up finish
    check_page(page, name)
    page.screenshot(path=str(out / f"{name}.png"), full_page=True)


with sync_playwright() as p:
    browser = p.chromium.launch()
    desktop = {"viewport": {"width": 1440, "height": 900}, "device_scale_factor": 1}
    phone = {"viewport": {"width": 390, "height": 844}, "device_scale_factor": 2, "is_mobile": True, "has_touch": True}

    # The sign-in pages, signed out (the first visit goes to "Set up Ludex").
    for label, options in (("desktop", desktop), ("phone", phone)):
        context = browser.new_context(**options)
        page = context.new_page()
        watch(page, f"{label} first visit")
        page.goto(base + "/")
        page.wait_for_load_state("networkidle")
        page.wait_for_timeout(900)
        assert "/create-account" in page.url, page.url
        page.screenshot(path=str(out / f"{label}-setup.png"))
        context.close()

    # Rodas, the admin.
    rodas_context = browser.new_context(**desktop)
    rodas = rodas_context.new_page()
    watch(rodas, "rodas")
    create_account(rodas, "Rodas", first=True)

    # Two friends: accounts, games, and a request each to Rodas.
    for user, games in (
        ("Tiago", [("Silksong", "Playing", None, 22, "PC"), ("Elden Ring", "Playing", None, 18, "PS5"), ("Monster Hunter Wilds", "Finished", 7, 60, "PC"),
                   ("Split Fiction", "Finished", 9, 14, "PS5"), ("Red Dead Redemption 2", "Finished", 10, 95, "PC")]),
        ("Ines", [("Expedition 33", "Finished", 10, 45, "PS5"), ("Silksong", "Finished", 9, 41, "Switch"), ("Baldur's Gate 3", "Finished", 9, 130, "PC"),
                  ("Outer Wilds", "Finished", 10, 24, "PC"), ("Monster Hunter Wilds", "Dropped", 6, 9, "PC"), ("Elden Ring", "Finished", 10, 120, "PS5")]),
    ):
        context = browser.new_context(**desktop)
        page = context.new_page()
        watch(page, user)
        create_account(page, user)
        for game, status, rating, hours, platform in games:
            save(page, game, status, rating, hours, platform)
        request_friend(page, "Rodas")
        context.close()

    # Rodas accepts both requests on the Friends page.
    rodas.goto(base + "/friends")
    for _ in range(2):
        rodas.click("form[action='/friends/accept'] button")
        rodas.wait_for_load_state("networkidle")
    assert rodas.locator("form[action='/friends/accept']").count() == 0, "requests still waiting"
    print("Rodas: accepted Tiago and Ines")

    # Rodas's library (the last one saved is the hero: Silksong).
    for game, status, rating, hours, platform, notes in (
        ("Pokemon Z-A", "Finished", 8, 34, "Switch 2", None),
        ("Baldur's Gate 3", "Finished", 10, 118, "PC", "Karlach forever."),
        ("Elden Ring", "Finished", 10, 142, "PS5", None),
        ("Stardew Valley", "Dropped", 7, 27, "Switch", None),
        ("Expedition 33", "Wishlist", None, None, None, None),
        ("Dune Imperium", "Playing", None, 12, "PC", None),
        ("Balatro", "Playing", 9, 41, "Switch", None),
        ("Hades II", "Playing", 9, 22, "Switch", None),
        ("Silksong", "Playing", 9, 38, "Switch", "The citadel is huge."),
    ):
        save(rodas, game, status, rating, hours, platform, notes)
    print("Rodas: library filled")

    # A one-tap add from the search, like a person would.
    rodas.goto(base + "/discover?q=zelda")
    rodas.wait_for_selector(".result")
    rodas.click(f".result[data-game='{G['Breath of the Wild']}'] button[data-status=Finished]")
    rodas.wait_for_selector(f".result[data-game='{G['Breath of the Wild']}'] .in-lib")
    print("Rodas: added Breath of the Wild from the search")

    screens = [
        ("home", "/", None),
        ("library", "/library", None),
        ("library-finished", "/library?status=finished", None),
        ("discover", "/discover?q=zelda", ".result"),
        ("game", f"/games/{G['Silksong']}", None),
        ("game-new", f"/games/{G['Outer Wilds']}", None),
        ("friends", "/friends", None),
        ("settings", "/settings", None),
    ]
    for name, path, wait_for in screens:
        shoot(rodas, f"desktop-{name}", path, wait_for)

    cookies = rodas_context.cookies()
    phone_context = browser.new_context(**phone)
    phone_context.add_cookies(cookies)
    mobile = phone_context.new_page()
    watch(mobile, "phone")
    for name, path, wait_for in screens:
        shoot(mobile, f"phone-{name}", path, wait_for)

    # The live search with its skeletons, caught mid-flight on the phone.
    # (The results are held back a moment, or they'd replace the skeletons before the picture is taken.)
    mobile.route("**/discover/results**", lambda route: (time.sleep(1.5), route.continue_()))
    mobile.goto(base + "/discover")
    mobile.fill("#q", "hades")
    mobile.wait_for_selector(".skel-row", timeout=5000)
    mobile.screenshot(path=str(out / "phone-discover-skeleton.png"))
    mobile.wait_for_selector(".result", timeout=10000)
    mobile.unroute("**/discover/results**")

    # Signed-out sign-in page, now with covers on the wall.
    for label, options in (("desktop", desktop), ("phone", phone)):
        context = browser.new_context(**options)
        page = context.new_page()
        watch(page, f"{label} sign-in")
        page.goto(base + "/sign-in")
        page.wait_for_load_state("networkidle")
        page.wait_for_timeout(900)
        page.screenshot(path=str(out / f"{label}-sign-in.png"))
        context.close()

    browser.close()

if errors:
    print("\n".join(errors))
    sys.exit(1)
print("all screens ok")
