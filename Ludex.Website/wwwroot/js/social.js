/* Ludex — the Backloggd-style parts: the Log a game dialog (search a game, then status, half-star rating, how it
   ended, the review and a session for the journal), likes on reviews and lists, spoilers, the review tabs on Home,
   and the game page's "Your lists". Uses what app.js shares on window.ludex. Without it, the pages still work:
   the button opens Discover, and every list, review and journal page is plain links and forms. */
'use strict';

(function () {
    const L = window.ludex;
    if (!L) return;
    const $ = (selector, root) => (root || document).querySelector(selector);
    const $$ = (selector, root) => Array.from((root || document).querySelectorAll(selector));
    const ENDS = ['Finished', 'Played', 'Dropped'];

    /* ---------- Log a game ---------- */
    const dialog = $('[data-log-dialog]');
    const searchStep = dialog && $('[data-log-step="search"]', dialog);
    const formStep = dialog && $('[data-log-step="form"]', dialog);
    const queryInput = dialog && $('[data-log-q]', dialog);
    const results = dialog && $('[data-log-results]', dialog);
    let searchTimer;
    let searchSeq = 0;

    async function html(url) {
        const response = await fetch(url, { credentials: 'same-origin', headers: { 'Accept': 'text/html' } });
        if (response.status === 401) { location.href = '/sign-in?returnUrl=' + encodeURIComponent(location.pathname + location.search); return null; }
        const text = await response.text();
        if (!response.ok) throw new Error(text || 'That didn’t work.');
        return text;
    }

    function open() {
        if (!dialog) return false;
        if (!dialog.open) dialog.showModal();
        return true;
    }

    function showSearch() {
        formStep.hidden = true;
        formStep.innerHTML = '';
        searchStep.hidden = false;
        queryInput.focus();
        if (!results.childElementCount) search('');
    }

    async function search(query) {
        const seq = ++searchSeq;
        results.setAttribute('aria-busy', 'true');
        try {
            const text = await html('/log/search?q=' + encodeURIComponent(query));
            if (seq !== searchSeq || text === null) return;
            results.innerHTML = text;
            L.sharpen(results);
        } catch (error) {
            if (seq === searchSeq) results.innerHTML = `<p class="muted log-empty">${L.esc(error.message)}</p>`;
        } finally {
            if (seq === searchSeq) results.removeAttribute('aria-busy');
        }
    }

    async function showForm(gameId, entryId) {
        searchStep.hidden = true;
        formStep.hidden = false;
        formStep.innerHTML = '<div class="log-loading"><span class="skel"></span><span class="skel"></span><span class="skel"></span></div>';
        try {
            const text = await html(`/log/${encodeURIComponent(gameId)}` + (entryId ? `?entry=${encodeURIComponent(entryId)}` : ''));
            if (text === null) return;
            formStep.innerHTML = text;
            L.sharpen(formStep);
            wireForm($('[data-log-form]', formStep));
        } catch (error) {
            formStep.innerHTML = `<div class="log-error"><p>${L.esc(error.message)}</p><button type="button" class="btn ghost small" data-log-back>Pick another game</button></div>`;
        }
    }

    function wireForm(form) {
        if (!form) return;
        const statusButtons = $$('[data-log-status]', form);
        const endingButtons = $$('[data-log-ending]', form);
        const endings = $('[data-log-endings]', form);
        const stars = $$('[data-log-rate]', form);
        const value = $('[data-log-rating-value]', form);
        const clear = $('[data-log-clear]', form);
        let status = statusButtons.find(b => b.getAttribute('aria-pressed') === 'true')?.dataset.logStatus || 'Finished';
        let ending = endingButtons.find(b => b.getAttribute('aria-pressed') === 'true')?.dataset.logEnding || null;
        let rating = Number(stars.find(b => b.getAttribute('aria-pressed') === 'true')?.dataset.logRate || 0);

        const paintStars = shown => {
            stars.forEach(b => {
                const n = Number(b.dataset.logRate);
                b.classList.toggle('on', n <= shown);
                b.setAttribute('aria-pressed', n === rating ? 'true' : 'false');
            });
            value.textContent = shown ? String(shown / 2) : '—';
        };
        const paint = () => {
            statusButtons.forEach(b => b.setAttribute('aria-pressed', b.dataset.logStatus === status ? 'true' : 'false'));
            const hasEnd = ENDS.includes(status);
            endings.hidden = !hasEnd;
            if (!hasEnd) ending = null;
            endingButtons.forEach(b => b.setAttribute('aria-pressed', b.dataset.logEnding === ending ? 'true' : 'false'));
            paintStars(rating);
            clear.hidden = !rating;
        };
        paint();

        statusButtons.forEach(b => b.addEventListener('click', () => {
            status = b.dataset.logStatus;
            // Finishing a game suggests "Completed"; dropping it, "Abandoned" (both can be changed).
            if (status === 'Finished' && (!ending || ending === 'Abandoned' || ending === 'Shelved')) ending = 'Completed';
            if (status === 'Dropped' && (!ending || ending === 'Completed' || ending === 'Mastered')) ending = 'Abandoned';
            paint();
        }));
        endingButtons.forEach(b => b.addEventListener('click', () => { ending = ending === b.dataset.logEnding ? null : b.dataset.logEnding; paint(); }));
        stars.forEach(b => {
            b.addEventListener('click', () => { rating = Number(b.dataset.logRate); paint(); });
            b.addEventListener('pointerenter', () => paintStars(Number(b.dataset.logRate)));
        });
        $('[data-log-stars]', form)?.addEventListener('pointerleave', () => paintStars(rating));
        clear.addEventListener('click', () => { rating = 0; paint(); });

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const f = form.elements;
            const number = el => { const v = (el?.value || '').trim(); return v === '' ? null : Number(v); };
            const minutes = (number(f.sessionHours) || 0) * 60 + (number(f.sessionMinutes) || 0);
            const button = $('button[type="submit"]', form);
            button.disabled = true;
            const data = await L.post(`/log/${form.dataset.game}`, {
                entryId: form.dataset.entry ? Number(form.dataset.entry) : null,
                status,
                rating: rating || null,
                platform: f.platform.value || null,
                hours: number(f.hours),
                ending,
                startedOn: f.startedOn.value || null,
                finishedOn: f.finishedOn.value || null,
                review: f.review.value,
                spoilers: f.spoilers.checked,
                sessionMinutes: minutes || null,
                sessionNote: f.sessionNote.value || null,
                share: f.share.checked,
            });
            button.disabled = false;
            if (!data) return;
            L.toast(data.reviewId ? 'Logged, with your review' : 'Logged');
            const page = `/games/${form.dataset.game}`;
            setTimeout(() => {
                if (location.pathname === page || location.pathname.startsWith('/reviews/')) location.reload();
                else location.href = page;
            }, 450);
        });

        $('[name="review"]', form)?.focus({ preventScroll: true });
    }

    if (dialog) {
        document.addEventListener('click', e => {
            const opener = e.target.closest('[data-log-open]');
            if (opener) {
                e.preventDefault();
                if (open()) showSearch();
                return;
            }

            const fromGame = e.target.closest('[data-log-game]');
            if (fromGame) {
                e.preventDefault();
                if (open()) showForm(fromGame.dataset.logGame, fromGame.dataset.logEntry);
                return;
            }

            if (!dialog.open) return;
            if (e.target === dialog || e.target.closest('[data-log-close]')) { dialog.close(); return; }
            const pick = e.target.closest('[data-log-pick]');
            if (pick) { showForm(pick.dataset.logPick); return; }
            if (e.target.closest('[data-log-back]')) showSearch();
        });

        queryInput.addEventListener('input', () => {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(() => search(queryInput.value.trim()), 250);
        });
        queryInput.addEventListener('keydown', e => {
            if (e.key === 'Enter') { e.preventDefault(); clearTimeout(searchTimer); search(queryInput.value.trim()); }
        });
        dialog.addEventListener('close', () => { formStep.innerHTML = ''; });
    }

    /* ---------- Likes on reviews and lists ---------- */
    document.addEventListener('click', async e => {
        const button = e.target.closest('[data-like-review], [data-like-list]');
        if (!button || button.disabled) return;
        const review = button.dataset.likeReview;
        const like = button.getAttribute('aria-pressed') !== 'true';
        const count = $('[data-like-count]', button);
        const before = Number(count?.textContent || 0);
        button.setAttribute('aria-pressed', like ? 'true' : 'false');
        if (count) count.textContent = String(Math.max(0, before + (like ? 1 : -1)));
        button.disabled = true;
        const data = await L.post(review ? `/reviews/${review}/like` : `/lists/${button.dataset.likeList}/like`, { like });
        button.disabled = false;
        if (!data) {
            button.setAttribute('aria-pressed', like ? 'false' : 'true');
            if (count) count.textContent = String(before);
            return;
        }
        button.setAttribute('aria-pressed', data.liked ? 'true' : 'false');
        if (count) count.textContent = String(data.likes);
    });

    /* ---------- Spoilers: hidden until asked ---------- */
    document.addEventListener('click', e => {
        const show = e.target.closest('[data-spoiler-show]');
        if (!show) return;
        const warning = show.closest('[data-spoiler]');
        const text = warning?.parentElement && $('[data-spoiler-text]', warning.parentElement);
        if (text) { text.hidden = false; warning.remove(); }
    });

    /* ---------- Tabs (Home's popular reviews) ---------- */
    $$('[data-tabs]').forEach(tabs => {
        const scope = tabs.closest('section') || document;
        const buttons = $$('[data-tab]', tabs);
        buttons.forEach(b => b.addEventListener('click', () => {
            buttons.forEach(x => x.setAttribute('aria-selected', x === b ? 'true' : 'false'));
            $$('[data-pane]', scope).forEach(p => { p.hidden = p.dataset.pane !== b.dataset.tab; });
        }));
    });

    /* ---------- Game page: Your lists ---------- */
    const lists = $('[data-lists]');
    if (lists) {
        const game = Number(lists.dataset.game);
        const chips = $('.g-col-chips', lists);
        const render = mine => {
            const inside = mine.filter(l => l.hasGame);
            const outside = mine.filter(l => !l.hasGame);
            chips.innerHTML = inside.map(l => `<span class="col-chip"><a href="/lists/${l.id}">${L.esc(l.name)}</a><button type="button" aria-label="Take it out of ${L.esc(l.name)}" data-list-out="${l.id}"><svg class="ic" viewBox="0 0 24 24" aria-hidden="true"><path d="M6 6l12 12M18 6L6 18"/></svg></button></span>`).join('')
                + `<select class="col-add" data-list-add aria-label="Add to a list"><option value="">+ Add to a list…</option>${outside.map(l => `<option value="${l.id}">${L.esc(l.name)}</option>`).join('')}<option value="new">New list…</option></select>`;
        };
        lists.addEventListener('change', async e => {
            const select = e.target.closest('[data-list-add]');
            if (!select || !select.value) return;
            if (select.value === 'new') { location.href = `/lists/new?game=${game}`; return; }
            const name = select.selectedOptions[0]?.textContent || 'the list';
            const data = await L.post('/lists/item', { listId: Number(select.value), gameId: game, in: true });
            if (data) { render(data); L.toast(`Added to ${name}`); } else select.value = '';
        });
        lists.addEventListener('click', async e => {
            const out = e.target.closest('[data-list-out]');
            if (!out) return;
            const data = await L.post('/lists/item', { listId: Number(out.dataset.listOut), gameId: game, in: false });
            if (data) { render(data); L.toast('Taken out of the list'); }
        });
    }

    /* ---------- Profile: edit the four favourites ---------- */
    document.addEventListener('click', e => {
        const edit = e.target.closest('[data-favs-edit]');
        if (!edit) return;
        const form = $('[data-favs-form]');
        if (form) { form.hidden = !form.hidden; if (!form.hidden) $('select', form)?.focus(); }
    });
})();
