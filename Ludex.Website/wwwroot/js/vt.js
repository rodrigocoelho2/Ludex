/* Ludex: page-to-page view transitions. Loaded in <head> so its pagereveal listener is there before the first frame.
   Only the cover of the game you open (or leave) gets a view-transition name and flies across; every other cover
   just cross-fades with the page. Naming every cover made each click snapshot hundreds of them. */
(() => {
    'use strict';
    const gameOf = href => {
        try { const m = new URL(href, location.href).pathname.match(/^\/games\/(\d+)/); return m ? m[1] : null; } catch { return null; }
    };
    const nameCover = id => {
        if (!id) return null;
        const cover = Array.from(document.querySelectorAll(`.cover[data-vt="cover-${id}"]`)).find(c => {
            const r = c.getBoundingClientRect();
            return r.width > 0 && r.bottom > 0 && r.top < window.innerHeight;
        });
        if (cover) cover.style.viewTransitionName = `cover-${id}`;
        return cover || null;
    };
    window.addEventListener('pageswap', e => {
        if (e.viewTransition && e.activation) nameCover(gameOf(e.activation.entry.url) || gameOf(location.href));
    });
    window.addEventListener('pageshow', e => {
        if (e.persisted) document.querySelectorAll('.cover[data-vt]').forEach(c => { c.style.viewTransitionName = ''; });
    });
    window.addEventListener('pagereveal', e => {
        if (!e.viewTransition) return;
        const from = window.navigation && window.navigation.activation && window.navigation.activation.from;
        const cover = nameCover(gameOf(location.href) || (from ? gameOf(from.url) : null));
        if (cover) e.viewTransition.finished.finally(() => { cover.style.viewTransitionName = ''; });
    });
})();
