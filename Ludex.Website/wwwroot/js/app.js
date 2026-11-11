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

    const statusWords = { Playing: 'Playing', Finished: 'Finished', Wishlist: 'Wishlist', Dropped: 'Dropped', Backlog: 'Backlog' };

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

    /* ---------- Library: status, platform, copy, friends and text filters, and Steam's sorts, in the page ---------- */
    const store = {
        get: key => { try { return localStorage.getItem(key); } catch { return null; } },
        set: (key, value) => { try { localStorage.setItem(key, value); } catch { /* private window */ } },
    };
    const library = $('[data-library]');
    if (library) {
        const cards = $$('.lib-card', library);
        const text = $('[data-text-filter]');
        const status = $('[data-status-filter]');
        const platform = $('[data-platform-filter]');
        const copy = $('[data-copy-filter]');
        const collection = $('[data-collection-filter]');
        const friendsOnly = $('[data-friends-filter]');
        const sort = $('[data-sort]');
        const label = $('[data-shown-label]');
        const none = $('[data-none]');
        const num = (c, key) => Number(c.dataset[key] || 0);
        const keys = {
            played: (a, b) => num(b, 'updated') - num(a, 'updated'),
            name: (a, b) => a.dataset.name.localeCompare(b.dataset.name),
            friends: (a, b) => num(b, 'friends') - num(a, 'friends') || a.dataset.name.localeCompare(b.dataset.name),
            rating: (a, b) => num(b, 'rating') - num(a, 'rating') || num(b, 'hours') - num(a, 'hours'),
            achievements: (a, b) => num(b, 'achievements') - num(a, 'achievements') || a.dataset.name.localeCompare(b.dataset.name),
            critic: (a, b) => num(b, 'critic') - num(a, 'critic') || a.dataset.name.localeCompare(b.dataset.name),
            recent: (a, b) => num(b, 'recent') - num(a, 'recent') || num(b, 'updated') - num(a, 'updated'),
            // Unknown times go last.
            shortest: (a, b) => (num(a, 'ttb') || 1e9) - (num(b, 'ttb') || 1e9) || a.dataset.name.localeCompare(b.dataset.name),
            hours: (a, b) => num(b, 'hours') - num(a, 'hours'),
            release: (a, b) => num(b, 'release') - num(a, 'release') || a.dataset.name.localeCompare(b.dataset.name),
            added: (a, b) => num(b, 'added') - num(a, 'added'),
        };
        const saved = store.get('ludex.sort');
        if (sort && saved && keys[saved]) sort.value = saved;
        const apply = () => {
            const q = (text?.value || '').trim().toLowerCase();
            const st = status?.value || '';
            const pf = platform?.value || '';
            const cp = copy?.value || '';
            const col = collection?.value || '';
            let shown = 0;
            cards.forEach(card => {
                const d = card.dataset;
                const visible = (!q || d.name.includes(q))
                    && (!st || d.status === st)
                    && (!pf || (pf.startsWith('family:') ? d.family === pf.slice(7) : d.platform.toLowerCase() === pf.slice(9).toLowerCase()))
                    && (!cp || (cp === 'Physical' || cp === 'Digital' ? d.format === cp : d.store === cp))
                    && (!col || (d.collections || '').includes(`,${col},`))
                    && (!friendsOnly?.checked || Number(d.friends) > 0);
                card.classList.toggle('is-hidden', !visible);
                if (visible) shown++;
            });
            const compare = keys[sort?.value] || keys.played;
            cards.slice().sort(compare).forEach(card => library.appendChild(card));
            if (label) label.textContent = `${shown} ${shown === 1 ? 'game' : 'games'}`;
            if (none) none.hidden = shown > 0;
            const url = new URL(location.href);
            if (st) url.searchParams.set('status', st.toLowerCase()); else url.searchParams.delete('status');
            if (col) url.searchParams.set('collection', col); else url.searchParams.delete('collection');
            history.replaceState(null, '', url);
            const acts = $('[data-col-acts]');
            if (acts) acts.hidden = !col;
        };
        // Cards carry their cover's view-transition name, so a filter or a new order animates every card into place.
        const animate = () => (document.startViewTransition && !reduceMotion) ? document.startViewTransition(apply) : apply();
        let textTimer;
        text?.addEventListener('input', () => { clearTimeout(textTimer); textTimer = setTimeout(animate, 140); });
        [status, platform, copy, collection, friendsOnly].forEach(el => el?.addEventListener('change', animate));
        $('[data-col-rename]')?.addEventListener('click', async () => {
            const id = Number(collection.value);
            const current = collection.selectedOptions[0]?.textContent.replace(/\s\(\d+\)$/, '') || '';
            const name = (window.prompt('New name for the collection', current) || '').trim();
            if (!name || name === current) return;
            if (await post('/collections/rename', { id, name })) location.reload();
        });
        $('[data-col-delete]')?.addEventListener('click', async () => {
            const id = Number(collection.value);
            if (!window.confirm('Delete this collection? Its games stay in your library.')) return;
            if (await post('/collections/delete', { id })) { location.href = '/library'; }
        });
        sort?.addEventListener('change', () => { store.set('ludex.sort', sort.value); animate(); });
        apply();
    }

    /* ---------- Library sidebar: find a game, and keep the list where it was between pages ---------- */
    const side = $('[data-lib-side]');
    if (side) {
        const list = $('.lib-side-groups', side);
        const top = Number(store.get('ludex.side') || 0);
        if (list && top) list.scrollTop = top;
        const current = $('a[aria-current="page"]', list || side);
        if (list && current && !top) current.scrollIntoView({ block: 'center' });
        list?.addEventListener('scroll', () => store.set('ludex.side', String(list.scrollTop)), { passive: true });
        // A new collection.
        const newButton = $('[data-new-collection]', side);
        const newForm = $('[data-new-collection-form]', side);
        newButton?.addEventListener('click', () => {
            newForm.hidden = !newForm.hidden;
            if (!newForm.hidden) $('input', newForm).focus();
        });
        newForm?.addEventListener('submit', async e => {
            e.preventDefault();
            const name = $('input', newForm).value.trim();
            if (!name) return;
            if (await post('/collections/create', { name })) location.reload();
        });

        // Drag a game (a card, or a game in the sidebar) onto a collection to put it there.
        document.addEventListener('dragstart', e => {
            const source = e.target.closest && e.target.closest('[data-drag-game]');
            if (!source) return;
            e.dataTransfer.setData('text/x-ludex-game', source.dataset.dragGame);
            e.dataTransfer.effectAllowed = 'copy';
            side.classList.add('is-dragging');
        });
        document.addEventListener('dragend', () => {
            side.classList.remove('is-dragging');
            $$('.is-drop', side).forEach(el => el.classList.remove('is-drop'));
        });
        $$('[data-drop-collection]', side).forEach(group => {
            group.addEventListener('dragover', e => {
                if (!e.dataTransfer.types.includes('text/x-ludex-game')) return;
                e.preventDefault();
                e.dataTransfer.dropEffect = 'copy';
                group.classList.add('is-drop');
            });
            group.addEventListener('dragleave', e => { if (!group.contains(e.relatedTarget)) group.classList.remove('is-drop'); });
            group.addEventListener('drop', async e => {
                e.preventDefault();
                group.classList.remove('is-drop');
                const gameId = Number(e.dataTransfer.getData('text/x-ludex-game'));
                if (!gameId) return;
                const collectionId = Number(group.dataset.dropCollection);
                const list = $('ul', group);
                if ($(`[data-drag-game="${gameId}"]`, list)) { toast('Already in this collection'); return; }
                if (!(await post('/collections/game', { collectionId, gameId, in: true }))) return;
                const item = $(`.lib-group:not(.lib-col) [data-drag-game="${gameId}"]`, side)?.closest('li');
                if (item) {
                    const copy = item.cloneNode(true);
                    $('a', copy)?.removeAttribute('aria-current');
                    copy.classList.add('pop-in');
                    list.appendChild(copy);
                }
                const count = $('[data-col-count]', group);
                if (count) count.textContent = String(Number(count.textContent) + 1);
                const card = $(`.lib-card[data-drag-game="${gameId}"]`);
                if (card) card.dataset.collections = (card.dataset.collections || ',') + collectionId + ',';
                group.open = true;
                toast(`Added to ${$('summary span', group)?.textContent || 'the collection'}`);
            });
        });

        const find = $('[data-side-filter]', side);
        find?.addEventListener('input', () => {
            const q = find.value.trim().toLowerCase();
            $$('[data-side-name]', side).forEach(li => { li.hidden = !!q && !li.dataset.sideName.includes(q); });
            $$('.lib-group', side).forEach(g => {
                const any = $$('[data-side-name]', g).some(li => !li.hidden);
                g.hidden = !any;
                if (q) g.open = true;
            });
        });
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

        // The copy: the stores for a PC, physical or digital for a console (only what that platform had).
        const copySelect = form.elements.copy;
        const copyChoices = (() => { try { return JSON.parse(copySelect?.dataset.choices || '{}'); } catch { return {}; } })();
        const copyLabel = $('[data-copy-label]');
        const fillCopies = () => {
            if (!copySelect) return;
            const platformName = form.elements.platform.value || '';
            const key = Object.keys(copyChoices).find(k => k.toLowerCase() === platformName.toLowerCase());
            const choices = copyChoices[key ?? ''] || ['Physical', 'Digital'];
            const keep = copySelect.value;
            copySelect.innerHTML = '<option value="">—</option>' + choices.map(c => `<option value="${c}">${c}</option>`).join('');
            copySelect.value = choices.includes(keep) ? keep : (choices.length === 1 ? choices[0] : '');
            if (copyLabel) copyLabel.textContent = choices.includes('Steam') ? 'Bought on' : 'Copy';
        };
        form.elements.platform?.addEventListener('change', fillCopies);

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
                store: copySelect && copySelect.value && copySelect.value !== 'Physical' && copySelect.value !== 'Digital' ? copySelect.value : null,
                format: copySelect && (copySelect.value === 'Physical' || copySelect.value === 'Digital') ? copySelect.value : null,
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

    /* ---------- Game page: collections ---------- */
    const colBox = $('[data-collections]');
    if (colBox) {
        const gameId = Number(colBox.dataset.game);
        colBox.addEventListener('click', async e => {
            const out = e.target.closest('[data-col-out]');
            if (!out) return;
            const collectionId = Number(out.dataset.colOut);
            if (!(await post('/collections/game', { collectionId, gameId, in: false }))) return;
            location.reload();
        });
        $('[data-col-add]', colBox)?.addEventListener('change', async e => {
            const value = e.target.value;
            e.target.value = '';
            if (!value) return;
            let collectionId = Number(value);
            if (value === 'new') {
                const name = (window.prompt('Name of the new collection') || '').trim();
                if (!name) return;
                const list = await post('/collections/create', { name });
                const made = list && list.find(c => c.name.toLowerCase() === name.toLowerCase());
                if (!made) return;
                collectionId = made.id;
            }
            if (await post('/collections/game', { collectionId, gameId, in: true })) location.reload();
        });
    }

    /* ---------- Game page: every achievement ---------- */
    $('[data-ach-more]')?.addEventListener('click', e => {
        $$('[data-ach-item]').forEach(li => { li.hidden = false; });
        e.currentTarget.remove();
    });

    /* ---------- Game page: the price alert ---------- */
    const priceBox = $('[data-price]');
    if (priceBox) {
        $('[data-price-save]', priceBox)?.addEventListener('click', async () => {
            const raw = $('[data-price-target]', priceBox).value.trim().replace(',', '.');
            const target = raw === '' ? null : Number(raw);
            const result = await post(`/games/${priceBox.dataset.game}/price-target`, { target });
            if (result) toast(target === null ? 'No price alert' : `You'll get an alert under ${target.toFixed(2)}`);
        });
    }

    /* ---------- The bell ---------- */
    const bell = $('[data-bell-count]');
    if (bell) {
        fetch('/notifications/count', { credentials: 'same-origin' })
            .then(r => r.ok ? r.json() : { unread: 0 })
            .then(d => { if (d.unread > 0) { bell.textContent = d.unread > 9 ? '9+' : String(d.unread); bell.hidden = false; } })
            .catch(() => { });
    }

    /* ---------- Steam: sync now ---------- */
    $('[data-steam-sync]')?.addEventListener('click', async e => {
        const button = e.currentTarget;
        button.disabled = true;
        button.classList.add('is-busy');
        const result = await post('/steam/sync', {});
        button.disabled = false;
        button.classList.remove('is-busy');
        if (!result) return;
        const parts = [];
        if (result.added) parts.push(`${result.added} new`);
        if (result.updated) parts.push(`${result.updated} with more hours`);
        if (result.achievements) parts.push(`${result.achievements} achievement counts`);
        toast(parts.length ? `Synced: ${parts.join(', ')}` : 'Synced: nothing new');
    });

    /* ---------- Library: what should I play next? ---------- */
    const nextDialog = $('[data-next]');
    if (nextDialog && library) {
        let hours = 10;
        let from = 'Backlog';
        const picks = $('[data-next-picks]', nextDialog);
        const note = $('[data-next-note]', nextDialog);
        const press = (selector, value) => $$(selector, nextDialog).forEach(b => b.setAttribute('aria-pressed', b.dataset[selector.includes('hours') ? 'nextHours' : 'nextFrom'] === String(value) ? 'true' : 'false'));
        const pick = () => {
            const pool = $$('.lib-card', library).filter(c => {
                const st = c.dataset.status;
                const okFrom = from === 'both' ? (st === 'Backlog' || st === 'Wishlist') : st === from;
                const ttb = Number(c.dataset.ttb || 0);
                return okFrom && (hours === 0 || (ttb > 0 && ttb <= hours));
            });
            // Better-rated by critics and liked by friends come up more often.
            const weight = c => 1 + Number(c.dataset.critic || 0) / 25 + Number(c.dataset.friends || 0);
            const chosen = [];
            const rest = pool.slice();
            while (chosen.length < 3 && rest.length) {
                const total = rest.reduce((s, c) => s + weight(c), 0);
                let r = Math.random() * total;
                const i = rest.findIndex(c => (r -= weight(c)) <= 0);
                chosen.push(rest.splice(i < 0 ? 0 : i, 1)[0]);
            }
            picks.innerHTML = chosen.map(c => {
                const ttb = Number(c.dataset.ttb || 0);
                const name = c.getAttribute('aria-label') || '';
                const esc = s => s.replace(/[&<>"]/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[ch]));
                return `<a class="next-pick pop-in" href="${c.getAttribute('href')}">`
                    + `<span class="next-cover" style="--tint:${c.dataset.tint}">${c.dataset.cover ? `<img src="${c.dataset.cover}" alt="">` : `<span>${esc(name)}</span>`}</span>`
                    + `<b>${esc(name)}</b><small>${ttb ? `${ttb} h main story` : 'Length unknown'}${Number(c.dataset.critic) ? ` · critics ${c.dataset.critic}` : ''}</small></a>`;
            }).join('');
            note.textContent = pool.length ? `${pool.length} ${pool.length === 1 ? 'game fits' : 'games fit'}` : 'Nothing fits: try more time, or Both.';
        };
        $('[data-next-open]')?.addEventListener('click', () => { nextDialog.showModal(); pick(); });
        nextDialog.addEventListener('click', e => {
            const h = e.target.closest('[data-next-hours]');
            const f = e.target.closest('[data-next-from]');
            if (h) { hours = Number(h.dataset.nextHours); press('[data-next-hours]', hours); pick(); }
            if (f) { from = f.dataset.nextFrom; press('[data-next-from]', from); pick(); }
            if (e.target.closest('[data-next-shuffle]')) pick();
            if (e.target === nextDialog) nextDialog.close();
        });
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
