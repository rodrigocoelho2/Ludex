# Profile redesign mocks

The design the customizable profile was built from, made in Claude Design (Nov 2026).

| File | What it is |
| --- | --- |
| `Main.dc.html` | The profile: banner, photo with frame and level, title, XP bar, Playing now, the showcases (favourite game with a quote, in numbers, rarest achievements, year in games, top 5) and the side column (about and tags, badges, pinned collection, friends). |
| `Customize.dc.html` | Customize profile: live preview, colour, banner, frame and title, about and tags, showcases with drag handles (6 at most) and who sees what. |
| `canvas.json` | How the two artboards sit on the canvas. |

They're Claude Design artboards (templated HTML with `{{…}}` values and `sc-if` / `sc-for` blocks), so they open in
Claude Design rather than straight in a browser. The working pages are `Ludex.Website/Views/Profile/Index.cshtml` and
`Edit.cshtml`.
