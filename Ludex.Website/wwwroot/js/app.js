/* Ludex — the page scripts: covers that sharpen as they load, numbers that count up, one-tap adds, the live search with
   skeletons, the library filters (animated with view transitions), the game page's status and rating, and the
   screenshot viewer. Everything also works without it, a little less smoothly. */
'use strict';

(function () {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const token = document.querySelector('meta[name="request-verification-token"]')?.content || '';
    const $ = (selector, root) => (root || document).querySelector(selector);
    const $$ = (selector, root) => Array.from((root || document).querySelectorAll(selector));

    /* ---------- Blur-up: the sharp image fades in over the blurred one once it has loaded ---------- */
    const SHARP = 'img.cover-img, img.art-img';
    const loaded = img => img.classList.add('is-loaded');
    const failed = img => {
        // No image after all: the tint and the name underneath stay, which is how a game without a cover looks.
        const lq = img.previousElementSibling;
        if (lq && lq.classList.contains('cover-lq')) lq.remove();
        if (lq && lq.classList.contains('art-lq')) lq.remove();
        img.remove();
    };
    function sharpen(root) {
        $$(SHARP, root).forEach(img => {
            if (img.complete) { img.naturalWidth > 0 ? loaded(img) : failed(img); }
        });
    }
    // load and error don't bubble, but they can be caught on the way down: this covers images added later too.
    document.addEventListener('load', e => { if (e.target.matches && e.target.matches(SHARP)) loaded(e.target); }, true);
    document.addEventListener('error', e => {
        const t = e.target;
        if (!t.matches) return;
        if (t.matches(SHARP)) failed(t);
        else if (t.matches('img.cover-lq, img.art-lq, .feed-thumb img, .wall-tile img, .avatar img, .recent-art img, .thumb img')) t.remove();
    }, true);
    sharpen(document);

    /* ---------- Toast ---------- */
    let toastTimer;
    function toast(text, isError) {
        const el = $('.toast');
        if (!el) return;
        el.textContent = text;
        el.classList.toggle('err', !!isError);
        el.hidden = false;
        requestAnimationFrame(() => el.classList.add('show'));
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => { el.classList.remove('show'); setTimeout(() => { el.hidden = true; }, 300); }, 2600);
    }

    /* ---------- Calls to the Website's JSON actions ---------- */
    async function post(url, body) {
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify(body || {}),
                credentials: 'same-origin',
            });
            if (response.status === 401) { location.href = '/sign-in?returnUrl=' + encodeURIComponent(location.pathname + location.search); return null; }
            const result = await response.json();
            if (!result.isValid) {
                toast((result.validationErrors || []).map(e => e.message).join(' ') || 'That didn’t work.', true);
                return null;
            }
            return result.data;
        } catch {
            toast('Can’t reach Ludex right now.', true);
            return null;
        }
    }

    const statusWords = { Playing: 'Playing', Finished: 'Finished', Wishlist: 'Wishlist', Dropped: 'Dropped' };

    /* ---------- Count-up on the home tiles ---------- */
    function countUp() {
        const items = $$('[data-count]').filter(el => el.dataset.count !== '');
        if (!items.length || reduceMotion) return;
        const start = performance.now();
        const duration = 1100;
        const finals = items.map(el => ({ el, to: parseFloat(el.dataset.count), decimals: parseInt(el.dataset.decimals || '0', 10) }));
        function frame(now) {
            const p = Math.min(1, (now - start) / duration);
            const eased = 1 - Math.pow(1 - p, 3);
            finals.forEach(({ el, to, decimals }) => { el.textContent = (to * eased).toFixed(decimals); });
            if (p < 1) requestAnimationFrame(frame);
        }
        requestAnimationFrame(frame);
    }
    countUp();

    /* ---------- One-tap status buttons (search results, friends' favourites, the hero) ---------- */
    document.addEventListener('click', async e => {
        const button = e.target.closest('button[data-status][data-game]');
        if (!button || button.closest('[data-game-form]')) return;
        e.preventDefault();
        const game = button.dataset.game;
        const status = button.dataset.status;
        const group = button.closest('[data-acts]') || button.parentElement;
        $$('button', group).forEach(b => { b.disabled = true; });
        const entry = await post(`/games/${game}/status`, { status });
        $$('button', group).forEach(b => { b.disabled = false; });
        if (!entry) return;

        if (group.matches('[data-acts]')) {
            group.innerHTML = `<a class="in-lib pop-in" href="/games/${game}"><svg class="ic" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12.5l4.5 4.5L19 7.5"/></svg>In your library · ${statusWords[status] || status}</a>`;
        } else if (button.classList.contains('icon-btn')) {
            button.classList.add('on');
            button.disabled = true;
        }
        toast(status === 'Wishlist' ? 'On your wishlist' : `Added · ${statusWords[status] || status}`);
        if (button.closest('.hero')) setTimeout(() => location.reload(), 700);
    });

    /* ---------- Discover: search as you type, with skeletons meanwhile ---------- */
    const searchForm = $('[data-search]');
    const results = $('[data-results]');
    if (searchForm && results) {
        const input = $('input[name=q]', searchForm);
        const skeleton = $('#skeleton');
        let timer;
        let controller;
        const showSkeletons = () => {
            if (!skeleton) return;
            results.innerHTML = '';
            for (let i = 0; i < 5; i++) results.appendChild(skeleton.content.cloneNode(true));
        };
        const run = async () => {
            const q = input.value.trim();
            const url = new URL(location.href);
            if (q) url.searchParams.set('q', q); else url.searchParams.delete('q');
            history.replaceState(null, '', url);
            if (q.length < 2) { results.innerHTML = '<p class="muted small-empty">Type at least two letters.</p>'; return; }
            if (controller) controller.abort();
            controller = new AbortController();
            showSkeletons();
            try {
                const response = await fetch('/discover/results?q=' + encodeURIComponent(q), { signal: controller.signal, credentials: 'same-origin' });
                if (response.status === 401) { location.reload(); return; }
                results.innerHTML = await response.text();
                sharpen(results);
            } catch (error) {
                if (error.name !== 'AbortError') results.innerHTML = '<div class="msg err" role="alert"><p>Can’t reach Ludex right now.</p></div>';
            }
        };
        input.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(run, 320); });
        searchForm.addEventListener('submit', e => { e.preventDefault(); clearTimeout(timer); run(); });
    }

    /* ---------- Library: filter by text, platform and friends, and sort, in the page ---------- */
    const library = $('[data-library]');
    if (library) {
        const cards = $$('.card', library);
        const text = $('[data-text-filter]');
        const families = $$('[data-family-filter]');
        const friendsOnly = $('[data-friends-filter]');
        const sort = $('[data-sort]');
        const label = $('[data-shown-label]');
        const none = $('[data-none]');
        const keys = {
            updated: c => -Number(c.dataset.updated),
            added: c => -Number(c.dataset.added),
            rating: c => -Number(c.dataset.rating),
            hours: c => -Number(c.dataset.hours),
            name: c => c.dataset.name,
        };
        const apply = () => {
            const q = (text?.value || '').trim().toLowerCase();
            const allowed = new Set(families.filter(f => f.checked).map(f => f.value));
            let shown = 0;
            cards.forEach(card => {
                const visible = (!q || card.dataset.name.includes(q))
                    && (!families.length || allowed.has(card.dataset.family))
                    && (!friendsOnly?.checked || Number(card.dataset.friends) > 0);
                card.classList.toggle('is-hidden', !visible);
                if (visible) shown++;
            });
            const key = keys[sort?.value] || keys.updated;
            cards.slice().sort((a, b) => {
                const ka = key(a), kb = key(b);
                return ka < kb ? -1 : ka > kb ? 1 : 0;
            }).forEach(card => library.appendChild(card));
            if (label) label.textContent = `${shown} ${shown === 1 ? 'game' : 'games'}`;
            if (none) none.hidden = shown > 0;
        };
        // Cards carry their cover's view-transition name, so a filter or a new order animates every card into place.
        const animate = () => (document.startViewTransition && !reduceMotion) ? document.startViewTransition(apply) : apply();
        let textTimer;
        text?.addEventListener('input', () => { clearTimeout(textTimer); textTimer = setTimeout(animate, 140); });
        families.forEach(f => f.addEventListener('change', animate));
        friendsOnly?.addEventListener('change', animate);
        sort?.addEventListener('change', animate);
    }

    /* ---------- Game page: status and rating save at once; hours, platform and notes with Save ---------- */
    const form = $('[data-game-form]');
    if (form) {
        const game = form.dataset.game;
        const statusButtons = $$('[data-set-status]', form);
        const rateButtons = $$('[data-set-rating]', form);
        const ratingValue = $('[data-rating-value]', form);
        const clearRating = $('[data-clear-rating]', form);
        let status = statusButtons.find(b => b.getAttribute('aria-pressed') === 'true')?.dataset.setStatus || null;
        let rating = Number(rateButtons.find(b => b.getAttribute('aria-pressed') === 'true')?.dataset.setRating || 0) || null;

        const paint = () => {
            statusButtons.forEach(b => b.setAttribute('aria-pressed', b.dataset.setStatus === status ? 'true' : 'false'));
            rateButtons.forEach(b => {
                const n = Number(b.dataset.setRating);
                b.classList.toggle('on', rating !== null && n <= rating);
                b.setAttribute('aria-pressed', n === rating ? 'true' : 'false');
            });
            if (ratingValue) ratingValue.textContent = rating ?? '—';
            if (clearRating) clearRating.hidden = rating === null;
        };
        paint();

        const save = async message => {
            // Rating a game that isn't in the library yet means it was played: it goes in as Finished.
            if (!status) { status = rating ? 'Finished' : 'Playing'; paint(); }
            const hours = form.elements.hours.value.trim();
            const entry = await post(`/games/${game}/save`, {
                status,
                rating,
                hours: hours === '' ? null : Number(hours.replace(',', '.')),
                platform: form.elements.platform.value || null,
                notes: form.elements.notes.value.trim() || null,
            });
            if (!entry) return;
            const added = form.dataset.inLibrary !== 'true';
            form.dataset.inLibrary = 'true';
            const pill = $('[data-lib-pill]');
            if (pill) {
                pill.classList.remove('out');
                const text = $('[data-lib-pill-text]', pill);
                if (text) text.textContent = `In your library · ${statusWords[entry.status] || entry.status}`;
                const icon = $('svg', pill);
                if (icon) icon.innerHTML = '<path d="M5 12.5l4.5 4.5L19 7.5"/>';
            }
            const line = $('[data-your-line]', form);
            if (line) {
                const parts = [];
                if (entry.hours != null) parts.push(`${Number(entry.hours).toLocaleString('en-IE')} h on record`);
                parts.push('added ' + new Date(entry.addedAt).toLocaleDateString('en-IE', { day: 'numeric', month: 'short' }));
                line.textContent = parts.join(' · ');
            }
            const fav = $('[data-favorite]', form);
            if (fav) fav.hidden = false;
            toast(added ? 'Added to your library' : message);
        };

        statusButtons.forEach(b => b.addEventListener('click', () => { status = b.dataset.setStatus; paint(); save(statusWords[status] || 'Saved'); }));
        rateButtons.forEach(b => b.addEventListener('click', () => { rating = Number(b.dataset.setRating); paint(); save(`Rated ${rating}/10`); }));
        clearRating?.addEventListener('click', () => { rating = null; paint(); save('Rating cleared'); });
        form.addEventListener('submit', e => { e.preventDefault(); save('Saved'); });
    }

    /* ---------- Game page: the favourite on the profile ---------- */
    const favorite = $('[data-favorite]');
    if (favorite && form) {
        favorite.addEventListener('click', async () => {
            const on = favorite.getAttribute('aria-pressed') !== 'true';
            favorite.disabled = true;
            const result = await post('/profile/favorite', { gameId: on ? Number(form.dataset.game) : null });
            favorite.disabled = false;
            if (!result) return;
            favorite.setAttribute('aria-pressed', on ? 'true' : 'false');
            const label = $('[data-favorite-label]', favorite);
            if (label) label.textContent = on ? 'Your favourite' : 'Make favourite';
            toast(on ? 'Shown on your profile' : 'Favourite cleared: your best rated shows instead');
        });
    }

    /* ---------- Game page: the screenshot viewer (arrows, thumbnails, keys, swipe) ---------- */
    const viewer = $('[data-viewer]');
    if (viewer) {
        const slides = $$('.viewer-shot', viewer);
        const thumbs = $$('[data-viewer-pick]');
        const count = $('[data-viewer-count]', viewer);
        let index = 0;
        const show = n => {
            if (!slides.length) return;
            index = (n + slides.length) % slides.length;
            slides.forEach((s, i) => {
                const on = i === index;
                s.classList.toggle('on', on);
                s.tabIndex = on ? 0 : -1;
                const img = $('img', s);
                if (on && img && img.dataset.big && img.getAttribute('src') !== img.dataset.big) {
                    img.classList.remove('is-loaded');
                    img.src = img.dataset.big;
                }
            });
            thumbs.forEach((t, i) => {
                t.classList.toggle('on', i === index);
                t.setAttribute('aria-pressed', i === index ? 'true' : 'false');
                if (i === index) t.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: reduceMotion ? 'auto' : 'smooth' });
            });
            if (count) count.textContent = `${index + 1} / ${slides.length}`;
        };
        $$('[data-viewer-step]', viewer).forEach(b => b.addEventListener('click', () => show(index + Number(b.dataset.viewerStep))));
        thumbs.forEach(t => t.addEventListener('click', () => show(Number(t.dataset.viewerPick))));
        viewer.addEventListener('keydown', e => {
            if (e.key === 'ArrowLeft') { e.preventDefault(); show(index - 1); }
            if (e.key === 'ArrowRight') { e.preventDefault(); show(index + 1); }
        });
        let startX = null;
        viewer.addEventListener('touchstart', e => { startX = e.touches[0].clientX; }, { passive: true });
        viewer.addEventListener('touchend', e => {
            if (startX === null) return;
            const dx = e.changedTouches[0].clientX - startX;
            if (Math.abs(dx) > 40) show(index + (dx < 0 ? 1 : -1));
            startX = null;
        });
    }

    /* ---------- Profile photo: shrunk to 256 px here, then sent ---------- */
    const photoInput = $('[data-photo-input]');
    if (photoInput) {
        const preview = $('[data-photo-preview]');
        const shrink = file => new Promise((resolve, reject) => {
            const url = URL.createObjectURL(file);
            const img = new Image();
            img.onload = () => {
                // A centred square, 256 px: avatars are square everywhere.
                const side = Math.min(img.naturalWidth, img.naturalHeight);
                const canvas = document.createElement('canvas');
                canvas.width = canvas.height = 256;
                const ctx = canvas.getContext('2d');
                ctx.imageSmoothingQuality = 'high';
                ctx.drawImage(img, (img.naturalWidth - side) / 2, (img.naturalHeight - side) / 2, side, side, 0, 0, 256, 256);
                URL.revokeObjectURL(url);
                canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error('encode')), 'image/jpeg', 0.86);
            };
            img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('read')); };
            img.src = url;
        });
        photoInput.addEventListener('change', async () => {
            const file = photoInput.files && photoInput.files[0];
            if (!file) return;
            let blob;
            try { blob = await shrink(file); } catch { toast('That image can’t be read. Try a JPEG, PNG or WebP.', true); return; }
            const body = new FormData();
            body.append('photo', blob, 'avatar.jpg');
            photoInput.disabled = true;
            try {
                const response = await fetch('/profile/photo', { method: 'POST', body, headers: { 'RequestVerificationToken': token }, credentials: 'same-origin' });
                if (response.status === 401) { location.href = '/sign-in'; return; }
                const result = await response.json();
                if (!result.isValid) { toast((result.validationErrors || []).map(e => e.message).join(' ') || 'That didn’t work.', true); return; }
                // Show it at once, everywhere on this page, then reload so every avatar uses the new address.
                const local = URL.createObjectURL(blob);
                $$('.avatar').forEach(av => {
                    if (!av.closest('[data-photo-preview]') && !av.closest('.me') && !av.closest('.dock')) return;
                    let img = $('img', av);
                    if (!img) { img = document.createElement('img'); img.alt = ''; av.appendChild(img); }
                    img.src = local;
                });
                if (preview) preview.classList.add('pop-in');
                toast('Photo saved');
                setTimeout(() => location.reload(), 900);
            } catch {
                toast('Can’t reach Ludex right now.', true);
            } finally {
                photoInput.disabled = false;
                photoInput.value = '';
            }
        });
    }

    /* ---------- Steam import: load (with a wait note), tick, pick statuses, import ---------- */
    const steamLoad = $('[data-steam-load]');
    if (steamLoad) {
        steamLoad.addEventListener('submit', () => {
            const button = $('[data-steam-load-button]', steamLoad);
            if (button) { button.disabled = true; button.classList.add('is-busy'); }
            const wait = $('[data-steam-wait]', steamLoad);
            if (wait) wait.hidden = false;
        });
    }
    const steam = $('[data-steam]');
    if (steam && steam.dataset.steam) {
        const rows = $$('[data-steam-row]', steam);
        const boxes = () => rows.map(r => ({ row: r, box: $('input[type=checkbox]', r) })).filter(x => x.box && !x.box.disabled);
        const count = $('[data-steam-count]', steam);
        const word = $('[data-steam-count-word]', steam);
        const bar = $('[data-steam-bar]', steam);
        const paint = () => {
            const n = boxes().filter(x => x.box.checked).length;
            if (count) count.textContent = n;
            if (word) word.textContent = n === 1 ? 'game' : 'games';
            rows.forEach(r => { const b = $('input[type=checkbox]', r); r.classList.toggle('is-off', !!b && !b.checked); });
            const go = $('[data-steam-import]', steam);
            if (go) go.disabled = n === 0;
        };
        steam.addEventListener('change', e => {
            if (e.target.matches('input[type=checkbox]')) paint();
            if (e.target.matches('.steam-status')) {
                // Choosing a status means "import this one".
                const box = $('input[type=checkbox]', e.target.closest('[data-steam-row]'));
                if (box && !box.disabled) { box.checked = true; paint(); }
            }
            if (e.target.matches('[data-steam-all]') && e.target.value) {
                $$('.steam-status', steam).forEach(sel => { sel.value = e.target.value; });
                e.target.value = '';
            }
        });
        $$('[data-steam-pick]', steam).forEach(b => b.addEventListener('click', () => {
            const how = b.dataset.steamPick;
            const section = b.closest('.steam-block');
            boxes().filter(x => section.contains(x.row)).forEach(x => {
                x.box.checked = how === 'all' || (how === 'played' && x.row.dataset.minutesPlayed === '1');
            });
            paint();
        }));
        $('[data-steam-import]', steam)?.addEventListener('click', async e => {
            const button = e.currentTarget;
            const games = boxes().filter(x => x.box.checked).map(({ row }) => ({
                gameId: Number(row.dataset.game),
                status: $('.steam-status', row)?.value || 'Finished',
                hours: row.dataset.hours === '' ? null : Number(row.dataset.hours),
                lastPlayedAt: row.dataset.played || null,
            }));
            if (!games.length) return;
            button.disabled = true;
            button.classList.add('is-busy');
            const result = await post('/steam/import', { steamId: steam.dataset.steam, games });
            button.classList.remove('is-busy');
            if (!result) { button.disabled = false; return; }
            const parts = [];
            if (result.added) parts.push(`${result.added} added`);
            if (result.updated) parts.push(`${result.updated} with more hours`);
            toast(parts.length ? `Steam: ${parts.join(', ')}` : 'Already up to date');
            setTimeout(() => { location.href = '/library'; }, 1200);
        });
        if (bar) bar.classList.add('is-ready');
        paint();
    }

    /* ---------- Characters left in a text box ---------- */
    $$('[data-count-chars]').forEach(box => {
        const max = Number(box.dataset.countChars);
        const left = box.parentElement && $('[data-chars-left]', box.parentElement);
        if (!left) return;
        const paint = () => { const n = max - box.value.length; left.textContent = `${n} ${n === 1 ? 'character' : 'characters'} left`; };
        box.addEventListener('input', paint);
        paint();
    });

    /* ---------- Screenshots open full size ---------- */
    const lightbox = $('[data-lightbox]');
    if (lightbox) {
        const img = $('img', lightbox);
        document.addEventListener('click', e => {
            const shot = e.target.closest('.shot[data-full], .viewer-shot[data-full]');
            if (!shot) return;
            img.src = shot.dataset.full;
            lightbox.showModal();
        });
        lightbox.addEventListener('click', e => { if (e.target === lightbox) lightbox.close(); });
    }

    /* ---------- Forms that ask first ---------- */
    document.addEventListener('submit', e => {
        const message = e.target.dataset && e.target.dataset.confirm;
        if (message && !confirm(message)) e.preventDefault();
    });

    /* ---------- Installable as an app ---------- */
    if ('serviceWorker' in navigator && location.protocol === 'https:') {
        navigator.serviceWorker.register('/sw.js').catch(() => { });
    }
})();
