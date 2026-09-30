(function () {
    const STORAGE_KEY = 'fastjobs.timeDisplay';

    function currentMode() {
        return localStorage.getItem(STORAGE_KEY) === 'local' ? 'local' : 'utc';
    }

    function formatLocal(isoUtc, pattern) {
        const date = new Date(isoUtc);
        if (isNaN(date.getTime())) return isoUtc;

        const parts = {
            yyyy: date.getFullYear(),
            MM: String(date.getMonth() + 1).padStart(2, '0'),
            dd: String(date.getDate()).padStart(2, '0'),
            HH: String(date.getHours()).padStart(2, '0'),
            mm: String(date.getMinutes()).padStart(2, '0'),
            ss: String(date.getSeconds()).padStart(2, '0'),
            MMM: date.toLocaleString(undefined, { month: 'short' })
        };

        return pattern.replace(/yyyy|MMM|MM|dd|HH|mm|ss/g, token => parts[token]);
    }

    function applyMode(mode) {
        document.querySelectorAll('[data-utc]').forEach(el => {
            const iso = el.getAttribute('data-utc');
            const pattern = el.getAttribute('data-pattern') || 'MMM dd, HH:mm';
            const utcLabel = el.getAttribute('data-utc-label');

            el.textContent = mode === 'local'
                ? formatLocal(iso, pattern)
                : (utcLabel ?? el.textContent);
        });

        document.querySelectorAll('[data-time-toggle]').forEach(btn => {
            const label = mode === 'local' ? 'Show UTC' : 'Show Local Time';
            // Rebuild the icon + label structure if anything flattened it to plain text.
            if (!btn.querySelector('.nav-label')) {
                btn.innerHTML = '<span class="ms">schedule</span><span class="nav-label"></span>';
            }
            const target = btn.querySelector('.nav-label');
            if (target.textContent !== label) target.textContent = label;
            btn.title = label;
        });
    }

    window.fastJobsToggleLocalTime = function () {
        const next = currentMode() === 'local' ? 'utc' : 'local';
        localStorage.setItem(STORAGE_KEY, next);
        applyMode(next);
    };

    window.fastJobsApplyTimeDisplay = function () {
        applyMode(currentMode());
    };

    document.addEventListener('DOMContentLoaded', window.fastJobsApplyTimeDisplay);
    if (window.Blazor) {
        window.Blazor.addEventListener('enhancedload', window.fastJobsApplyTimeDisplay);
    }
})();
