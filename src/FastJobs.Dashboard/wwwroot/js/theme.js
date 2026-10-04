(function () {
    const THEME_KEY = 'fastjobs.theme';
    const SIDEBAR_KEY = 'fastjobs.sidebar';
    const media = window.matchMedia('(prefers-color-scheme: dark)');

    function read(key, fallback) {
        try { return localStorage.getItem(key) || fallback; } catch { return fallback; }
    }

    function apply() {
        const pref = read(THEME_KEY, 'system');
        const resolved = pref === 'system' ? (media.matches ? 'dark' : 'light') : pref;
        const root = document.documentElement;
        // Only write when the value differs: the <html> observer below would otherwise re-trigger itself.
        const sidebar = read(SIDEBAR_KEY, 'expanded');
        if (root.getAttribute('data-theme') !== resolved) root.setAttribute('data-theme', resolved);
        if (root.getAttribute('data-sidebar') !== sidebar) root.setAttribute('data-sidebar', sidebar);
        document.querySelectorAll('[data-theme-option]').forEach(b =>
            b.classList.toggle('active', b.getAttribute('data-theme-option') === pref));
        const icon = resolved === 'dark' ? 'light_mode' : 'dark_mode';
        document.querySelectorAll('[data-theme-icon]').forEach(i => {
            if (i.textContent !== icon) i.textContent = icon; // guard: avoids re-triggering the observer
        });
    }

    window.fastJobsSetTheme = function (value) {
        try { localStorage.setItem(THEME_KEY, value); } catch { }
        apply();
    };
    window.fastJobsToggleSidebar = function () {
        const collapsed = document.documentElement.getAttribute('data-sidebar') === 'collapsed';
        try { localStorage.setItem(SIDEBAR_KEY, collapsed ? 'expanded' : 'collapsed'); } catch { }
        apply();
    };
    window.fastJobsToggleTheme = function () {
        const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
        window.fastJobsSetTheme(isDark ? 'light' : 'dark');
    };
    media.addEventListener('change', apply);
    apply();
    document.addEventListener('DOMContentLoaded', apply);
    // Blazor re-renders can drop the attributes/active states; re-apply after enhanced navigation and renders.
    new MutationObserver(apply).observe(document.body || document.documentElement, { childList: true, subtree: true });
    // Blazor's enhanced navigation syncs <html> attributes with the server-rendered markup (which has none),
    // stripping data-theme/data-sidebar. Restore them whenever they change.
    new MutationObserver(apply).observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['data-theme', 'data-sidebar']
    });
    document.addEventListener('enhancedload', apply);
})();

// Position state hover cards with fixed coordinates so table overflow never clips them.
(function () {
    function place(icon) {
        const pop = icon.querySelector('.state-card-pop');
        if (!pop) return;
        const r = icon.getBoundingClientRect();
        pop.style.left = Math.max(8, Math.min(r.left, window.innerWidth - 260)) + 'px';
        const below = r.bottom + 8;
        const h = pop.offsetHeight || 150;
        pop.style.top = (below + h > window.innerHeight ? Math.max(8, r.top - h - 8) : below) + 'px';
    }
    ['mouseover', 'focusin'].forEach(evt => document.addEventListener(evt, e => {
        const icon = e.target.closest && e.target.closest('.state-icon');
        if (icon) place(icon);
    }));
})();
