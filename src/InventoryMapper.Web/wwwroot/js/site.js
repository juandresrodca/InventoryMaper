// Global search
const searchInput = document.getElementById('globalSearch');
const searchResults = document.getElementById('searchResults');

if (searchInput) {
    let searchTimeout;
    searchInput.addEventListener('input', () => {
        clearTimeout(searchTimeout);
        const q = searchInput.value.trim();
        if (q.length < 2) { searchResults.style.display = 'none'; return; }
        searchTimeout = setTimeout(async () => {
            try {
                const res = await fetch(`/Assets/Search?q=${encodeURIComponent(q)}`);
                const data = await res.json();
                if (!data.length) { searchResults.style.display = 'none'; return; }
                searchResults.innerHTML = data.map(a => `
                    <a href="/Assets/Details/${a.id}" style="display:flex;align-items:center;gap:10px;padding:10px 16px;border-bottom:1px solid var(--border-color);color:var(--text-primary);text-decoration:none;" onmouseover="this.style.background='var(--bg-hover)'" onmouseout="this.style.background=''">
                        <span class="dot dot-${a.state.toLowerCase()}"></span>
                        <div>
                            <div style="font-weight:500;font-size:13px;">${a.hostname}</div>
                            <div style="font-size:11px;color:var(--text-muted);">${a.type} · ${a.ipAddress || 'No IP'}</div>
                        </div>
                    </a>
                `).join('');
                searchResults.style.display = 'block';
            } catch {}
        }, 250);
    });

    document.addEventListener('click', e => {
        if (!searchInput.contains(e.target) && !searchResults.contains(e.target))
            searchResults.style.display = 'none';
    });
}

// Auto-dismiss flash alerts after 5s
document.querySelectorAll('.alert').forEach(el => {
    if (el.closest('.modal-body') || el.closest('.canvas-toolbar')) return;
    setTimeout(() => el.style.transition = 'opacity 0.5s', 4500);
    setTimeout(() => { el.style.opacity = '0'; setTimeout(() => el.remove(), 500); }, 5000);
});
